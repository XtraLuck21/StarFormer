using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Preview scenes never save changes to the authored gameplay scenes.
public static class AnalyticsMapExporter
{
    private static readonly string[] Scenes =
        { "IntroBox", "IntroBubble", "IntroBouncyBall", "Level01", "IntroSpider", "IntroFan", "Level02" };

    [MenuItem("Analytics/Export all real level maps")]
    public static void ExportAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before exporting maps.");

        string project = Path.GetDirectoryName(Application.dataPath);
        string backend = Path.Combine(project, "AnalyticsBackend");
        string assetsFile = Path.Combine(backend, "MapCatalog.html");
        var history = File.Exists(assetsFile)
            ? JsonUtility.FromJson<AnalyticsMapCatalog>(File.ReadAllText(assetsFile)).maps.ToList()
            : new List<AnalyticsMapEntry>();
        var current = new List<AnalyticsMapEntry>();

        foreach (string sceneName in Scenes)
        {
            string scenePath = "Assets/Scenes/" + sceneName + ".unity";
            string sourceHash = Hash(File.ReadAllBytes(scenePath));
            Scene preview = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                AnalyticsMapEntry map = RenderMap(preview, sceneName, sourceHash, backend);
                history.RemoveAll(old => old.level_name == sceneName && old.map_version == map.map_version);
                history.Add(map);
                current.Add(new AnalyticsMapEntry
                {
                    level_name = map.level_name, map_version = map.map_version,
                    image_file = map.image_file, min_x = map.min_x, max_x = map.max_x,
                    source_scene_hash = sourceHash,
                    min_y = map.min_y, max_y = map.max_y, width = map.width, height = map.height
                });
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        Directory.CreateDirectory(Path.Combine(backend, "Maps"));
        // Apps Script can load this HTML resource as JSON without publishing
        // image URLs or exposing Drive permissions. Images are fetched on demand.
        File.WriteAllText(assetsFile, JsonUtility.ToJson(new AnalyticsMapCatalog { maps = history.ToArray() }));
        File.WriteAllText(Path.Combine(backend, "Maps/manifest.json"),
            JsonUtility.ToJson(new AnalyticsMapCatalog { maps = current.ToArray() }, true));
        File.WriteAllText(Path.Combine(Application.dataPath, "Resources/AnalyticsMapCatalog.json"),
            JsonUtility.ToJson(new AnalyticsMapCatalog { maps = current.ToArray() }, true));
        AssetDatabase.Refresh();
        Debug.Log($"[Analytics maps] Exported {current.Count} maps, bounds and runtime map versions.");
    }

    public static void ExportAllBatch() => ExportAll();

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static AnalyticsMapEntry RenderMap(Scene scene, string level, string sourceHash, string backend)
    {
        var renderers = new List<SpriteRenderer>();
        Color background = new Color(0.70f, 0.70f, 0.70f);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Remove screen UI and world-space instruction canvases, not geometry.
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(false);
            foreach (TMPro.TMP_Text text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                text.enabled = false;
            foreach (Camera existing in root.GetComponentsInChildren<Camera>(true))
            {
                if (existing.orthographic) background = existing.backgroundColor;
                existing.enabled = false;
            }
            renderers.AddRange(root.GetComponentsInChildren<SpriteRenderer>(false)
                .Where(item => item.enabled && item.sprite != null && item.bounds.size.sqrMagnitude > 0));
        }
        if (renderers.Count == 0) throw new InvalidOperationException("No map sprites in " + level);

        Bounds bounds = renderers[0].bounds;
        foreach (SpriteRenderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        bounds.Expand(new Vector3(2f, 2f, 0));

        float pixelsPerUnit = Mathf.Min(48f, 4096f / Mathf.Max(bounds.size.x, bounds.size.y));
        int width = Mathf.Clamp(Mathf.CeilToInt(bounds.size.x * pixelsPerUnit), 256, 4096);
        int height = Mathf.Clamp(Mathf.CeilToInt(bounds.size.y * pixelsPerUnit), 256, 4096);
        // Expand to the exact rendered aspect ratio, so coordinate conversion
        // cannot stretch the image independently of the death overlay.
        float worldHeight = Mathf.Max(bounds.size.y, bounds.size.x * height / width);
        float worldWidth = worldHeight * width / height;
        var cameraObject = new GameObject("Analytics export camera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = worldHeight * 0.5f;
        camera.aspect = (float)width / height;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -100f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.useOcclusionCulling = false;
        camera.scene = scene;

        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        byte[] png;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            png = image.EncodeToPNG();
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        string version = "map-" + sourceHash.Substring(0, 12) + "-" + Hash(png).Substring(0, 12);
        string relative = "Maps/" + level + "/" + version + ".png";
        string path = Path.Combine(backend, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, png);
        Debug.Log($"[Analytics maps] {level}: {width}x{height}, world {worldWidth}x{worldHeight}, {version}");
        return new AnalyticsMapEntry
        {
            level_name = level, map_version = version, image_file = relative,
            source_scene_hash = sourceHash,
            image_data_url = "data:image/png;base64," + Convert.ToBase64String(png),
            min_x = bounds.center.x - worldWidth * 0.5f, max_x = bounds.center.x + worldWidth * 0.5f,
            min_y = bounds.center.y - worldHeight * 0.5f, max_y = bounds.center.y + worldHeight * 0.5f,
            width = width, height = height
        };
    }
}
