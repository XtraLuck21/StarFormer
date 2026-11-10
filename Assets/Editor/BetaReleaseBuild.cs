using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Release preparation is local only. This never pushes or deploys a website.
public static class BetaReleaseBuild
{
    private static readonly string[] ExpectedScenes = {
        "Navigation", "IntroBox", "IntroBubble", "IntroBouncyBall",
        "Level01", "IntroSpider", "IntroFan", "Level02"
    };

    [Serializable] private sealed class SceneCheck
    {
        public string name, path, guid;
        public int missingScripts;
    }

    [Serializable] private sealed class Verification
    {
        public string unityVersion, playerVersion, analyticsVersion;
        public SceneCheck[] scenes;
    }

    public static void ExportMapsAndVerify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before release preparation.");
        var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        var names = enabled.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
        Require(names.SequenceEqual(ExpectedScenes), "Enabled build scene order differs from the release scene list.");
        var settings = Resources.Load<AnalyticsSettings>("AnalyticsSettings");
        Require(settings != null && settings.CollectionEnabled, "Production analytics configuration is missing or disabled.");
        string settingsText = File.ReadAllText("Assets/Resources/AnalyticsSettings.asset");
        Require(settingsText.Contains("buildVersion: " + PlayerSettings.bundleVersion), "Player/analytics versions differ.");
        Require(settingsText.Contains("environment: production"), "Analytics environment changed.");

        var checks = new List<SceneCheck>();
        foreach (var item in enabled)
        {
            Require(File.Exists(item.path), "Build scene missing: " + item.path);
            string name = Path.GetFileNameWithoutExtension(item.path);
            string guid = AssetDatabase.AssetPathToGUID(item.path);
            Require(!string.IsNullOrEmpty(guid) && item.guid.ToString() == guid,
                "Build-list GUID disagrees with scene metadata: " + item.path);
            if (name == "Level02")
                Require(guid == "7c566eddba60a82469938fe898ff5d22", "Level02 is not the promoted replacement scene.");
            var preview = EditorSceneManager.OpenPreviewScene(item.path);
            try
            {
                var objects = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                int missing = objects.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                Require(missing == 0, name + " has " + missing + " missing MonoBehaviour scripts.");
                if (name == "Navigation")
                {
                    var entrances = objects.Select(t => t.GetComponent<NavigationEntrance>()).Where(c => c != null);
                    Require(entrances.Any(c => new SerializedObject(c).FindProperty("destinationScene").stringValue == "Level02"),
                        "Navigation has no Level02 destination.");
                }
                if (name == "Level01")
                    Require(objects.Any(t => t.GetComponent<AdvancedCheckpointController>() != null), "Promoted Level01 checkpoint system missing.");
                if (name == "Level02")
                {
                    var route = objects.Select(t => t.GetComponent<SmileFaceFinalRoute>()).FirstOrDefault(c => c != null);
                    Require(route != null, "Replacement Level02 Smile route missing.");
                    foreach (string field in new[] { "player", "spider", "teeth", "expression", "roomCamera", "goal", "button", "doorGate", "foreheadRecoveryZone", "finalBox" })
                        Require(new SerializedObject(route).FindProperty(field).objectReferenceValue != null, "Smile route reference missing: " + field);
                    var supply = objects.Select(t => t.GetComponent<SeesawRoomSupplies>()).FirstOrDefault(c => c != null);
                    Require(supply != null && supply.ReferencesReady, "Seesaw supply references missing.");
                    var teeth = objects.Select(t => t.GetComponent<TeethTriggeredDoor>()).FirstOrDefault(c => c != null);
                    Require(teeth != null && new SerializedObject(teeth).FindProperty("requiredTriggers").arraySize == 3,
                        "Smile teeth do not have three arrival triggers.");
                }
                checks.Add(new SceneCheck { name = name, path = item.path, guid = guid, missingScripts = missing });
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/BetaReleaseSceneVerification.json", JsonUtility.ToJson(new Verification {
            unityVersion = Application.unityVersion, playerVersion = PlayerSettings.bundleVersion,
            analyticsVersion = PlayerSettings.bundleVersion, scenes = checks.ToArray()
        }, true));
        AnalyticsMapExporter.ExportAll();
        Debug.Log("BETA RELEASE VERIFICATION PASS: eight enabled scenes, valid scene identities/references, no missing scripts, seven current analytics maps.");
    }

    public static void Build()
    {
        ExportMapsAndVerify();
        WebGLBuild.Build();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
