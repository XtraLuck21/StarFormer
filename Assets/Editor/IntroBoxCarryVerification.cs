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

// Explicit Play Mode verification using real input and physics.
[InitializeOnLoad]
public static class IntroBoxCarryVerification
{
    const string KeyName = "IntroBoxCarryVerification.Running";
    static Keyboard keyboard;
    static IntroBoxCarryLesson lesson;
    static PlayerPossessionController player;
    static int step;
    static bool questionCaptured;
    static Vector3 shrinkStation, growStation, dockText;
    static Vector2 parkedCargo;
    static float dockX;
    static int dockScene;
    static float since;
    static double deadline, reportAt, pauseUntil;
    static int previousHandle;

    static IntroBoxCarryVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("IntroBox.Verify.Live", false))
            {
                EditorApplication.update -= Tick;
                SessionState.SetBool(KeyName, false);
                SessionState.SetBool("IntroBox.Verify.Live", false);
                Application.runInBackground = SessionState.GetBool("IntroBox.Verify.RunBackground", false);
                InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)
                    SessionState.GetInt("IntroBox.Verify.Input", 0);
                InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)
                    SessionState.GetInt("IntroBox.Verify.Background", 0);
            }
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(KeyName, false)) return;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            keyboard = InputSystem.AddDevice<Keyboard>();
            deadline = EditorApplication.timeSinceStartup + 150;
            EditorApplication.update += Tick;
        };
    }
    [MenuItem("Tools/IntroBox/Verify Current Scene in Play Mode")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/IntroBox.unity")
            throw new InvalidOperationException("Open IntroBox first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetInt("IntroBox.Verify.Input", (int)InputSystem.settings.editorInputBehaviorInPlayMode);
        SessionState.SetInt("IntroBox.Verify.Background", (int)InputSystem.settings.backgroundBehavior);
        SessionState.SetBool("IntroBox.Verify.RunBackground", Application.runInBackground);
        SessionState.SetBool("IntroBox.Verify.Live", true);
        SessionState.SetBool(KeyName, true);
        EditorApplication.EnterPlaymode();
    }
    public static void RunBatch()
    {
        if (!Application.isBatchMode || Application.companyName != "StarFormerVerification")
            throw new InvalidOperationException("Use the disposable verification project.");
        EditorSceneManager.OpenScene("Assets/Scenes/IntroBox.unity");
        SessionState.SetBool(KeyName, true);
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at step " + step);
            if (player == null) player = Object.FindFirstObjectByType<PlayerPossessionController>();
            if (lesson == null) lesson = Object.FindFirstObjectByType<IntroBoxCarryLesson>();
            if (player == null || player.CurrentBody == null) return;
            if (EditorApplication.timeSinceStartup > reportAt)
            {
                reportAt = EditorApplication.timeSinceStartup + 3;
                Debug.Log("CARRY TEST step=" + step + " body=" + player.CurrentBody.name + " pos=" + player.CurrentBody.Body.position +
                    " stage=" + lesson?.LessonStage + " C=" + lesson?.cargo?.transform.position + " recovery=" + lesson?.RecoveryCount);
            }
            Check(!player.IsDead, "No deaths on the tutorial path");
            if (step < 20)
                Check(Time.timeScale == 1 && player.enabled, "Camera guidance never pauses or disables movement");
            if (lesson != null && lesson.LessonStage == "Carry" && !questionCaptured)
            {
                Check(lesson.hint.text.Contains("box above you"), "The question follows the camera reveal");
                Capture("03-camera-question"); questionCaptured = true;
            }
            switch (step)
            {
                case 0:
                    if (Time.time < .7f) break;
                    Check(player.CurrentBody.Form == BodyForm.Star, "Starts as Star");
                    Check(player.CurrentBody.Body.position.x < -18, "Opening has a longer walking runway");
                    Check(lesson.moveKeys.gameObject.activeSelf && !lesson.jumpKey.gameObject.activeSelf,
                        "Move keycaps appear before the jump prompt");
                    Check(lesson.welcomeSign != null && lesson.welcomeSign.activeInHierarchy,
                        "First possession sign is visible from the beginning");
                    Capture("01-opening"); Move(Key.D); Next(1); break;
                case 1:
                    if (player.CurrentBody.Body.position.x < -13.15f) break;
                    Check(!lesson.moveKeys.gameObject.activeSelf && lesson.jumpKey.gameObject.activeSelf,
                        "Jump keycap replaces movement practice near the step");
                    Capture("01-jump"); Move(Key.D, Key.Space); Next(2); break;
                case 2:
                    if (Time.time - since < .12f) break;
                    Move(Key.D); Next(3); break;
                case 3:
                    if (player.CurrentBody == lesson.boxA)
                    {
                        Move(Key.D); Capture("02-first-possession"); Next(4);
                    }
                    else Check(player.CurrentBody.Form == BodyForm.Star && player.CurrentBody.Body.position.x < -7,
                        "Star cannot bypass A or reach C");
                    if (player.CurrentBody.Form == BodyForm.Star && player.CurrentBody.Body.position.x > -10.9f)
                        Move(((int)(Time.time * 8) % 2 == 0) ? new[] { Key.D, Key.Space } : new[] { Key.D });
                    break;
                case 4:
                    if (player.CurrentBody != lesson.carrier) break;
                    Check(!lesson.cargo.IsControlled, "C is already touching B and does not chain-possess");
                    Next(5); break;
                case 5:
                    Check(player.CurrentBody == lesson.carrier, "B retains ownership while carrying C");
                    if (lesson.carrier.Body.position.x < -.8f) break;
                    Check(lesson.RevealElapsed >= .65f && lesson.ledgeGuide.activeInHierarchy,
                        "The camera reveal exposes a graphical landing destination");
                    Check(Mathf.Abs(lesson.cargo.Body.position.x - lesson.carrier.Body.position.x - .85f) < .2f,
                        "C rides B without slipping");
                    Capture("03-carrying"); Move(Key.A); Next(6); break;
                case 6:
                    if (Time.time - since < .35f) break;
                    Check(player.CurrentBody == lesson.carrier && lesson.RecoveryCount == 0, "Reverse movement preserves the stack");
                    Move(Key.D); Next(7); break;
                case 7:
                    if (lesson.carrier.BodyCollider.bounds.max.x < 3.95f) break;
                    Move(); Capture("04-dock"); Next(8); break;
                case 8:
                    if (Time.time - since < .3f || lesson.LessonStage != "Separate") break;
                    Check(questionCaptured, "The reveal presented the question without stopping play");
                    var landingViewport = lesson.lessonCamera.WorldToViewportPoint(new Vector3(3.85f, -.55f, 0));
                    Check(landingViewport.x > 0 && landingViewport.x < 1, "The camera shows the ledge destination");
                    Check(player.CurrentBody == lesson.carrier && lesson.RecoveryCount == 0, "Dock retains B ownership");
                    Check(GuideVisible(lesson.dockShrink), "Shrink keycap and outline are visible at the dock");
                    Capture("04-dock");
                    Move(Key.K); Next(9); break;
                case 9:
                    if (!lesson.Separated) break;
                    Check(lesson.Docked && player.CurrentBody == lesson.carrier, "C stays on ledge during genuine separation");
                    Move(); Capture("05-separated"); Next(10); break;
                case 10:
                    if (Time.time - since < .25f) break;
                    Check(GuideVisible(lesson.reconnectGrow), "Grow keycap and outline are visible after separation");
                    Capture("05-separated");
                    shrinkStation = lesson.dockShrink.transform.position;
                    growStation = lesson.reconnectGrow.transform.position;
                    dockText = lesson.hintPanel.position;
                    parkedCargo = lesson.cargo.Body.position;
                    dockX = lesson.carrier.Body.position.x;
                    dockScene = SceneManager.GetActiveScene().handle;
                    Move(Key.A); Next(30); break;
                case 30:
                    if (Time.time - since < 1.25f) break;
                    Check(SceneManager.GetActiveScene().handle == dockScene && lesson.RecoveryCount == 0,
                        "Walking left after separation neither resets the lesson nor changes scenes");
                    Check(player.CurrentBody == lesson.carrier && lesson.Docked && lesson.Separated,
                        "Carrier retains control and dock progress while retreating");
                    Check(Vector2.Distance(parkedCargo, lesson.cargo.Body.position) < .08f,
                        "The parked passenger stays on the platform");
                    Check(lesson.dockShrink.transform.position == shrinkStation &&
                        lesson.reconnectGrow.transform.position == growStation && lesson.hintPanel.position == dockText,
                        "Resize guides and text remain fixed at the dock");
                    Check(!GuideVisible(lesson.reconnectGrow), "Leaving the dock hides its resize guide");
                    Capture("05-retreat-no-reset"); Move(Key.D); Next(31); break;
                case 31:
                    if (lesson.carrier.Body.position.x < dockX - .05f) break;
                    Move(); Next(32); break;
                case 32:
                    if (Time.time - since < .2f) break;
                    Check(GuideVisible(lesson.reconnectGrow), "Returning to the dock restores its fixed grow guide");
                    Debug.Log("CARRY TEST PASS: separated retreat, fixed hints, parked passenger and return");
                    Move(Key.J); Next(11); break;
                case 11:
                    if (player.CurrentBody != lesson.cargo) break;
                    Move(); Capture("06-reconnected"); Next(12); break;
                case 12:
                    if (Time.time - since < .4f) break;
                    Check(player.CurrentBody == lesson.cargo, "No ping-pong after reconnecting");
                    Check(GuideVisible(lesson.passageShrink), "Small-body passage shrink diagram is visible");
                    Capture("06-reconnected");
                    Move(Key.K); Next(13); break;
                case 13:
                    if (lesson.cargo.BodyCollider.bounds.size.y > .49f) break;
                    Move(Key.D); Next(14); break;
                case 14:
                    if (SceneManager.GetActiveScene().name == "IntroBubble")
                    {
                        Debug.Log("CARRY TEST PASS: full route with camera guidance and no forced pauses");
                        Move(); Next(15);
                    }
                    break;
                case 15:
                    if (Time.time - since < .7f) break;
                    var back = Object.FindObjectsByType<IntroSceneExit>(FindObjectsSortMode.None)
                        .First(e => new SerializedObject(e).FindProperty("returnToPreviousExit").boolValue);
                    player.CurrentBody.Body.position = back.transform.position;
                    player.CurrentBody.transform.position = back.transform.position;
                    Physics2D.SyncTransforms(); Next(16); break;
                case 16:
                    if (SceneManager.GetActiveScene().name != "IntroBox") break;
                    Next(17); break;
                case 17:
                    if (Time.time - since < .4f) break;
                    Check(player.CurrentBody == lesson.cargo && lesson.flag.IsOpen, "Return from Bubble restores C at exit with gate open");
                    previousHandle = SceneManager.GetActiveScene().handle;
                    SceneManager.LoadScene("IntroBox"); Next(18); break;
                case 18:
                    if (SceneManager.GetActiveScene().handle == previousHandle || Time.time - since < .4f) break;
                    Check(player.CurrentBody.Form == BodyForm.Star && !lesson.flag.IsOpen, "Full reload resets to Star and closes gate");
                    player.TryRestoreLessonBody(lesson.carrier);
                    Move(Key.K); Next(19); break;
                case 19:
                    if (lesson.RecoveryCount < 1) break;
                    Move(); Next(20); break;
                case 20:
                    if (Time.time - since < .6f) break;
                    Check(player.CurrentBody == lesson.carrier && lesson.cargo.IsGrounded(), "Premature shrink recovers to a supported stack");
                    Check(lesson.RecoveryCount == 1, "Recovery does not loop or chain-possess C");
                    Capture("07-recovery");
                    Debug.Log("CARRY TEST PASS: return, reload, premature-shrink recovery");
                    Time.timeScale = 0; pauseUntil = EditorApplication.timeSinceStartup + .2; Next(21); break;
                case 21:
                    if (EditorApplication.timeSinceStartup < pauseUntil) break;
                    Check(!lesson.hintPanel.gameObject.activeSelf, "Pause hides local teaching callout");
                    Time.timeScale = 1;
                    var controls = new SerializedObject(player.GetComponent<MobileFormControls>());
                    controls.FindProperty("showMode").enumValueIndex = 1;
                    controls.ApplyModifiedPropertiesWithoutUndo();
                    Next(22); break;
                case 22:
                    if (Time.time - since < 2.2f) break;
                    Check(player.GetComponent<MobileFormControls>().MobileMode, "Touch mode can be previewed");
                    Check(lesson.jumpKey != null && lesson.moveKeys != null, "Original visual control guides are assigned");
                    Check(lesson.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t => t.name != "Body Letter" && t.name != "Move Key Label"),
                        "No designer letters or plain-text keyboard labels");
                    Capture("08-touch");
                    Debug.Log("CARRY TEST PASS: jump-bypass attempts, pause and visual touch controls");
                    Finish(0); break;
            }
        }
        catch (Exception ex) { Debug.LogError("CARRY TEST FAIL: " + ex); Capture("failure"); Finish(1); }
    }
    static bool GuideVisible(BoxGrowHint guide) => guide != null && guide.gameObject.activeInHierarchy &&
        guide.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.gameObject.activeInHierarchy);
    static void Move(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Next(int value) { step = value; since = Time.time; }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Capture(string name)
    {
        var camera = Camera.main; if (camera == null) return;
        Directory.CreateDirectory("/private/tmp/introbox-captures");
        var rt = new RenderTexture(1600, 900, 24);
        var previous = camera.targetTexture;
        camera.targetTexture = rt; camera.Render();
        var active = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes("/private/tmp/introbox-captures/" + name + ".png", image.EncodeToPNG());
        camera.targetTexture = previous; RenderTexture.active = active;
        Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
    }
    static void Finish(int code)
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        EditorApplication.update -= Tick; SessionState.SetBool(KeyName, false);
        Time.timeScale = 1;
        if (SessionState.GetBool("IntroBox.Verify.Live", false)) EditorApplication.ExitPlaymode();
        else EditorApplication.Exit(code);
    }
}
