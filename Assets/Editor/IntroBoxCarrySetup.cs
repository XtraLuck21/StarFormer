using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit authoring action only. No automatic edits when opening the project.
public static class IntroBoxCarrySetup
{
    public static void FinalizeVisuals()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (string name in new[] { "Exit FlagPole", "Exit FlagCloth" })
            if (GameObject.Find(name) != null) Object.DestroyImmediate(GameObject.Find(name));
        GameObject.Find("IntroBoxFlag").transform.position = new Vector3(13.1f, -1.02f, 0);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("INTROBOX FLAG aligned with receiving-platform surface and gate trigger.");
    }
    const string ScenePath = "Assets/Scenes/IntroBox.unity";
    static Sprite groundSprite;
    static Material groundMaterial;
    static Transform root;

    [MenuItem("Tools/IntroBox/Build Carry Lesson")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (Object.FindFirstObjectByType<IntroBoxCarryLesson>() != null)
            throw new InvalidOperationException("Carry lesson already exists. Edit the authored scene rather than rebuilding it.");
        var player = Object.FindFirstObjectByType<PlayerPossessionController>();
        var star = GameObject.Find("StarPlayer").GetComponent<PossessableBody>();
        var a = GameObject.Find("BoxA").GetComponent<PossessableBody>();
        var c = GameObject.Find("BoxB").GetComponent<PossessableBody>();
        var ground = GameObject.Find("Ground").GetComponent<SpriteRenderer>();
        groundSprite = ground.sprite; groundMaterial = ground.sharedMaterial;

        // Retain old inactive content and UI references, replacing only this scene's layout.
        string[] remove = { "Ground", "JumpStep", "TutorialPlatform", "RelayPlatform", "TunnelRoof",
            "TunnelEndWall", "BubbleBody", "BubbleCanopy", "PressurePlate", "TutorialGoal",
            "BoxA_GrowHint", "BoxB_GrowHint", "StarMoveHint", "StarJumpHint", "ContinueRightHint" };
        foreach (string name in remove) { var obj = GameObject.Find(name); if (obj != null) obj.SetActive(false); }
        foreach (var oldHint in Object.FindObjectsByType<IntroFormHintUI>(FindObjectsSortMode.None)) oldHint.enabled = false;
        GameObject.Find("TutorialTitle")?.SetActive(false);
        GameObject.Find("TutorialInstructions")?.SetActive(false);

        root = new GameObject("IntroBox Carry Layout").transform;
        Terrain("Floor", new Vector2(0, -3.5f), new Vector2(34, 1));
        Terrain("Left Boundary", new Vector2(-16.5f, .5f), new Vector2(1, 8));
        Terrain("Jump Practice Step", new Vector2(-12.1f, -2.825f), new Vector2(.8f, .35f));
        Terrain("Entry Roof - No Star Bypass", new Vector2(-8.8f, 1.4f), new Vector2(3.4f, 6.24f));
        var platform = Terrain("Receiving Ledge", new Vector2(10, -2.01f), new Vector2(12, 1.98f));
        Terrain("C Low Passage", new Vector2(9.8f, .9f), new Vector2(3.5f, 2.5f)); // underside -.35, clearance .67
        Terrain("Right Boundary", new Vector2(16.5f, .5f), new Vector2(1, 8));

        ConfigureBody(a, new Vector2(-8.6f, -2.4f), 1.2f);
        c.name = "BoxC";
        ConfigureBody(c, new Vector2(-2.65f, -.55f), .9f);
        var b = Object.Instantiate(a.gameObject).GetComponent<PossessableBody>();
        b.name = "BoxB";
        ConfigureBody(b, new Vector2(-3.5f, -2), 2);
        Set(b, "moveSpeed", 2.5f);
        star.transform.position = new Vector3(-14.5f, -2.45f, 0);
        Set(player, "maximumHeight", 12f);

        var camera = Camera.main;
        foreach (var component in camera.GetComponents<MonoBehaviour>())
            if (!(component is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)) component.enabled = false;
        camera.transform.position = new Vector3(-8, .5f, -10);
        camera.orthographic = true; camera.orthographicSize = 5.3f;
        camera.backgroundColor = new Color(.77f, .87f, .91f, 1);

        var flag = Object.FindFirstObjectByType<IntroFlagGate>();
        Set(flag, "requiredBody", c);
        var flagGO = flag.gameObject;
        flagGO.transform.position = new Vector3(13.1f, -.4f, 0);
        flagGO.transform.localScale = Vector3.one;
        flag.GetComponent<BoxCollider2D>().size = new Vector2(.7f, 1.3f);
        GameObject.Find("IntroBoxFlag").transform.position = new Vector3(13.1f, -1.02f, 0);
        var door = (GameObject)new SerializedObject(flag).FindProperty("exitDoor").objectReferenceValue;
        door.SetActive(true);
        door.transform.position = new Vector3(14.2f, .1f, 0);
        door.transform.localScale = new Vector3(.25f, 2.3f, 1);
        var exit = Object.FindFirstObjectByType<IntroSceneExit>();
        exit.transform.position = new Vector3(15.3f, -.25f, 0);
        exit.transform.localScale = Vector3.one;
        exit.GetComponent<BoxCollider2D>().size = new Vector2(.5f, 1.5f);
        Set(exit, "returnBody", c); Set(exit, "returnPosition", new Vector2(13.1f, -.78f));
        Set(exit, "returnBoxSize", .5f);

        var lesson = root.gameObject.AddComponent<IntroBoxCarryLesson>();
        lesson.player = player; lesson.boxA = a; lesson.carrier = b; lesson.cargo = c;
        lesson.receivingPlatform = platform; lesson.lessonCamera = camera; lesson.flag = flag;
        var panel = new GameObject("Local Lesson Hint").transform;
        panel.SetParent(root); panel.position = new Vector3(-13, .5f, -1);
        var text = Label(panel, "Local Instructions", "", Vector2.zero, 3, new Vector2(5, .5f));
        lesson.hint = text; lesson.hintPanel = panel;
        PolishVisuals();
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("INTROBOX CARRY LAYOUT SAVED");
    }

    [MenuItem("Tools/IntroBox/Polish Current Scene Visuals")]
    public static void PolishVisuals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var lesson = Object.FindFirstObjectByType<IntroBoxCarryLesson>();
        if (lesson == null || lesson.gameObject.scene.path != ScenePath)
            throw new InvalidOperationException("Open the authored IntroBox scene first.");
        var scene = lesson.gameObject.scene;
        var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform Find(string name) => objects.FirstOrDefault(t => t != null && t.name == name);
        foreach (var t in objects.Where(t => new[] { "Body Letter", "Hint Backplate", "Move Key Label",
            "Jump Key Label", "Dock Label", "Exit Label" }.Contains(t.name)))
            Object.DestroyImmediate(t.gameObject);
        var ground = Find("Ground").GetComponent<SpriteRenderer>();
        foreach (var t in lesson.transform.Cast<Transform>())
        {
            var sprite = t.GetComponent<SpriteRenderer>();
            if (sprite != null && t.GetComponent<Collider2D>() != null) sprite.color = ground.color;
        }
        lesson.lessonCamera.backgroundColor = new Color(.6784314f, .654902f, .654902f, 0);
        lesson.hint.text = "";
        lesson.hint.fontSize = 2.8f;
        lesson.hint.color = new Color(.12f, .12f, .12f, 1);
        lesson.hint.rectTransform.sizeDelta = new Vector2(6.6f, 1f);
        lesson.hint.textWrappingMode = TextWrappingModes.NoWrap;
        ConfigureWelcomeSign(lesson, ground);
        foreach (string retired in new[] { "Reading Cue", "Reading Progress" })
        {
            var child = lesson.hintPanel.Find(retired);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
        ConfigureLedgeGuide(lesson, ground);
        // Extra flat runway separates movement practice from the existing jump.
        Find("Floor").position = new Vector3(-2, -3.5f, 0);
        Find("Floor").localScale = new Vector3(38, 1, 1);
        Find("Left Boundary").position = new Vector3(-20.5f, .5f, 0);
        Find("StarPlayer").position = new Vector3(-18.5f, -2.45f, 0);
        lesson.lessonCamera.transform.position = new Vector3(-10.6f, .5f, -10);
        lesson.moveKeys = ConfigureKeys(Find("StarMoveHint"), new Vector2(-18.5f, -.9f),
            new Vector2(9, 5), new Vector2(1.8f, -1));
        lesson.jumpKey = ConfigureKeys(Find("StarJumpHint"), new Vector2(Find("Jump Practice Step").position.x, -.9f),
            new Vector2(3.8f, 5), new Vector2(-.3f, -1));
        lesson.dockShrink = ConfigureResize(lesson, "Dock Shrink Guide", lesson.carrier,
            BoxGrowHint.HintMode.Shrink, new Vector2(3, -2.5f), Vector2.one);
        lesson.reconnectGrow = ConfigureResize(lesson, "Reconnect Grow Guide", lesson.carrier,
            BoxGrowHint.HintMode.Grow, new Vector2(3, -2), Vector2.one * 2);
        lesson.passageShrink = ConfigureResize(lesson, "Passage Shrink Guide", lesson.cargo,
            BoxGrowHint.HintMode.Shrink, new Vector2(7.1f, -.76f), Vector2.one * .5f);
        // The existing exit arrow replaces the extra EXIT text label.
        var exitArrow = Find("ContinueRightHint");
        if (exitArrow != null)
        {
            exitArrow.position = new Vector3(14, 1.7f, 0);
            exitArrow.gameObject.SetActive(true);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("INTROBOX POLISH SAVED: original palette, visual keycaps, brief local hints, no body letters.");
    }

    [MenuItem("Tools/IntroBox/Align Jump Hint Above Step")]
    public static void AlignJumpHint()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var lesson = Object.FindFirstObjectByType<IntroBoxCarryLesson>();
        if (lesson == null || lesson.gameObject.scene.path != ScenePath) return;
        var step = lesson.transform.Find("Jump Practice Step");
        var guide = lesson.jumpKey;
        if (step == null || guide == null) return;
        Undo.RecordObjects(new Object[] { guide, guide.transform }, "Align jump hint above step");
        Vector3 before = guide.transform.position;
        guide.transform.position = new Vector3(step.position.x, before.y, before.z);
        // Preserve the existing world-space trigger area while moving the drawing.
        var settings = new SerializedObject(guide);
        var offset = settings.FindProperty("activationAreaOffset");
        offset.vector2Value += new Vector2(before.x - step.position.x, 0);
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(lesson.gameObject.scene);
        EditorSceneManager.SaveScene(lesson.gameObject.scene);
        Debug.Log("INTROBOX JUMP HINT aligned above the step; activation area preserved.");
    }

    static void ConfigureLedgeGuide(IntroBoxCarryLesson lesson, SpriteRenderer ground)
    {
        var existing = lesson.transform.Find("Dock Destination Guide");
        if (existing != null)
        {
            lesson.ledgeGuide = existing.gameObject;
            lesson.ledgeRoute = existing.Find("Carry Route").gameObject;
            return;
        }
        var guide = new GameObject("Dock Destination Guide").transform;
        guide.SetParent(lesson.transform, false);
        lesson.ledgeGuide = guide.gameObject;
        var gold = new Color(1, .82f, .15f, .85f);
        var target = new GameObject("Passenger Landing Outline").transform;
        target.SetParent(guide, false);
        target.position = new Vector3(3.85f, -.55f, -.1f);
        for (int edge = 0; edge < 4; edge++)
            for (int dash = 0; dash < 6; dash++)
            {
                float along = -.42f + dash * .16f;
                bool horizontal = edge < 2;
                var position = horizontal ? new Vector2(along, edge == 0 ? -.47f : .47f) :
                    new Vector2(edge == 2 ? -.47f : .47f, along);
                SignStroke(target, "Landing dash", position,
                    horizontal ? new Vector2(.10f, .035f) : new Vector2(.035f, .10f), gold, ground);
            }
        var route = new GameObject("Carry Route").transform;
        route.SetParent(guide, false);
        lesson.ledgeRoute = route.gameObject;
        // A short dotted path points right, then down into the landing outline.
        for (int i = 0; i < 11; i++)
            SignStroke(route, "Rightward dash", new Vector2(.65f + i * .29f, .75f),
                new Vector2(.16f, .04f), gold, ground);
        SignStroke(route, "Downward shaft", new Vector2(3.85f, .49f), new Vector2(.04f, .5f), gold, ground);
        var left = SignStroke(route, "Landing arrow left", new Vector2(3.73f, .33f), new Vector2(.32f, .04f), gold, ground);
        left.transform.localRotation = Quaternion.Euler(0, 0, -48);
        var right = SignStroke(route, "Landing arrow right", new Vector2(3.97f, .33f), new Vector2(.32f, .04f), gold, ground);
        right.transform.localRotation = Quaternion.Euler(0, 0, 48);
        guide.gameObject.SetActive(false);
    }

    static void ConfigureWelcomeSign(IntroBoxCarryLesson lesson, SpriteRenderer ground)
    {
        var existing = lesson.transform.Find("Touch Welcome Sign");
        if (existing != null) { lesson.welcomeSign = existing.gameObject; return; }
        var sign = new GameObject("Touch Welcome Sign").transform;
        sign.SetParent(lesson.transform, false);
        sign.position = new Vector3(-8.8f, .45f, -.1f);
        lesson.welcomeSign = sign.gameObject;
        var gold = new Color(1, .82f, .15f, 1);
        var title = Label(sign, "Touch Title", "Touch it.\nBecome it!", new Vector2(0, 1.25f), 3.4f, new Vector2(2.9f, 1.1f));
        title.color = gold;
        var description = Label(sign, "Touch Description", "Find a new body.\nMake it yours.", new Vector2(0, .15f), 2.5f, new Vector2(2.9f, .9f));
        description.color = Color.white;
        // A visual sentence: the same Star moves inside the orange body.
        SignStar(sign, "Star before touch", new Vector2(-.95f, -.9f), .65f);
        SignStroke(sign, "Body after touch", new Vector2(.85f, -.9f), Vector2.one * .72f,
            lesson.boxA.GetComponent<SpriteRenderer>().color, ground);
        SignStar(sign, "Star inside body", new Vector2(.85f, -.9f), .43f);
        SignStroke(sign, "Touch arrow", new Vector2(-.05f, -.9f), new Vector2(.52f, .035f), Color.white, ground);
        var upper = SignStroke(sign, "Arrow upper", new Vector2(.15f, -.84f), new Vector2(.2f, .035f), Color.white, ground);
        upper.transform.localRotation = Quaternion.Euler(0, 0, -40);
        var lower = SignStroke(sign, "Arrow lower", new Vector2(.15f, -.96f), new Vector2(.2f, .035f), Color.white, ground);
        lower.transform.localRotation = Quaternion.Euler(0, 0, 40);
        SignStroke(sign, "Sign accent", new Vector2(0, 2.05f), new Vector2(2.6f, .035f), gold, ground);
    }

    static void SignStar(Transform parent, string name, Vector2 position, float scale)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(position.x, position.y, -.04f);
        obj.transform.localScale = Vector3.one * scale;
        obj.AddComponent<StarVisual>();
    }

    static SpriteRenderer SignStroke(Transform parent, string name, Vector2 position, Vector2 size,
        Color color, SpriteRenderer source)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(position.x, position.y, -.02f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite; renderer.sharedMaterial = source.sharedMaterial;
        renderer.color = color; renderer.sortingOrder = 5;
        return renderer;
    }

    static BubbleMovementHint ConfigureKeys(Transform t, Vector2 position, Vector2 area, Vector2 offset)
    {
        t.position = position;
        var guide = t.GetComponent<BubbleMovementHint>();
        Set(guide, "activationAreaSize", area); Set(guide, "activationAreaOffset", offset);
        t.gameObject.SetActive(true);
        return guide;
    }

    static BoxGrowHint ConfigureResize(IntroBoxCarryLesson lesson, string name, PossessableBody body,
        BoxGrowHint.HintMode mode, Vector2 position, Vector2 frame)
    {
        var t = lesson.transform.Find(name);
        if (t == null) { t = new GameObject(name).transform; t.SetParent(lesson.transform); }
        t.gameObject.SetActive(false);
        t.position = position;
        var guide = t.GetComponent<BoxGrowHint>() ?? t.gameObject.AddComponent<BoxGrowHint>();
        var settings = new SerializedObject(guide);
        settings.FindProperty("mode").enumValueIndex = (int)mode;
        settings.FindProperty("playerController").objectReferenceValue = lesson.player;
        settings.FindProperty("targetBody").objectReferenceValue = body;
        settings.FindProperty("frameSize").vector2Value = frame;
        settings.FindProperty("activationAreaSize").vector2Value = body == lesson.carrier ?
            new Vector2(4.2f, 5) : new Vector2(8, 5);
        settings.FindProperty("showHoldLabel").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        return guide;
    }

    static BoxCollider2D Terrain(string name, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject(name); obj.transform.SetParent(root);
        obj.transform.position = pos; obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = groundSprite;
        renderer.sharedMaterial = groundMaterial; renderer.color = new Color(.16f, .22f, .29f);
        return obj.AddComponent<BoxCollider2D>();
    }
    static void ConfigureBody(PossessableBody body, Vector2 pos, float size)
    {
        body.gameObject.SetActive(true); body.transform.SetParent(null);
        body.transform.position = pos; body.transform.localScale = Vector3.one * size;
        body.GetComponent<BoxCollider2D>().size = Vector2.one;
        body.GetComponent<BoxCollider2D>().offset = Vector2.zero;
        var rigid = body.GetComponent<Rigidbody2D>(); rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        rigid.bodyType = RigidbodyType2D.Dynamic; rigid.linearVelocity = Vector2.zero;
        Set(body.GetComponent<BoxResizeAbility>(), "minimumSize", .5f);
        Set(body.GetComponent<BoxResizeAbility>(), "maximumSize", 2f);
    }
    static TextMeshPro Label(Transform parent, string name, string value, Vector2 pos, float size, Vector2 area)
    {
        var text = new GameObject(name).AddComponent<TextMeshPro>(); text.transform.SetParent(parent, false);
        text.transform.localPosition = new Vector3(pos.x, pos.y, -.2f);
        text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = area; text.color = new Color(.08f, .15f, .20f);
        text.sortingOrder = 30; text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
    static void Set(Object obj, string name, Object value) { var s = new SerializedObject(obj); s.FindProperty(name).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Set(Object obj, string name, float value) { var s = new SerializedObject(obj); s.FindProperty(name).floatValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Set(Object obj, string name, Vector2 value) { var s = new SerializedObject(obj); s.FindProperty(name).vector2Value = value; s.ApplyModifiedPropertiesWithoutUndo(); }
}
