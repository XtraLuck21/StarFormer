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

    private static void OnPlayState(PlayModeStateChange state)
    {
        int phase = SessionState.GetInt(Prefix + "Phase", 0);
        if (state == PlayModeStateChange.EnteredEditMode && phase == 2)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Navigation.unity");
            EditorApplication.EnterPlaymode();
        }
        if (state != PlayModeStateChange.EnteredPlayMode || phase == 0) return;
        errors = "";
        Application.logMessageReceived += OnLog;
        routine = phase == 1 ? VerifySession() : VerifyNewLaunch();
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
        var book = Object.FindFirstObjectByType<HelpBookUI>();
        Check(book.PageCount == 6, "Navigation book keeps all six pages");
        var content = Field<HelpBookContent>(book, "content");
        book.Open();
        Check(book.IsOpen, "Navigation book opens");
        for (int i = 0; i < content.Pages.Length; i++)
        {
            var page = content.Pages[i];
            book.GoToPage(i);
            bool expected = i == 0 || unlocked.Contains(page.unlockId);
            Check(Field<TextMeshProUGUI>(book, "pageTitleText").text == (expected ? page.title : Field<string>(book, "lockedTitle")), "Book title gated: " + page.title);
            Check(Field<SpriteSequencePlayer>(book, "demoPlayer").gameObject.activeSelf == expected, "Book demo gated: " + page.title);
        }
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
