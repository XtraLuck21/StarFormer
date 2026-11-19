using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

// Opt-in checks in a disposable project copy. Uses actual 2D physics and UI pointer handlers.
public static class SpiderSilkVerification
{
    private static PossessableBody body;
    private static PlayerPossessionController player;
    private static HexagonSurfaceMovement movement;
    private static SpiderSilkAbility silk;
    private static BoxCollider2D wall, floor;
    private static SpiderSilkAimControl wheel;
    private static Camera camera;
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Debug.Log("SILK PASS: " + message);
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/.utmp/"))
            throw new InvalidOperationException("Run only in a disposable .utmp project copy.");
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Global Light").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            light.intensity = 1f;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Time.timeScale = 1f;
            camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.transform.position = new Vector3(0.5f, 1f, -10f);
            camera.backgroundColor = new Color(0.04f, 0.08f, 0.13f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            player = new GameObject("Player").AddComponent<PlayerPossessionController>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Form/Spider.prefab");
            var spider = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            body = spider.GetComponent<PossessableBody>();
            movement = spider.GetComponent<HexagonSurfaceMovement>();
            silk = spider.GetComponent<SpiderSilkAbility>();
            Call(body, "Awake");
            Call(movement, "Awake");
            Call(silk, "Awake");
            Set(player, "currentBody", body);
            Set(player, "initialBody", body);
            Set(player, "useHeightLimit", false);
            body.SetOwner(player);
            floor = Solid("Floor", new Vector2(0f, -0.6f), new Vector2(12f, 0.2f));
            wall = Solid("Wall", new Vector2(2.5f, 1f), new Vector2(0.2f, 5f));
            Reset();
            Check(silk.TryFire(Vector2.right), "Can shoot at a nearby wall");
            Steps(45);
            Check(body.Body.position.x > 1.7f && body.Body.position.x < 2.1f &&
                movement.IsAttached && movement.AttachedSurface == wall, "Pull reaches wall and attaches without penetrating");
            float beforeY = body.Body.position.y;
            for (int i = 0; i < 8; i++) { movement.Tick(1f); Simulate(); }
            Check(body.Body.position.y > beforeY + 0.25f, "Surface movement continues up the wall after pulling");

            Reset();
            wall.transform.position = new Vector3(0f, 2.5f, 0f);
            wall.size = new Vector2(8f, 0.2f);
            Physics2D.SyncTransforms();
            Check(silk.TryFire(Vector2.up), "Can shoot upward");
            Steps(45);
            Check(body.Body.position.y > 1.7f && movement.IsAttached && movement.AttachedSurface == wall,
                "Ceiling pull hands off to ceiling attachment");

            Reset();
            wall.transform.position = new Vector3(-2.5f, 1f, 0f);
            Physics2D.SyncTransforms();
            silk.TryFire(Vector2.left);
            Steps(45);
            Check(body.Body.position.x < -1.7f && movement.AttachedSurface == wall, "Leftward pull works");

            Reset();
            wall.transform.position = new Vector3(5f, 1f, 0f);
            Physics2D.SyncTransforms();
            silk.TryFire(Vector2.right);
            Steps(30);
            Check(!silk.IsPulling && Mathf.Abs(body.Body.position.x) < 0.1f, "An out-of-range shot does not pull");

            Reset();
            var trigger = Solid("Trigger", new Vector2(1f, 0.1f), Vector2.one);
            trigger.isTrigger = true;
            silk.TryFire(Vector2.right);
            Steps(45);
            Check(body.Body.position.x > 1.7f && movement.AttachedSurface == wall, "Triggers do not block a wall grapple");
            trigger.gameObject.SetActive(false);

            Reset();
            wall.gameObject.layer = 2;
            // Include Ignore Raycast in the silk mask to check that climbing rules still reject it.
            Set(silk, "hitMask", (LayerMask)(~0));
            silk.TryFire(Vector2.right);
            Steps(30);
            Check(!silk.IsPulling && Mathf.Abs(body.Body.position.x) < 0.1f, "Non-climbable solid cannot become an anchor");
            wall.gameObject.layer = 0;

            Reset();
            var corner = Solid("Corner", new Vector2(1.2f, 0.6f), new Vector2(0.2f, 0.35f));
            silk.TryFire(Vector2.right);
            Steps(45);
            Check(body.Body.position.x < 1.2f && !body.BodyCollider.Distance(corner).isOverlapped,
                "Body sweep stops a pull at a corner missed by the silk ray");
            corner.gameObject.SetActive(false);

            Reset();
            StartPull();
            wall.transform.position += Vector3.right * 0.2f;
            Physics2D.SyncTransforms();
            Steps(45);
            Check(body.Body.position.x > 1.9f && movement.AttachedSurface == wall, "Anchor follows a moving wall");

            Reset();
            StartPull();
            wall.gameObject.SetActive(false);
            silk.TickPhysics();
            Check(!silk.IsPulling && body.Body.gravityScale > 0f, "Removed anchor cancels pull and restores gravity");
            wall.gameObject.SetActive(true);

            Reset();
            StartPull();
            Vector2 beforePause = body.Body.position;
            Time.timeScale = 0f;
            silk.TickPhysics();
            Check(silk.IsPulling && body.Body.position == beforePause, "Pause preserves the in-flight pull");
            Time.timeScale = 1f;
            body.SetOwner(null);
            Check(!silk.IsFiring && body.Body.gravityScale > 0f, "Ownership loss clears silk and restores gravity");
            body.SetOwner(player);

            Reset();
            StartPull();
            Set(player, "isDead", true);
            silk.TickPhysics();
            Check(!silk.IsFiring && body.Body.gravityScale > 0f, "Death clears the pull");
            Set(player, "isDead", false);

            VerifyWheel();
            Debug.Log("SPIDER SILK VERIFICATION COMPLETE");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static BoxCollider2D Solid(string name, Vector2 position, Vector2 size)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = size;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = size;
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2f);
        renderer.color = new Color(0.2f, 0.32f, 0.42f);
        return collider;
    }

    private static void Reset()
    {
        silk.Cancel();
        Set(silk, "nextShotAt", -1f);
        movement.Release();
        body.transform.position = new Vector3(0f, 0.03f, 0f);
        body.Body.position = body.transform.position;
        body.Body.linearVelocity = Vector2.zero;
        wall.transform.position = new Vector3(2.5f, 1f, 0f);
        wall.size = new Vector2(0.2f, 5f);
        Physics2D.SyncTransforms();
        movement.Tick(0f);
    }

    private static void Simulate() { Physics2D.SyncTransforms(); Physics2D.Simulate(Time.fixedDeltaTime); }
    private static void Steps(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!silk.TickPhysics()) movement.Tick(0f);
            Simulate();
        }
    }
    private static void StartPull()
    {
        Check(silk.TryFire(Vector2.right), "Pull can be started after resetting");
        for (int i = 0; i < 20 && !silk.IsPulling; i++) Steps(1);
        Check(silk.IsPulling, "Silk entered the pulling state");
    }

    private static void VerifyWheel()
    {
        Reset();
        var events = new GameObject("EventSystem").AddComponent<EventSystem>();
        var mobile = player.gameObject.AddComponent<MobileFormControls>();
        var mode = typeof(MobileFormControls).GetField("showMode", BindingFlags.Instance | BindingFlags.NonPublic);
        mode.SetValue(mobile, Enum.Parse(mode.FieldType, "NeverShow"));
        wheel = SpiderSilkAimControl.Create(player);
        Call(wheel, "LateUpdate");
        var group = wheel.GetComponentInParent<CanvasGroup>();
        Check(group.alpha == 0f && !group.blocksRaycasts, "Desktop wheel is invisible and does not intercept mouse clicks");
        mode.SetValue(mobile, Enum.Parse(mode.FieldType, "AlwaysShow"));
        Call(wheel, "LateUpdate");
        Check(group.alpha == 1f && group.blocksRaycasts, "Mobile wheel is visible and accepts touches");
        Call(wheel, "LateUpdate");
        Canvas.ForceUpdateCanvases();
        Vector2 center = RectTransformUtility.WorldToScreenPoint(null, wheel.transform.position);
        float scale = wheel.GetComponentInParent<Canvas>().scaleFactor;
        var e = new PointerEventData(events) { pointerId = 11, button = PointerEventData.InputButton.Left, position = center };
        wheel.OnPointerDown(e);
        Call(wheel, "LateUpdate");
        Check(!silk.IsFiring, "Pressing the skill wheel does not fire immediately");
        e.position = center + new Vector2(-90f, 60f) * scale;
        wheel.OnDrag(e);
        Check(silk.IsAiming, "Dragging out of center starts directional aim");
        var otherFinger = new PointerEventData(events) { pointerId = 22, button = PointerEventData.InputButton.Left, position = e.position };
        wheel.OnPointerUp(otherFinger);
        Check(silk.IsAiming && !silk.IsFiring, "Another finger cannot release the skill wheel");
        e.position = center + Vector2.up * 230f * scale;
        wheel.OnPointerUp(e);
        Check(!silk.IsAiming && !silk.IsFiring && silk.CooldownRemaining == 0f, "Cancel target consumes no cooldown");

        e.position = center;
        wheel.OnPointerDown(e);
        e.position = center + Vector2.right * 90f * scale;
        wheel.OnDrag(e);
        wheel.OnPointerUp(e);
        Check(silk.IsFiring && !silk.IsAiming, "Release fires the aimed strand");
        silk.Cancel();
        Check(!silk.BeginAim(), "Cooldown prevents repeated casts");
        Reset();
        e.position = center;
        wheel.OnPointerDown(e);
        wheel.OnPointerUp(e);
        Check(!silk.IsFiring && silk.CooldownRemaining == 0f, "Neutral tap cancels without firing");

        wheel.OnPointerDown(e);
        e.position = center + Vector2.right * 90f * scale;
        wheel.OnDrag(e);
        wheel.CancelGesture();
        wheel.OnPointerUp(e);
        Check(!silk.IsAiming && !silk.IsFiring, "Interrupted gesture cannot fire on a late release");

        // Render only after input checks: changing canvas render modes changes screen coordinates.
        e.position = center;
        wheel.OnPointerDown(e);
        e.position = center + Vector2.right * 90f * scale;
        wheel.OnDrag(e);
        Call(silk, "DrawAim");
        Capture("SpiderSilkAim.png");
        camera.backgroundColor = new Color(0.83f, 0.91f, 0.94f);
        silk.Aim(new Vector2(0.7f, 0.7f));
        Call(silk, "DrawAim");
        Capture("SpiderSilkAimLight.png");
        camera.backgroundColor = new Color(0.4f, 0.38f, 0.38f);
        wheel.GetComponentInParent<Canvas>().enabled = false;
        Capture("SpiderSilkAimGray.png", 450, 318);
        silk.Aim(Vector2.right);
        Capture("SpiderSilkAimContact.png", 450, 318);
    }

    private static void Capture(string file, int width = 1600, int height = 900)
    {
        var canvas = wheel.GetComponentInParent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var target = new RenderTexture(width, height, 24);
        target.Create();
        camera.targetTexture = target;
        Call(silk, "DrawAim");
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        Directory.CreateDirectory("Logs");
        File.WriteAllBytes(Path.Combine("Logs", file), image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        target.Release();
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);
    }
}
