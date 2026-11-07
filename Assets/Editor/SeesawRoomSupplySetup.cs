using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Authors the supply anchors and prefab variants; also supports a headless configuration pass.</summary>
public static class SeesawRoomSupplySetup
{
    private const string FanPath = "Assets/Form/SeesawRoomFan.prefab";
    private const string BallPath = "Assets/Form/SeesawRoomBall.prefab";

    [MenuItem("Tools/Seesaw Room/Configure Supplies")]
    public static void Configure()
    {
        StarRoomDoor door = UnityEngine.Object.FindFirstObjectByType<StarRoomDoor>();
        if (door == null) throw new InvalidOperationException("Open NewLevel02 with its StarRoomDoor before configuring supplies.");
        var doorData = new SerializedObject(door);
        Collider2D board = doorData.FindProperty("arrivalSurface").objectReferenceValue as Collider2D;
        PlayerPossessionController player = doorData.FindProperty("player").objectReferenceValue as PlayerPossessionController;
        if (board == null || player == null) throw new InvalidOperationException("The door must reference the board and player.");
        if (doorData.FindProperty("roomSupplies").objectReferenceValue != null)
        {
            Validate();
            return;
        }
        Physics2D.SyncTransforms();
        Bounds bounds = board.bounds;
        var savedPositions = new Dictionary<Transform, Vector3>();
        foreach (GameObject root in door.gameObject.scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) savedPositions[child] = child.position;

        PossessableBody fanPrefab = CreateFanVariant(Mathf.Max(18f, bounds.extents.x + 3f));
        WindReleasedBall ballPrefab = CreateBallVariant();
        Transform roomRoot = board.transform.parent != null ? board.transform.parent : board.transform;
        Transform manager = Child("Room Supplies", roomRoot);
        var supply = manager.gameObject.AddComponent<SeesawRoomSupplies>();
        Transform volumeTransform = Child("Room Bounds", manager);
        volumeTransform.position = bounds.center + Vector3.up * 2f;
        volumeTransform.gameObject.layer = 2; // Sensor only; never a Spider climbing surface.
        var volume = volumeTransform.gameObject.AddComponent<BoxCollider2D>();
        volume.isTrigger = true;
        volume.size = new Vector2(Mathf.Max(40f, bounds.size.x + 12f), 22f);
        Vector2 surface = board.ClosestPoint(new Vector2(bounds.center.x, bounds.max.y + 2f));
        Transform fanPoint = Child("Fan Spawner", board.transform);
        fanPoint.position = surface + Vector2.up * 0.48f;
        Transform ballPoint = Child("Wind Ball Spawner", board.transform);
        ballPoint.position = surface + Vector2.up * 3.4f;
        Physics2D.SyncTransforms();

        // Replace the old pre-placed supplies with the new system. Keep them editable in the scene.
        foreach (PossessableBody oldFan in UnityEngine.Object.FindObjectsByType<PossessableBody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (oldFan.gameObject.scene == door.gameObject.scene && oldFan.Form == BodyForm.Fan && volume.bounds.Contains(oldFan.transform.position))
                oldFan.gameObject.SetActive(false);
        foreach (CircleVisual oldBall in UnityEngine.Object.FindObjectsByType<CircleVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (oldBall.gameObject.scene == door.gameObject.scene && oldBall.TryGetComponent(out Rigidbody2D body) &&
                body.GetComponent<PossessableBody>() == null && volume.bounds.Contains(body.position))
                oldBall.gameObject.SetActive(false);

        var data = new SerializedObject(supply);
        Set(data, "player", player); Set(data, "boardSurface", board); Set(data, "roomVolume", volume);
        Set(data, "fanPrefab", fanPrefab); Set(data, "fanSpawnPoint", fanPoint);
        Set(data, "ballPrefab", ballPrefab); Set(data, "ballSpawnPoint", ballPoint);
        data.FindProperty("activateOnStart").boolValue = false;
        data.FindProperty("countBallsAcrossScene").boolValue = true;
        data.ApplyModifiedPropertiesWithoutUndo();
        doorData.Update();
        Set(doorData, "roomSupplies", supply);
        doorData.ApplyModifiedPropertiesWithoutUndo();
        foreach (var pair in savedPositions)
            if (pair.Key != null && (pair.Key.position - pair.Value).sqrMagnitude > 0.000001f)
                throw new InvalidOperationException("Configuration unexpectedly moved " + pair.Key.name);
        EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
        EditorSceneManager.SaveScene(door.gameObject.scene);
        AssetDatabase.SaveAssets();
        Validate();
    }

    public static void ConfigureBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/NewLevel02.unity");
        Configure();
    }

    [MenuItem("Tools/Seesaw Room/Validate Supplies")]
    public static void Validate()
    {
        SeesawRoomSupplies supply = UnityEngine.Object.FindFirstObjectByType<SeesawRoomSupplies>();
        if (supply == null || !supply.ReferencesReady) throw new InvalidOperationException("Supply references are incomplete.");
        var door = UnityEngine.Object.FindFirstObjectByType<StarRoomDoor>();
        if (door == null || new SerializedObject(door).FindProperty("roomSupplies").objectReferenceValue != supply)
            throw new InvalidOperationException("Door must activate this room's supplies.");
        Debug.Log("ROOM SUPPLY SCENE PASS: prefabs, board anchors, room bounds and door activation are configured.");
    }

    private static PossessableBody CreateFanVariant(float range)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(FanPath);
        if (asset != null) return asset.GetComponent<PossessableBody>();
        var instance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Form/FanBody.prefab")) as GameObject;
        try
        {
            instance.name = "Seesaw Room Fan";
            var body = instance.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 3f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.useFullKinematicContacts = true;
            var form = new SerializedObject(instance.GetComponent<PossessableBody>());
            form.FindProperty("moveSpeed").floatValue = 3f;
            form.ApplyModifiedPropertiesWithoutUndo();
            var wind = new SerializedObject(instance.GetComponent<FanBlower>());
            wind.FindProperty("maxRange").floatValue = range;
            wind.FindProperty("airflowWidth").floatValue = 2.4f;
            wind.FindProperty("startBlowing").boolValue = false;
            wind.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            PrefabUtility.SaveAsPrefabAsset(instance, FanPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(FanPath).GetComponent<PossessableBody>();
    }

    private static WindReleasedBall CreateBallVariant()
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(BallPath);
        if (asset != null) return asset.GetComponent<WindReleasedBall>();
        var instance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Form/Obstacle.prefab")) as GameObject;
        try
        {
            instance.name = "Seesaw Room Ball";
            instance.AddComponent<WindReleasedBall>();
            var body = instance.GetComponent<Rigidbody2D>();
            body.gravityScale = 1.5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            PrefabUtility.SaveAsPrefabAsset(instance, BallPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(BallPath).GetComponent<WindReleasedBall>();
    }

    private static Transform Child(string name, Transform parent)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
    private static void Set(SerializedObject data, string name, UnityEngine.Object value) => data.FindProperty(name).objectReferenceValue = value;
}

[CustomEditor(typeof(SeesawRoomSupplies))]
public class SeesawRoomSuppliesInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var supply = (SeesawRoomSupplies)target;
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Toggle("Room Active", supply.IsRoomActive);
        EditorGUILayout.IntField("Fans In Room", supply.FanCount);
        EditorGUILayout.IntField("Ordinary Balls", supply.OrdinaryBallCount);
        EditorGUILayout.Toggle("Any Fan Blowing", supply.AnyFanBlowing);
        EditorGUI.EndDisabledGroup();
    }
}
