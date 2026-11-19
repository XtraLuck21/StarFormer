using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Run in a disposable project copy with -executeMethod SessionDiscoveryVerification.RunBatch.
// Exercises real scene UI and ownership/reload paths, then a second Play session
// with domain reload disabled. No network tests or release builds are performed.
[InitializeOnLoad]
public static class SessionDiscoveryVerification
{
    private const string Prefix = "StarFormer.DiscoveryVerification.";
    private const string TierKey = "StarFormer.ShapeCloset.UnlockTier.v1";
    private const string GroupsKey = "StarFormer.ShapeCloset.UnlockedGroups.v2";
    private static readonly string[] Forms = { "Box", "Bubble", "BouncyBall", "Spider", "Fan" };
    private static IEnumerator routine;
    private static double resumeAt, deadline;
    private static string errors = "";

    static SessionDiscoveryVerification()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode || Application.companyName != "StarFormerVerification")
            throw new InvalidOperationException("Use a disposable batch project with Company Name StarFormerVerification.");
        SessionState.SetString(Prefix + "Results", "");
        SessionState.SetBool(Prefix + "OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
        SessionState.SetInt(Prefix + "Options", (int)EditorSettings.enterPlayModeOptions);
        SavePreference(TierKey);
        SavePreference(GroupsKey);
        PlayerPrefs.SetInt(TierKey, 2);
        PlayerPrefs.SetInt(GroupsKey, 3);
        // The hardest Editor reset case: static fields survive leaving Play Mode.
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        SessionState.SetInt(Prefix + "Phase", 1);
        EditorSceneManager.OpenScene("Assets/Scenes/Navigation.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunCollectionBatch()
    {
        RunBatch();
        SessionState.SetInt(Prefix + "Phase", 3);
    }

    public static void RunAllScenesCollectionBatch()
    {
        RunBatch();
        SessionState.SetInt(Prefix + "Phase", 5);
    }

    private static IEnumerator VerifyAllScenesCollection()
    {
        var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => File.ReadAllText(path).Contains("Assembly-CSharp::SceneCollectionBookUI"))
            .OrderBy(path => path).ToArray();
        Check(paths.Length == 10, "All ten existing non-Navigation collection entries migrated");
        foreach (string path in paths)
        {
            Time.timeScale = 1;
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            yield return .6f;
            Time.timeScale = 1;
            var bridge = Object.FindFirstObjectByType<SceneCollectionBookUI>();
            Check(bridge != null && bridge.isActiveAndEnabled, path + " bridge enabled");
            var books = Object.FindObjectsByType<HelpBookUI>(FindObjectsSortMode.None);
            Check(books.Count(item => item.isActiveAndEnabled) == 1, path + " has exactly one active collection");
            var book = books.First(item => item.isActiveAndEnabled);
            Check(Field<HelpBookContent>(book, "content") == AssetDatabase.LoadAssetAtPath<HelpBookContent>("Assets/UI/HelpBook/CollectionContent.asset"), path + " uses shared Navigation content");
            Check(Field<bool>(book, "collectionOverview"), path + " uses card overview");
            Check(!Field<GameObject>(bridge, "legacyPanel").activeSelf, path + " old panel retained but hidden");
            var entry = Field<UnityEngine.UI.Button>(bridge, "entryButton");
            Check(entry != null && entry.isActiveAndEnabled && entry.interactable, path + " entry available");
            entry.onClick.Invoke();
            Check(book.IsOpen && Time.timeScale == 0, path + " entry opens collection and pauses game");
            yield return .15f; // Allow the opened overlay to register its rendered UI depth.
            var homeMenu = Object.FindFirstObjectByType<MainMenuButtonUI>();
            if (homeMenu != null)
            {
                var homeCanvas = Field<GameObject>(homeMenu, "hud").GetComponent<Canvas>();
                Check(book.GetComponent<Canvas>().sortingOrder > homeCanvas.sortingOrder, path + " collection renders above Home HUD");
                Canvas.ForceUpdateCanvases();
                var homeRect = (RectTransform)Field<UnityEngine.UI.Button>(homeMenu, "home").transform;
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, homeRect.TransformPoint(homeRect.rect.center))
                };
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(book.transform), path + " collection intercepts clicks over Home button; point=" + pointer.position + "; hits=" + string.Join(", ", hits.Select(hit => hit.gameObject.name + "@" + hit.sortingOrder)));
            }
            HelpBookUnlocks.RecordPossession(BodyForm.Spider);
            var grid = book.transform.Find("Help Overlay/HelpPanel/BookFrame/Collection Overview/Forms");
            Check(grid.childCount == 6, path + " displays all six cards");
            foreach (var page in Field<HelpBookContent>(book, "content").Pages)
            {
                var card = grid.Find(page.unlockId + " Card");
                bool discovered = HelpBookUnlocks.IsUnlocked(page.unlockId);
                Check(card.Find("Form Name").GetComponent<TextMeshProUGUI>().text == (discovered ? page.title.ToUpperInvariant() : "???"), path + " discovery matches: " + page.unlockId);
            }
            Check(grid.Find("Spider Card/Form Icon").GetComponent<UnityEngine.UI.Image>().sprite.texture.name == "CollectionSpiderOrange", path + " orange Spider portrait");
            Check(!book.GetComponentsInChildren<SpriteSequencePlayer>(true).Any(player => player.isActiveAndEnabled), path + " no active demos");
            Field<UnityEngine.UI.Button>(book, "closeButton").onClick.Invoke();
            Check(!book.IsOpen && Time.timeScale == 1, path + " close button resumes game");
            if (homeMenu != null)
                Check(Field<UnityEngine.UI.Button>(homeMenu, "home").gameObject.activeInHierarchy, path + " Home button remains available after closing collection");
            entry.onClick.Invoke();
            Check(book.IsOpen, path + " reopens with discovery preserved");
            Field<UnityEngine.UI.Button>(book, "backdropButton").onClick.Invoke();
            Check(!book.IsOpen && Time.timeScale == 1, path + " backdrop closes collection");
            if (path.EndsWith("IntroBox.unity") || path.EndsWith("Level02.unity"))
                CaptureCollection("shared-" + Path.GetFileNameWithoutExtension(path), 1920, 1080);
            entry.onClick.Invoke();
            bridge.enabled = false;
            Check(!book.IsOpen && Time.timeScale == 1, path + " disabling owner restores time");
        }
        Time.timeScale = 1;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Navigation.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return .5f;
        var navigationBook = Object.FindObjectsByType<HelpBookUI>(FindObjectsSortMode.None).First(book => book.isActiveAndEnabled);
        navigationBook.Open();
        Check(navigationBook.IsOpen && HelpBookUnlocks.IsUnlocked("Spider"), "Navigation shares discoveries from other scenes");
        navigationBook.Close();
    }

    private static IEnumerator VerifyCollection()
    {
        yield return .4f;
        CheckNavigation();
        CaptureCollection("locked", 1920, 1080);
        // Exercise the user's actual path: acquire a body in a level and return to Navigation.
        Load("IntroBox"); yield return .4f;
        Possess(BodyForm.Box);
        Load("Navigation"); yield return .4f;
        CheckNavigation("Box");
        CaptureCollection("partial", 1920, 1080);
        var discovered = new System.Collections.Generic.List<string> { "Box" };
        foreach (string id in Forms.Skip(1))
        {
            var book = Object.FindObjectsByType<HelpBookUI>(FindObjectsSortMode.None).First(item => item.isActiveAndEnabled);
            book.Open();
            HelpBookUnlocks.RecordPossession((BodyForm)Enum.Parse(typeof(BodyForm), id));
            var status = book.transform.Find("Help Overlay/HelpPanel/BookFrame/Collection Overview/Forms/" + id + " Card/Unlock Status");
            Check(status.GetComponent<TextMeshProUGUI>().text == "DISCOVERED", "Open collection refreshes immediately: " + id);
            book.Close();
            discovered.Add(id);
            CheckNavigation(discovered.ToArray());
        }
        Load("Navigation"); yield return .4f;
        CheckNavigation(Forms);
        CaptureCollection("all", 1920, 1080);
        CaptureCollection("all-16x10", 1280, 800);
        Check(true, "Collection discoveries survive scene reload");
    }

    private static IEnumerator VerifyCollectionNewLaunch()
    {
        yield return .4f;
        CheckNavigation();
        Check(true, "New Play session resets discoveries with domain reload disabled");
    }

    private static void CaptureCollection(string name, int width, int height)
    {
        var book = Object.FindObjectsByType<HelpBookUI>(FindObjectsSortMode.None).First(item => item.isActiveAndEnabled);
        book.Open();
        var canvas = book.GetComponent<Canvas>();
        var oldMode = canvas.renderMode;
        var oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        var cameraObject = new GameObject("Collection verification camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -1000);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.18f, .20f, .22f);
        var target = new RenderTexture(width, height, 24);
        camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        var overview = book.transform.Find("Help Overlay/HelpPanel/BookFrame/Collection Overview");
        foreach (var label in overview.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, name + " text fits: " + label.name);
        }
        foreach (var glyph in overview.GetComponentsInChildren<CollectionBookGlyph>())
        {
            Check(glyph.GetComponent<CanvasRenderer>() != null, "Input symbol has renderer: " + glyph.name);
            var mesh = glyph.canvasRenderer.GetMesh();
            Check(mesh.vertexCount > 0, "Input symbol has visible geometry: " + glyph.name);
            Object.Destroy(mesh);
        }
        camera.Render();
        var oldTarget = RenderTexture.active;
        RenderTexture.active = target;
        var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();
        string directory = "/tmp/starformer-collection-previews";
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), screenshot.EncodeToPNG());
        RenderTexture.active = oldTarget;
        canvas.renderMode = oldMode;
        canvas.worldCamera = oldCamera;
        canvas.planeDistance = oldDistance;
        camera.targetTexture = null;
        Object.Destroy(screenshot);
        Object.Destroy(target);
        Object.Destroy(cameraObject);
        book.Close();
    }

    private static void OnPlayState(PlayModeStateChange state)
    {
        int phase = SessionState.GetInt(Prefix + "Phase", 0);
        if (state == PlayModeStateChange.EnteredEditMode && (phase == 2 || phase == 4))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Navigation.unity");
            EditorApplication.EnterPlaymode();
        }
        if (state != PlayModeStateChange.EnteredPlayMode || phase == 0) return;
        errors = "";
        Application.logMessageReceived += OnLog;
        routine = phase == 5 ? VerifyAllScenesCollection() : phase == 4 ? VerifyCollectionNewLaunch() : phase == 3 ? VerifyCollection() : phase == 1 ? VerifySession() : VerifyNewLaunch();
        resumeAt = 0;
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update += Tick;
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase")) return;
        if (message.StartsWith("HelpBookUI 缺少引用"))
        {
            SessionState.SetString(Prefix + "Results", SessionState.GetString(Prefix + "Results", "") +
                "KNOWN EXISTING ISSUE: legacy gameplay Help component has missing button/panel references.\n");
            return;
        }
        errors += message + "\n";
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out");
            if (EditorApplication.timeSinceStartup < resumeAt) return;
            if (routine.MoveNext())
            {
                resumeAt = EditorApplication.timeSinceStartup + (routine.Current is float delay ? delay : 0);
                return;
            }
            Check(errors.Length == 0, "No unexpected runtime errors: " + errors);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            if (SessionState.GetInt(Prefix + "Phase", 0) == 1)
            {
                SessionState.SetInt(Prefix + "Phase", 2);
                EditorApplication.ExitPlaymode();
            }
            else if (SessionState.GetInt(Prefix + "Phase", 0) == 3)
            {
                SessionState.SetInt(Prefix + "Phase", 4);
                EditorApplication.ExitPlaymode();
            }
            else Finish(0, "");
        }
        catch (Exception exception) { Finish(1, exception + "\n" + errors); }
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        SessionState.SetString(Prefix + "Results", SessionState.GetString(Prefix + "Results", "") + "PASS: " + label + "\n");
    }

    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

    private static void Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);

    private static void Expect(params string[] unlocked)
    {
        foreach (string form in Forms)
            Check(HelpBookUnlocks.IsUnlocked(form) == unlocked.Contains(form), "Discovery " + form + " = " + unlocked.Contains(form));
        Check(HelpBookUnlocks.IsUnlocked("") && HelpBookUnlocks.IsUnlocked("Star"), "Star/general help is always available");
        Check(!HelpBookUnlocks.IsUnlocked("BouncingBall"), "Misspelled IDs do not unlock another form");
    }

    private static void Load(string scene)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(scene);
    }

    private static void Possess(BodyForm form)
    {
        var player = Object.FindFirstObjectByType<PlayerPossessionController>();
        var target = Object.FindObjectsByType<PossessableBody>(FindObjectsSortMode.None).First(body => body.Form == form && body.IsReady);
        Time.timeScale = 0;
        // Exercise the real successful ownership handoff, independently of movement timing.
        Invoke(player, "TransferTo", target);
        Check(player.CurrentBody == target && target.IsControlled, "Successful ownership handoff: " + form);
        Check(HelpBookUnlocks.IsUnlocked(form.ToString()), "Ownership unlocks " + form);
    }

    private static void CheckNavigation(params string[] unlocked)
    {
        Expect(unlocked);
        var slots = Object.FindObjectsByType<ShapeClosetSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Check(slots.Length == 5, "Exactly five discovery-driven Closet slots");
        foreach (var slot in slots)
        {
            string id = Field<string>(slot, "unlockId");
            bool expected = unlocked.Contains(id);
            Check(slot.isActiveAndEnabled, id + " slot root stays active");
            Check(Field<GameObject>(slot, "unlockedVisual").activeSelf == expected, id + " Closet icon matches discovery");
            Check(Field<GameObject[]>(slot, "lockedVisuals").All(obj => obj.activeSelf == !expected), id + " locked artwork matches discovery");
        }
        var book = Object.FindObjectsByType<HelpBookUI>(FindObjectsSortMode.None).First(item => item.isActiveAndEnabled);
        Check(book.PageCount == 6, "Navigation book keeps all six pages");
        var content = Field<HelpBookContent>(book, "content");
        book.Open();
        Check(book.IsOpen, "Navigation book opens");
        var overview = book.transform.Find("Help Overlay/HelpPanel/BookFrame/Collection Overview");
        Check(overview != null, "Collection overview exists");
        var grid = overview.Find("Forms");
        Check(grid.childCount == 6, "All six forms visible together");
        Check(grid.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length == 0, "Static cards contain no practice or activation buttons");
        foreach (var page in content.Pages)
        {
            bool expected = page.unlockId == "Star" || unlocked.Contains(page.unlockId);
            var card = grid.Find(page.unlockId + " Card");
            Check(card.gameObject.activeInHierarchy, "Card visible: " + page.title);
            Check(card.Find("Unlock Status").GetComponent<TextMeshProUGUI>().text == (expected ? "DISCOVERED" : "UNDISCOVERED"), "Card discovery status: " + page.title);
            Check(page.demoFrames.Length == 0, "Collection has no demo frames: " + page.title);
            Check(card.Find("Form Name").GetComponent<TextMeshProUGUI>().text == (expected ? page.title.ToUpperInvariant() : "???"), "Title reveals only discovered form: " + page.title);
            Check(card.Find("Controls").gameObject.activeSelf == expected, "Controls reveal only discovered form: " + page.title);
            Check(card.Find("Locked Hint").gameObject.activeSelf != expected, "Mystery hint matches lock: " + page.title);
            if (!expected)
            {
                var mystery = card.Find("Form Icon").GetComponent<UnityEngine.UI.Image>().sprite;
                Check(mystery != null && mystery.texture.name == "CollectionForms", "Locked form uses mystery artwork");
                Check(card.Find("Footer/Flavor").GetComponent<TextMeshProUGUI>().text == "Find a new form to reveal it.", "Locked card hides flavor text");
            }

        }
        Check(!book.GetComponentsInChildren<SpriteSequencePlayer>(true).Any(player => player.isActiveAndEnabled), "No active collection animations");
        Canvas.ForceUpdateCanvases();
        foreach (var label in overview.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, "Collection text fits: " + label.name);
        }
        Check(overview.Find("Discovery Count").GetComponent<TextMeshProUGUI>().text == $"{unlocked.Length + 1} / 6 forms discovered", "Collection count matches discovery");
        book.Close();
        Check(Time.timeScale == 1, "Book closes and resumes navigation");
    }

    private static IEnumerator VerifySession()
    {
        yield return .4f;
        CheckNavigation(); // Seeded historical PlayerPrefs must have no effect.
        Check(PlayerPrefs.GetInt(GroupsKey) == 3 && PlayerPrefs.GetInt(TierKey) == 2, "Historical saves ignored, not rewritten or cleared");

        Load("Level02"); yield return .25f;
        Expect();
        var player = Object.FindFirstObjectByType<PlayerPossessionController>();
        Check(player.CurrentBody.Form == BodyForm.Star, "Direct Level02 entry controls Star only");
        var goal = Object.FindFirstObjectByType<LevelGoalToMenu>();
        Invoke(goal, "CheckPlayer", player.CurrentBody.BodyCollider);
        Check(Field<bool>(goal, "cleared"), "Level02 completion handler exercised");
        Expect(); // Completing the advanced level no longer grants all forms.
        Load("Navigation"); yield return .25f; CheckNavigation();

        Load("IntroBox"); yield return .25f; Expect();
        Load("Navigation"); yield return .25f; CheckNavigation();
        Load("IntroBox"); yield return .25f;
        Possess(BodyForm.Box); Expect("Box");
        Time.timeScale = 1;
        player = Object.FindFirstObjectByType<PlayerPossessionController>();
        Check(player.ConfirmManualRestart(true), "Restart Level accepted");
        yield return .4f; Expect("Box");
        player = Object.FindFirstObjectByType<PlayerPossessionController>();
        player.SetCheckpoint("DISCOVERY_TEST", player.CurrentBody.transform.position, false);
        Check(player.ConfirmManualRestart(false), "Checkpoint retry accepted");
        yield return .4f; Expect("Box");
        player = Object.FindFirstObjectByType<PlayerPossessionController>();
        player.KillControlledBody(player.CurrentBody, AnalyticsDeathCause.Unknown, .1f);
        yield return .5f;
        Check(!Object.FindFirstObjectByType<PlayerPossessionController>().IsDead, "Automatic death recovery completed");
        Expect("Box");
        PlayerPossessionController.ResetCheckpointSession();
        Load("Navigation"); yield return .25f; CheckNavigation("Box");

        Load("IntroBubble"); yield return .3f;
        Check(Object.FindFirstObjectByType<PlayerPossessionController>().CurrentBody.Form == BodyForm.Box, "Starting host owns Box without discovering waiting Bubble");
        Expect("Box");
        Possess(BodyForm.Bubble); Expect("Box", "Bubble");
        Load("Navigation"); yield return .25f; CheckNavigation("Box", "Bubble");
        Load("IntroBouncyBall"); yield return .25f; Possess(BodyForm.BouncyBall);
        Load("IntroSpider"); yield return .3f; Possess(BodyForm.Spider);
        Expect("Box", "Bubble", "BouncyBall", "Spider");
        Load("IntroFan"); yield return .3f;
        while (Object.FindFirstObjectByType<PlayerPossessionController>().CurrentBody == null ||
            !Object.FindFirstObjectByType<PlayerPossessionController>().enabled) yield return .1f;
        Possess(BodyForm.Fan);
        Load("Navigation"); yield return .3f; CheckNavigation(Forms);
        Check(PlayerPrefs.GetInt(GroupsKey) == 3 && PlayerPrefs.GetInt(TierKey) == 2, "Possession and completion never write historical save keys");
    }

    private static IEnumerator VerifyNewLaunch()
    {
        yield return .4f;
        CheckNavigation();
        Check(true, "New Play session clears all discoveries even with domain reload disabled");
        Load("IntroSpider"); yield return .3f; Possess(BodyForm.Spider);
        Load("IntroFan"); yield return .3f;
        while (Object.FindFirstObjectByType<PlayerPossessionController>().CurrentBody == null ||
            !Object.FindFirstObjectByType<PlayerPossessionController>().enabled) yield return .1f;
        Possess(BodyForm.Fan);
        Load("Navigation"); yield return .3f;
        CheckNavigation("Spider", "Fan");
        Check(true, "Skipping basic school reveals only the two forms actually controlled");
        Load("IntroBubble"); yield return .3f;
        Expect("Box", "Spider", "Fan");
        Check(true, "Assigned starting Box counts as used but waiting Bubble stays locked");
        Load("Navigation"); yield return .3f; CheckNavigation("Box", "Spider", "Fan");
        var book = Object.FindObjectsByType<HelpBookUI>(FindObjectsSortMode.None).First(item => item.isActiveAndEnabled);
        book.Open();
        HelpBookUnlocks.RecordPossession(BodyForm.Bubble);
        var status = book.transform.Find("Help Overlay/HelpPanel/BookFrame/Collection Overview/Forms/Bubble Card/Unlock Status");
        Check(status.GetComponent<TextMeshProUGUI>().text == "DISCOVERED", "Open collection updates immediately on discovery");
        book.Close();
        CheckNavigation("Box", "Bubble", "Spider", "Fan");
    }

    private static void SavePreference(string key)
    {
        SessionState.SetBool(Prefix + key + ".Exists", PlayerPrefs.HasKey(key));
        SessionState.SetInt(Prefix + key, PlayerPrefs.GetInt(key));
    }

    private static void Finish(int code, string failure)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        SessionState.SetInt(Prefix + "Phase", 0);
        foreach (string key in new[] { TierKey, GroupsKey })
        {
            if (SessionState.GetBool(Prefix + key + ".Exists", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Prefix + key, 0));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Prefix + "OptionsEnabled", false);
        EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Prefix + "Options", 0);
        string report = SessionState.GetString(Prefix + "Results", "") + (code == 0 ? "PASS: session discovery regression suite\n" : "FAIL: " + failure);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "starformer-session-discovery-results.txt"), report);
        Debug.Log(report);
        EditorApplication.Exit(code);
    }
}
