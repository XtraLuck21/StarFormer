using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Gives Level02's plain black/brown blocks a school look. Safe to run repeatedly; colliders are untouched.
//
// Under the seesaw: running track on top, soil with strata and pebbles below (including the tunnel
// covers and the cracking passages), and a concrete drain-pipe tunnel for the Spider/BouncyBall route.
// The room ceiling becomes a banner beam and the chase ceiling school roof eaves; the room walls stay plain.
public static class Level02SchoolSurfaces
{
    private const string ScenePath = "Assets/Scenes/Level02.unity";
    private const string PreviewFolder = "Temp/SchoolSurfacePreview";
    private static readonly Vector3 SeesawView = new Vector3(27.5f, -16.5f, -10f);
    private const float SeesawViewSize = 12f;

    private static readonly string[] Ground =
    {
        "Seesaw Room Floor", "Underpass Left Support", "Underpass Left Support (1)", "Underpass Left Support (2)",
        "Underpass Central Support", "Spider Cracked Entrance", "Bouncy Area Passage Exit",
        "Upper Tunnel Cover", "Lower Tunnel Cover", "Extending Floor Visual"
    };
    private static readonly string[] Pipe =
    {
        "Underpass Floor", "Underpass Left End Wall", "Underpass Right End Wall", "Underpass Bouncy Exit Ramp"
    };
    // Kept plain on request: earlier builds gave these a brick skin, which is removed and the color restored.
    private static readonly (string name, Color color)[] Plain =
    {
        ("Seesaw Room Left Wall", new Color(0.43f, 0.29f, 0.17f, 1f)),
        ("Seesaw Room Right Wall", new Color(0.43f, 0.29f, 0.17f, 1f)),
        ("Original Floor Left Remainder", Color.black),
        ("Original Floor Right Remainder", Color.black),
    };

    [MenuItem("StarFormer/Build Level02 School Surfaces")]
    public static void BuildFromMenu()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }
        Capture("before");
        Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        foreach (SchoolSurfaceSkin skin in Object.FindObjectsByType<SchoolSurfaceSkin>(FindObjectsSortMode.None))
            skin.Refresh();
        Capture("after");
        Debug.Log("Level02 school surfaces built.");
    }

    [MenuItem("StarFormer/Capture Level02 Seesaw Preview")]
    public static void CaptureFromMenu() => Capture("current");

    private static void Apply()
    {
        Physics2D.SyncTransforms();
        var objects = new Dictionary<string, GameObject>();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (!objects.ContainsKey(t.name)) objects[t.name] = t.gameObject;

        float surfaceTop = Bounds(objects, "Seesaw Room Floor").max.y;
        foreach (string name in Ground) Skin(objects, name, SchoolSurfaceSkin.Style.PlaygroundGround, surfaceTop);
        foreach (string name in Pipe) Skin(objects, name, SchoolSurfaceSkin.Style.ConcretePipe, surfaceTop);
        foreach (var (name, color) in Plain) RemoveSkin(objects, name, color);
        Skin(objects, "Seesaw Room Ceiling", SchoolSurfaceSkin.Style.BannerBeam, surfaceTop);
        Skin(objects, "Chase Tunnel Ceiling", SchoolSurfaceSkin.Style.RoofEaves, surfaceTop);

        // Pipe interior behind the underground route, between its end walls and under the covers.
        Bounds left = Bounds(objects, "Underpass Left End Wall"), right = Bounds(objects, "Underpass Right End Wall");
        Bounds floor = Bounds(objects, "Underpass Floor"), cover = Bounds(objects, "Lower Tunnel Cover");
        var interior = Rect.MinMaxRect(left.max.x, floor.max.y, right.min.x, cover.max.y);
        var skin = new SerializedObject(objects["Underpass Floor"].GetComponent<SchoolSurfaceSkin>());
        skin.FindProperty("backdrop").rectValue = interior;
        skin.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Skin(Dictionary<string, GameObject> objects, string name, SchoolSurfaceSkin.Style style,
        float surfaceTop)
    {
        if (!objects.TryGetValue(name, out GameObject target) || !target.TryGetComponent(out SpriteRenderer sprite))
        {
            Debug.LogWarning($"School surfaces: '{name}' was not found or has no SpriteRenderer; skipped.");
            return;
        }
        var skin = target.GetComponent<SchoolSurfaceSkin>();
        if (skin == null) skin = target.AddComponent<SchoolSurfaceSkin>();
        var serialized = new SerializedObject(skin);
        serialized.FindProperty("style").enumValueIndex = (int)style;
        serialized.FindProperty("surfaceTop").floatValue = surfaceTop;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        // The source block keeps the matching base color: shatter fragments copy it,
        // and the covers only change its alpha while fading.
        Color color = SchoolSurfaceSkin.BaseColor(style);
        color.a = sprite.color.a;
        var renderer = new SerializedObject(sprite);
        renderer.FindProperty("m_Color").colorValue = color;
        renderer.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveSkin(Dictionary<string, GameObject> objects, string name, Color color)
    {
        if (!objects.TryGetValue(name, out GameObject target)) return;
        if (target.TryGetComponent(out SchoolSurfaceSkin skin)) Object.DestroyImmediate(skin);
        if (!target.TryGetComponent(out SpriteRenderer sprite)) return;
        var renderer = new SerializedObject(sprite);
        renderer.FindProperty("m_Color").colorValue = color;
        renderer.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Bounds Bounds(Dictionary<string, GameObject> objects, string name)
    {
        if (!objects.TryGetValue(name, out GameObject target))
            throw new System.InvalidOperationException($"Level02 is missing '{name}'.");
        if (target.TryGetComponent(out Collider2D collider) && collider.enabled && target.activeInHierarchy)
            return collider.bounds;
        return target.GetComponent<Renderer>().bounds;
    }

    // Renders the seesaw room from a temporary camera into Temp/SchoolSurfacePreview/<label>.png.
    private static void Capture(string label)
    {
        var cameraObject = new GameObject("School surface preview camera") { hideFlags = HideFlags.HideAndDontSave };
        var texture = new RenderTexture(1600, 900, 24);
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = SeesawViewSize;
            camera.transform.position = SeesawView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.gray;
            camera.targetTexture = texture;
            var request = new RenderPipeline.StandardRequest { destination = texture };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
                RenderPipeline.SubmitRenderRequest(camera, request);
            else
                camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            Directory.CreateDirectory(PreviewFolder);
            string path = Path.Combine(PreviewFolder, label + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Debug.Log($"School surfaces preview saved: {Path.GetFullPath(path)}");
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
            texture.Release();
            Object.DestroyImmediate(texture);
        }
    }
}
