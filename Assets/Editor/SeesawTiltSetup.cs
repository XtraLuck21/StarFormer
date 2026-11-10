using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SeesawTiltSetup
{
    [MenuItem("Tools/Seesaw Room/Configure Triangle and Tilt")]
    public static void Configure()
    {
        var supply = UnityEngine.Object.FindFirstObjectByType<SeesawRoomSupplies>();
        if (supply == null || !supply.ReferencesReady) throw new InvalidOperationException("Configure room supplies first.");
        var existing = UnityEngine.Object.FindFirstObjectByType<SeesawTilt2D>();
        if (existing != null)
        {
            MountEndStops(existing.transform.parent, existing.transform);
            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
            EditorSceneManager.SaveScene(existing.gameObject.scene);
            Validate();
            return;
        }
        var data = new SerializedObject(supply);
        var board = data.FindProperty("boardSurface").objectReferenceValue as BoxCollider2D;
        if (board == null) throw new InvalidOperationException("The board must have a BoxCollider2D.");
        if (board.GetComponent<Rigidbody2D>() != null) throw new InvalidOperationException("The board already has a Rigidbody2D; use one pivot authority.");
        Physics2D.SyncTransforms();
        Transform room = board.transform.parent;
        Vector3 center = board.transform.TransformPoint(board.offset);
        Vector3 oldScale = board.transform.lossyScale;
        float a = board.size.x * Mathf.Abs(oldScale.x);
        float b = board.size.y * Mathf.Abs(oldScale.y);
        float length = Mathf.Max(a, b);
        float thickness = Mathf.Min(a, b);
        var childPositions = new Dictionary<Transform, Vector3>();
        foreach (Transform child in board.transform) childPositions[child] = child.position;

        Transform pivot = Child("Seesaw Pivot", room);
        pivot.position = center - Vector3.up * (thickness * 0.5f);
        var body = pivot.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.useFullKinematicContacts = true;
        board.transform.SetParent(pivot, false);
        board.transform.localPosition = Vector3.up * (thickness * 0.5f);
        board.transform.localRotation = Quaternion.identity;
        board.transform.localScale = new Vector3(length, thickness, 1f);
        board.size = Vector2.one;
        board.offset = Vector2.zero;
        foreach (var pair in childPositions) pair.Key.position = pair.Value;

        Color supportColor = new Color(0.85f, 0.62f, 0.36f);
        Transform legacy = room.Find("Fulcrum");
        if (legacy != null)
        {
            if (legacy.TryGetComponent(out SpriteRenderer oldRenderer)) supportColor = oldRenderer.color;
            legacy.name = "Legacy Fulcrum (disabled)";
            legacy.gameObject.SetActive(false);
        }
        float floorTop = pivot.position.y - 2.5f;
        Transform floor = room.Find("Seesaw Room Floor");
        if (floor != null && floor.TryGetComponent(out Collider2D floorCollider)) floorTop = floorCollider.bounds.max.y;
        float height = Mathf.Max(0.5f, pivot.position.y - floorTop);
        Transform triangle = Child("Fulcrum", room);
        triangle.position = pivot.position - Vector3.up * (height * 0.5f);
        triangle.localScale = new Vector3(3.2f, height, 1f);
        var visual = triangle.gameObject.AddComponent<TriangleFulcrumVisual>();
        var renderer = triangle.GetComponent<SpriteRenderer>();
        renderer.color = supportColor;
        renderer.sharedMaterial = board.GetComponent<SpriteRenderer>().sharedMaterial;
        renderer.sortingOrder = -1;

        var tilt = pivot.gameObject.AddComponent<SeesawTilt2D>();
        var tiltData = new SerializedObject(tilt);
        Set(tiltData, "supplies", supply); Set(tiltData, "boardSurface", board); Set(tiltData, "fulcrum", visual);
        tiltData.ApplyModifiedPropertiesWithoutUndo();
        data.Update(); Set(data, "seesaw", tilt); data.ApplyModifiedPropertiesWithoutUndo();
        Sensor(pivot, tilt, SeesawTilt2D.Side.Left, -length * 0.5f + 1.7f, thickness);
        Sensor(pivot, tilt, SeesawTilt2D.Side.Right, length * 0.5f - 1.7f, thickness);
        MountEndStops(room, pivot);
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(supply.gameObject.scene);
        EditorSceneManager.SaveScene(supply.gameObject.scene);
        Validate();
    }

    public static void ConfigureBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/NewLevel02.unity");
        Configure();
    }

    [MenuItem("Tools/Seesaw Room/Validate Triangle and Tilt")]
    public static void Validate()
    {
        var tilt = UnityEngine.Object.FindFirstObjectByType<SeesawTilt2D>();
        if (tilt == null || !tilt.ReferencesReady) throw new InvalidOperationException("Tilt references missing.");
        var support = UnityEngine.Object.FindFirstObjectByType<TriangleFulcrumVisual>();
        if (support == null || support.GetComponent<PolygonCollider2D>().GetPath(0).Length != 3 ||
            support.GetComponent<SpriteRenderer>().sprite == null)
            throw new InvalidOperationException("Fulcrum requires actual triangular render and collision geometry.");
        Texture2D texture = support.GetComponent<SpriteRenderer>().sprite.texture;
        if (texture.GetPixel(0, texture.height - 1).a > 0.01f ||
            texture.GetPixel(texture.width - 1, texture.height - 1).a > 0.01f ||
            texture.GetPixel(texture.width / 2, texture.height / 2).a < 0.9f)
            throw new InvalidOperationException("Fulcrum sprite must have a triangular silhouette.");
        if (tilt.GetComponentsInChildren<SeesawEndSensor>().Length != 2)
            throw new InvalidOperationException("Both end sensors are required.");
        Debug.Log("SEESAW TILT SCENE PASS: triangle silhouette/collider, central pivot and both end sensors.");
    }

    private static void Sensor(Transform pivot, SeesawTilt2D tilt, SeesawTilt2D.Side side, float x, float thickness)
    {
        Transform t = Child(side + " Ball Contact", pivot);
        t.gameObject.layer = 2;
        t.localPosition = new Vector3(x, thickness + 0.25f, 0);
        var collider = t.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.8f, 1.4f);
        var sensor = t.gameObject.AddComponent<SeesawEndSensor>();
        var data = new SerializedObject(sensor);
        Set(data, "seesaw", tilt);
        data.FindProperty("side").enumValueIndex = (int)side;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void MountEndStops(Transform room, Transform pivot)
    {
        foreach (string name in new[] { "Baffle01", "Baffle02" })
        {
            Transform stop = room.Find(name);
            if (stop != null) stop.SetParent(pivot, true);
        }
    }
    private static Transform Child(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }
    private static void Set(SerializedObject data, string name, UnityEngine.Object value) => data.FindProperty(name).objectReferenceValue = value;
}

[CustomEditor(typeof(SeesawTilt2D))]
public class SeesawTiltInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var tilt = (SeesawTilt2D)target;
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.EnumPopup("Last Side", tilt.LastSide);
        EditorGUILayout.FloatField("Current Angle", tilt.CurrentAngle);
        EditorGUILayout.FloatField("Target Angle", tilt.TargetAngle);
        EditorGUILayout.Toggle("Is Animating", tilt.IsAnimating);
        EditorGUILayout.IntField("Trigger Count", tilt.TriggerCount);
        EditorGUI.EndDisabledGroup();
    }
}
