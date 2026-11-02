using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Offline lifecycle verification: Editor-only logs, never POSTs to Google.
public static class AnalyticsVerification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunAllBatch()
    {
        AnalyticsMapExporter.ExportAll();
        VerifyLifecycle();
    }

    private static void Set(AnalyticsManager manager, string field, object value) =>
        typeof(AnalyticsManager).GetField(field, Private).SetValue(manager, value);

    private static void VerifyLifecycle()
    {
        AnalyticsSettings settings = Resources.Load<AnalyticsSettings>("AnalyticsSettings");
        AnalyticsMapCatalog maps = JsonUtility.FromJson<AnalyticsMapCatalog>(
            Resources.Load<TextAsset>("AnalyticsMapCatalog").text);
        var events = new List<AnalyticsEvent>();
        Application.LogCallback collect = (message, stack, type) =>
        {
            if (message.StartsWith("[Analytics] "))
                events.Add(JsonUtility.FromJson<AnalyticsEvent>(message.Substring(12)));
        };
        Application.logMessageReceived += collect;
        var managerObject = new GameObject("Offline analytics verification");
        AnalyticsManager manager = managerObject.AddComponent<AnalyticsManager>();
        Set(manager, "settings", settings);
        Set(manager, "playerId", "offline-verification-player");
        Set(manager, "sessionId", "offline-verification-session");
        Set(manager, "mapCatalog", maps);
        var scenes = new List<Scene>();
        try
        {
            foreach (AnalyticsMapEntry map in maps.maps)
            {
                Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + map.level_name + ".unity");
                scenes.Add(scene);
                var bodyObject = new GameObject("Offline test body");
                SceneManager.MoveGameObjectToScene(bodyObject, scene);
                PossessableBody body = bodyObject.AddComponent<PossessableBody>();
                manager.BeginLevel(body, "START");
                string run = events.Last(item => item.event_type == "level_start").level_run_id;
                Set(manager, "levelActiveSeconds", 4f);
                Set(manager, "attemptActiveSeconds", 4f);
                Set(manager, "currentFormActiveSeconds", 4f);
                manager.TrackDeath(body, AnalyticsDeathCause.Other);
                manager.BeginLevel(body, "START");
                AnalyticsEvent respawn = events.Last();
                Require(respawn.event_type == "attempt_start" && respawn.start_reason == "death_respawn" &&
                    respawn.level_run_id == run, "Death respawn did not retain run in " + map.level_name);
                manager.TrackLevelComplete(body);
                manager.TrackLevelComplete(body);
                Require(events.Count(item => item.level_name == map.level_name && item.event_type == "level_complete") == 1,
                    "Completion emitted more than once in " + map.level_name);
                Require(events.Where(item => item.level_name == map.level_name).All(item =>
                    item.schema_version == 2 && item.map_version == map.map_version), "Missing map metadata in " + map.level_name);
            }

            // Checkpoint travel time uses the cumulative level clock across death retries.
            Scene level = scenes.Find(scene => scene.name == "Level01");
            PossessableBody levelBody = level.GetRootGameObjects().First(go => go.name == "Offline test body").GetComponent<PossessableBody>();
            manager.BeginLevel(levelBody, "START");
            Set(manager, "levelActiveSeconds", 10f);
            Set(manager, "attemptActiveSeconds", 10f);
            manager.TrackCheckpoint("offline-CP1", levelBody);
            AnalyticsEvent firstCheckpoint = events.Last(item => item.event_type == "checkpoint_reached");
            Set(manager, "levelActiveSeconds", 25f);
            Set(manager, "attemptActiveSeconds", 15f);
            Set(manager, "currentFormActiveSeconds", 15f);
            manager.TrackDeath(levelBody, AnalyticsDeathCause.Other);
            manager.BeginLevel(levelBody, "offline-CP1");
            Require(events.Last().level_active_seconds == 25f && events.Last().attempt_active_seconds == 0f,
                "Respawn reset the cumulative level clock.");
            Set(manager, "levelActiveSeconds", 40f);
            Set(manager, "attemptActiveSeconds", 15f);
            manager.TrackCheckpoint("offline-CP2", levelBody);
            AnalyticsEvent secondCheckpoint = events.Last(item => item.event_type == "checkpoint_reached");
            Require(secondCheckpoint.level_active_seconds - firstCheckpoint.level_active_seconds == 30f,
                "Checkpoint travel time lost a failed attempt.");
            Set(manager, "currentFormActiveSeconds", 3f);
            typeof(AnalyticsManager).GetMethod("OnApplicationFocus", Private).Invoke(manager, new object[] { false });
            Require(events.Any(item => item.event_type == "form_duration" && item.end_reason == "focus_lost" && item.duration_seconds == 3f),
                "Focus loss did not flush the open form segment.");
            manager.TrackLevelComplete(levelBody);

            // Returning from IntroBubble is not a completion or abandonment.
            Scene bubble = scenes.Find(scene => scene.name == "IntroBubble");
            Scene box = scenes.Find(scene => scene.name == "IntroBox");
            PossessableBody bubbleBody = bubble.GetRootGameObjects().First(go => go.name == "Offline test body").GetComponent<PossessableBody>();
            PossessableBody boxBody = box.GetRootGameObjects().First(go => go.name == "Offline test body").GetComponent<PossessableBody>();
            manager.BeginLevel(bubbleBody, "START");
            manager.TrackSceneReturn("IntroBox");
            Require(events.Last().result == "scene_return", "Return exit was not classified as a scene return.");
            manager.BeginLevel(boxBody, "START");
            Require(events.Last().entry_reason == "return", "Destination did not retain return entry reason.");
            manager.TrackManualRestart("retry_checkpoint");
            Require(events.Last().end_reason == "retry_checkpoint", "Manual restart reason missing.");
            manager.BeginLevel(boxBody, "START");
            Require(events.Last().entry_reason == "normal", "Fresh restart inherited return metadata.");

            File.WriteAllText("Library/AnalyticsVerification.json", "{\"status\":\"passed\",\"scenes\":" + maps.maps.Length + ",\"events\":" + events.Count + "}");
            Debug.Log("[Analytics verification] PASS: all tracked scenes, map metadata, death respawn, once-only completion, return navigation and restart classification.");
        }
        finally
        {
            Application.logMessageReceived -= collect;
            foreach (Scene scene in scenes) EditorSceneManager.ClosePreviewScene(scene);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
