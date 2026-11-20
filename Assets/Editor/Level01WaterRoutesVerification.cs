using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit real-input regression for the two entrance routes and early-pop recovery.
[InitializeOnLoad]
public static class Level01WaterRoutesVerification
{
    const string KeyName = "Level01WaterRoutesVerification.Running";
    const string Output = "/private/tmp/level01-water-verification";
    static Keyboard keyboard;
    static PlayerPossessionController player;
    static Level01WaterRouteHints hints;
    static int step, bounces;
    static float since, maxY, riseStarted, loopDistance;
    static Vector2 loopLast;
    static bool swimRight;
    static float bounceLow, bounceHigh;
    static bool sawRiseLesson, capturedRise;
    static double deadline, reportAt;
    static bool wired;
    static Level01WaterRoutesVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyName, false))
            {
                step = SessionState.GetInt(KeyName + ".StartAt", 0); SessionState.SetInt(KeyName + ".StartAt", 0); bounces = 0; wired = sawRiseLesson = capturedRise = false; maxY = 0; since = Time.time;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                Application.runInBackground = true;
                keyboard = InputSystem.AddDevice<Keyboard>();
                deadline = EditorApplication.timeSinceStartup + 240;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(KeyName, false))
            {
                EditorApplication.update -= Tick;
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                SessionState.SetBool(KeyName, false);
                InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(KeyName + ".Input", 0);
                InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt(KeyName + ".Background", 0);
                Application.runInBackground = SessionState.GetBool(KeyName + ".RunBackground", false);
                if (Application.isBatchMode) EditorApplication.Exit(File.ReadAllText(Output + "/result.txt").Contains("\nPASS\n") ? 0 : 1);
            }
        };
    }
    [MenuItem("StarFormer/Level01/Verify Water Pocket Recovery")]
    public static void RunPocket()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetInt(KeyName + ".StartAt", 35);
        Run();
    }
    [MenuItem("StarFormer/Level01/Verify Water Routes in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        Directory.CreateDirectory(Output); File.WriteAllText(Output + "/result.txt", "RUNNING\n");
        SessionState.SetInt(KeyName + ".Input", (int)InputSystem.settings.editorInputBehaviorInPlayMode);
        SessionState.SetInt(KeyName + ".Background", (int)InputSystem.settings.backgroundBehavior);
        SessionState.SetBool(KeyName + ".RunBackground", Application.runInBackground);
        PlayerPossessionController.ResetCheckpointSession(); AdvancedCheckpointController.ClearSession();
        SessionState.SetBool(KeyName, true); EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at stage " + step);
            if (player == null) player = Object.FindFirstObjectByType<PlayerPossessionController>();
            if (hints == null) hints = Object.FindFirstObjectByType<Level01WaterRouteHints>();
            if (player == null || player.CurrentBody == null || hints == null) return;
            var body = player.CurrentBody; var pos = body.Body.position;
            if (EditorApplication.timeSinceStartup > reportAt)
            {
                reportAt = EditorApplication.timeSinceStartup + 2;
                File.AppendAllText(Output + "/result.txt", $"stage={step} form={body.Form} pos={pos} bounces={bounces}\n");
            }
            Check(!player.IsDead, "No deaths during routes");
            CheckNoOptionalHints();
            switch (step)
            {
                case 0:
                    if (Time.time - since < 1) break;
                    Check(body.Form == BodyForm.Star && GameObject.Find("BoxA_Start") == null, "Start as Star without BoxA");
                    CheckBubbleAppearance();
                    Move(Key.D); Next(1); break;
                case 1:
                    if (pos.x < 8.1f) break;
                    Check(body.Form == BodyForm.Star && body.IsInWater, "Star crosses the entry slit directly into water");
                    Check(hints.LessonText == "", "Star entry has no choice card or unsolicited explanation");
                    Capture("01-star-entry"); Next(2); break;
                case 2:
                    if (body.Form != BodyForm.BouncyBall) break;
                    if (!wired)
                    {
                        Check(pos.y < 2.4f, "Possessing the submerged ball does not teleport it to the surface");
                        body.GetComponent<BouncyBallAbility>().Bounced += () => bounces++;
                        riseStarted = Time.time; wired = true;
                    }
                    if (hints.LessonText.Contains("REMEMBER?") && hints.LessonText.Contains("Bouncy balls float!")) sawRiseLesson = true;
                    if (!capturedRise && pos.y > 2.8f && pos.y < 4.6f)
                    {
                        Capture("02a-gradual-rise-lesson"); capturedRise = true;
                    }
                    if (pos.y < 5.3f) break;
                    Check(body.IsInWater, "Ball floats on surface after contact possession");
                    Check(Time.time - riseStarted > 1.5f && sawRiseLesson, "First Ball possession shows REMEMBER? while it gradually floats");
                    Move(); Capture("02-ball-float"); Next(201); break;
                case 201:
                    if (Time.time - since < 14f) break;
                    Check(hints.LessonText == "" && hints.RecoveryText == "", "Waiting on the water shows no platform or route hint");
                    Capture("02b-floating-without-hints"); Move(Key.D); Next(3); break;
                case 3:
                    maxY = Mathf.Max(maxY, pos.y);
                    if (pos.x < 19.3f) break;
                    Check(body.Form == BodyForm.BouncyBall && bounces > 0 && maxY > 9.5f, "Ball bounces from dry deck and crosses upper opening");
                    Move(); Capture("03-ball-upper-exit"); Next(4); break;
                case 4:
                    if (body.Form != BodyForm.Box) break;
                    Check(body.name == "BoxB_DropShaft", "Ball route rejoins at BoxB");
                    Capture("04-ball-box-transfer");
                    File.AppendAllText(Output + "/result.txt", "PASS: Star entry and ball route\n");
                    player = null; hints = null; SceneManager.LoadScene("Level01"); Next(10); break;
                case 10:
                    if (Time.time - since < 1) break;
                    Move(Key.D); Next(11); break;
                case 11:
                    if (pos.x < 9.1f) break;
                    Move(Key.Space); Next(12); break;
                case 12:
                    if (body.Form != BodyForm.Bubble) break;
                    if (!hints.LessonText.Contains("Bubbles swim in water")) break;
                    Check(hints.LessonText.StartsWith("REMEMBER?\n") && hints.RecoveryText == "", "First Bubble contact shows only its REMEMBER? ability reminder");
                    Move(); Capture("05-bubble-choice"); Next(120); break;
                case 120:
                    if (Time.time - since < 5.6f) break;
                    Check(hints.LessonText == "", "Ability reminder disappears without replacing it with route instructions");
                    Move(Key.A); player.RequestRelease(); Next(13); break;
                case 13:
                    if (pos.x > 8.15f) break;
                    Move(); Next(131); break;
                case 131:
                    if (Time.time - since < .2f) break;
                    Check(body.Form == BodyForm.Star, "Early Pop restores Star");
                    Check(hints.RecoveryText == "" && hints.LessonText == "", "Pop does not automatically reveal help or redisplay the choice");
                    Check(Object.FindObjectsByType<PossessableBody>(FindObjectsSortMode.None).Count(b => b.name == "BubbleU1_WaterEntry") == 1, "Exactly one renewable entry Bubble");
                    CheckBubbleAppearance();
                    Move(); Capture("06-early-pop-recovery"); Next(14); break;
                case 14:
                    if (Time.time - since < 14f) break;
                    Check(hints.RecoveryText == "" && hints.LessonText == "", "Waiting after early Pop produces no optional help");
                    Capture("07-star-without-hints"); Move(Key.D); Next(141); break;
                case 141:
                    if (pos.x < 9.1f) break;
                    Move(Key.Space); Next(15); break;
                case 15:
                    if (body.Form != BodyForm.Bubble) break;
                    Check(hints.LessonText == "", "Repeat Bubble possession does not replay the reminder");
                    loopDistance = 0; loopLast = pos; swimRight = true; Move(Key.D); Next(150); break;
                case 150:
                    loopDistance += Vector2.Distance(pos, loopLast); loopLast = pos;
                    if (pos.x > 10.1f) swimRight = false;
                    if (pos.x < 8.8f) swimRight = true;
                    if (Time.time - since < 14f) { Move(swimRight ? Key.D : Key.A); break; }
                    Check(loopDistance > 4f && hints.LessonText == "" && hints.RecoveryText == "", "Pacing in the pool does not offer hints or repeat abilities");
                    Move(Key.D); Next(151); break;
                case 151:
                    if (pos.x < 15.7f) break;
                    Move(Key.S); Next(16); break;
                case 16:
                    if (pos.y > 1.85f) break;
                    Move(Key.D); Next(17); break;
                case 17:
                    if (pos.x < 17.45f) break;
                    Move(Key.W); Next(18); break;
                case 18:
                    if (pos.y < 9.75f) break;
                    Move(Key.D); Next(19); break;
                case 19:
                    if (pos.x < 19.4f) break;
                    Move(); player.RequestRelease(); Next(20); break;
                case 20:
                    if (body.Form != BodyForm.Box) break;
                    Check(body.name == "BoxB_DropShaft", "Bubble route rejoins at BoxB after Pop");
                    Capture("08-bubble-box-transfer");
                    File.AppendAllText(Output + "/result.txt", "PASS: Bubble route, matching renewable Bubbles and early-pop recovery\n");
                    player = null; hints = null; SceneManager.LoadScene("Level01"); Next(30); break;
                case 30:
                    if (Time.time - since < 1) break;
                    Move(Key.D); Next(31); break;
                case 31:
                    if (body.Form != BodyForm.BouncyBall || pos.x < 12.2f) break;
                    Move(); bounceLow = bounceHigh = pos.y; Next(310); break;
                case 310:
                    bounceLow = Mathf.Min(bounceLow, pos.y); bounceHigh = Mathf.Max(bounceHigh, pos.y);
                    if (Time.time - since < 16f) break;
                    Check(bounceHigh - bounceLow > 2f && hints.LessonText == "", "Repeated bouncing shows no route hints or following text");
                    Capture("12-bouncing-without-hints");
                    File.AppendAllText(Output + "/result.txt", "PASS: no choice card, optional Hint, platform highlight or route arrows while waiting, pacing or bouncing\n");
                    player = null; hints = null; SceneManager.LoadScene("Level01"); Next(35); break;
                case 35:
                    if (Time.time - since < 1) break;
                    Move(Key.D); Next(36); break;
                case 36:
                    if (body.Form != BodyForm.BouncyBall || pos.x < 16.45f) break;
                    Move(); Next(32); break;
                case 32:
                    if (!body.IsInWater || pos.y > 6f) break;
                    Next(320); break;
                case 320:
                    if (Time.time - since < .2f) break;
                    Check(hints.LessonText == "No ground to bounce.\nPress R to restart." && hints.RecoveryText.Contains("Press R"), "Entering the water pocket automatically explains recovery without clicking");
                    Check(GameObject.Find("Close water hint") == null, "Automatic warning has no Hint marker or close button");
                    Next(321); break;
                case 321:
                    if (Time.time - since < 13f) break;
                    Check(hints.RecoveryText.Contains("Press R"), "Pocket warning remains visible without offering an optional hint");
                    CheckNearbyCard(); Capture("09-ball-pocket-recovery");
                    var mobile = new SerializedObject(player.GetComponent<MobileFormControls>());
                    mobile.FindProperty("showMode").enumValueIndex = 1; mobile.ApplyModifiedPropertiesWithoutUndo();
                    Next(33); break;
                case 33:
                    if (Time.time - since < .3f) break;
                    Check(hints.RecoveryText.Contains("Home > Restart Level") && !hints.RecoveryText.Contains("Press R"), "Pocket guidance matches touch controls");
                    Capture("10-touch-pocket-recovery");
                    var desktop = new SerializedObject(player.GetComponent<MobileFormControls>());
                    desktop.FindProperty("showMode").enumValueIndex = 2; desktop.ApplyModifiedPropertiesWithoutUndo();
                    Move(Key.R); Next(34); break;
                case 34:
                    if (Time.timeScale != 0 || GameObject.Find("Current lesson") != null) break;
                    File.AppendAllText(Output + "/result.txt", "PASS: automatic desktop/touch pocket warning; immediate trigger, persistent notice, no optional Hint; pause hides help\nPASS\n");
                    Finish(); break;
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText(Output + "/result.txt", "FAIL: " + ex + "\n");
            Capture("failure"); Debug.LogError("LEVEL01 WATER TEST: " + ex.Message); Finish();
        }
    }
    static void CheckBubbleAppearance()
    {
        var a = GameObject.Find("BubbleU1_WaterEntry"); var b = GameObject.Find("WaterRoute_BackupBubble");
        Check(a != null && b != null, "Both Bubbles exist");
        Check(a.transform.lossyScale == b.transform.lossyScale, "Bubble sizes match");
        Check(Vector2.Distance(a.GetComponent<Collider2D>().bounds.size, b.GetComponent<Collider2D>().bounds.size) < .001f, "Bubble collision sizes match");
        var ar = a.GetComponentsInChildren<SpriteRenderer>(true); var br = b.GetComponentsInChildren<SpriteRenderer>(true);
        Check(ar.Length == br.Length, "Bubble visual layers match");
        var af = a.GetComponent<SpriteRenderer>(); var bf = b.GetComponent<SpriteRenderer>();
        Check(af.sprite == bf.sprite && af.color == bf.color, "Bubble fill and outline match after respawn");
        // Halos and star markers generate separate Sprite objects at runtime.
        var ag = new SerializedObject(a.GetComponent<FormGlowHalo>()); var bg = new SerializedObject(b.GetComponent<FormGlowHalo>());
        Check(ag.FindProperty("haloColor").colorValue == bg.FindProperty("haloColor").colorValue &&
            ag.FindProperty("glowThickness").floatValue == bg.FindProperty("glowThickness").floatValue,
            "Bubble glow color and size match after respawn");
    }
    static void CheckNoOptionalHints()
    {
        var names = new[] { "Water Hint", "Water choice card", "Water route visual cue", "Close water hint" };
        Check(!Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(t => names.Contains(t.name)), "Optional water-hint UI is not created, even while inactive");
    }
    static void CheckNearbyCard()
    {
        var rect = GameObject.Find("Current lesson").GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var box = new Rect(corners[0], corners[2] - corners[0]);
        Vector2 point = Camera.main.WorldToScreenPoint(player.CurrentBody.Body.position);
        Check(!box.Contains(point), "Help card does not cover the player");
        Vector2 nearest = new Vector2(Mathf.Clamp(point.x, box.xMin, box.xMax), Mathf.Clamp(point.y, box.yMin, box.yMax));
        Check(Vector2.Distance(point, nearest) < 180f * rect.GetComponentInParent<Canvas>().scaleFactor, "Help stays near the player rather than at screen top");
    }
    static void Move(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Next(int value) { step = value; since = Time.time; }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Finish() { Move(); EditorApplication.update -= Tick; EditorApplication.ExitPlaymode(); }
    static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main; if (camera == null) return;
        var rt = new RenderTexture(1600, 900, 24); var previous = camera.targetTexture;
        var hud = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.name == "Water lesson HUD");
        if (hud != null) { hud.renderMode = RenderMode.ScreenSpaceCamera; hud.worldCamera = camera; hud.planeDistance = 1; }
        camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); var active = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        camera.targetTexture = previous; RenderTexture.active = active;
        if (hud != null) hud.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
    }
}
