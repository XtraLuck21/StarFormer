using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Applies surface finishes only to the authored Smiley Face room; never edits colliders.</summary>
public static class Level02SmileSurfaces
{
    private const string ScenePath = "Assets/Scenes/Level02.unity";
    private const string PreviewFolder = "Temp/SmileSurfacePreview";

    [MenuItem("StarFormer/Build Level02 Smile Surfaces")]
    public static void BuildFromMenu()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogWarning("Open Level02 outside Play mode before applying Smile surface finishes.");
            return;
        }
        GameObject room = EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(root => root.name == "Smile face part");
        if (room == null) { Debug.LogError("The authored Smile face part was not found."); return; }
        Capture("before");
        foreach (Transform surface in room.GetComponentsInChildren<Transform>(true))
        {
            SmileRoomSurfaceSkin.Finish? finish = SelectFinish(surface);
            if (!finish.HasValue || !surface.TryGetComponent(out SpriteRenderer source)) continue;
            // Invisible arrival zones, face artwork, hazards, forms and markers are never restyled.
            if (!source.enabled || source.color.a <= .001f || source.sprite == null) continue;
            var skin = surface.GetComponent<SmileRoomSurfaceSkin>();
            if (skin == null) skin = surface.gameObject.AddComponent<SmileRoomSurfaceSkin>();
            var serialized = new SerializedObject(skin);
            serialized.FindProperty("finish").enumValueIndex = (int)finish.Value;
            if (finish == SmileRoomSurfaceSkin.Finish.PoolFloor)
            {
                var waters = room.GetComponentsInChildren<WaterZone>(true);
                float left = waters.Min(water => water.GetComponent<SpriteRenderer>().bounds.min.x);
                float right = waters.Max(water => water.GetComponent<SpriteRenderer>().bounds.max.x);
                serialized.FindProperty("poolTileSpan").vector2Value = new Vector2(left, right);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (finish != SmileRoomSurfaceSkin.Finish.Button)
            {
                Color color = SmileRoomSurfaceSkin.BaseColor(finish.Value);
                color.a = source.color.a;
                source.color = color;
                EditorUtility.SetDirty(source);
            }
            skin.Refresh();
        }
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Capture("after");
        Debug.Log("Smile room surface finishes saved. Layout, colliders, hazards and gameplay settings unchanged.");
    }

    private static SmileRoomSurfaceSkin.Finish? SelectFinish(Transform surface)
    {
        if (surface.GetComponent<RatchetingCeiling>() != null) return SmileRoomSurfaceSkin.Finish.IvoryTooth;
        if (surface.name == "Door (1)" || surface.name == "Smile Spider Return Wall")
            return SmileRoomSurfaceSkin.Finish.PaintedMetal;
        if (surface.name == "Pole" && surface.parent != null && surface.parent.name == "Smile Route Rising Flag Visual")
            return SmileRoomSurfaceSkin.Finish.FlagPole;
        if (surface.name == "Smile Route Button Cap") return SmileRoomSurfaceSkin.Finish.Button;
        if (surface.parent == null || surface.parent.name != "Smile face part") return null;
        if (surface.name == "wall (29)") return SmileRoomSurfaceSkin.Finish.PoolTile;
        if (surface.name == "wall (28)") return SmileRoomSurfaceSkin.Finish.PoolFloor;
        switch (surface.name)
        {
            case "wall (36)": case "wall (37)": case "wall (38)": case "wall (39)":
            case "wall (44)": case "wall (45)": case "wall (46)": case "wall (47)":
                return SmileRoomSurfaceSkin.Finish.Timber;
            case "wall (31)": case "wall (32)": case "wall (33)": case "wall (40)":
            case "wall (41)": case "wall (42)": case "wall (43)": case "wall (48)":
                return SmileRoomSurfaceSkin.Finish.Brick;
            default: return null;
        }
    }

    [MenuItem("StarFormer/Capture Level02 Smile Surfaces")]
    public static void CaptureFromMenu() => Capture("current");

    [MenuItem("StarFormer/Frame Level02 Smile Surfaces")]
    public static void FrameFromMenu()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath || SceneView.lastActiveSceneView == null) return;
        var view = SceneView.lastActiveSceneView;
        view.in2DMode = true;
        view.LookAt(new Vector3(44f, 18f, 0f), Quaternion.identity, 20f, true);
        view.Repaint();
    }

    private static void Capture(string label)
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().path != ScenePath) return;
        Directory.CreateDirectory(PreviewFolder);
        CaptureView(label + "-whole", new Vector2(44f, 18f), 18f);
        CaptureView(label + "-mouth", new Vector2(45f, 12.8f), 9f);
        CaptureView(label + "-climb", new Vector2(60f, 20.5f), 10f);
        CaptureView(label + "-forehead", new Vector2(44f, 28.5f), 7.5f);
    }

    private static void CaptureView(string label, Vector2 center, float size)
    {
        var go = new GameObject("Smile surface preview camera") { hideFlags = HideFlags.HideAndDontSave };
        RenderTexture target = RenderTexture.GetTemporary(1600, 1000, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.transform.position = new Vector3(center.x, center.y, -100f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(166, 183, 189, 255);
            camera.targetTexture = target;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(PreviewFolder, label + ".png"), image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(go);
        }
    }
}
