using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level01WoodlandSetup
{
    const string Art = "Assets/Art/IntroBouncyBallForest/";
    static Color Ink => Hex("070F0C");
    static Color Cream => Hex("F5DBA3");
    [MenuItem("StarFormer/Art/Apply Woodland to Level01")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string name in new[] { "Level01" })
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            ApplyScene(scene);
            EditorSceneManager.SaveScene(scene);
        }
        
        Debug.Log("LEVEL01 WOODLAND: saved; gameplay and collision components retained.");
    }
    static void ApplyScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true).Reverse())
            if (t.name == "Woodland Surface" || t.name == "Woodland Backdrop") Object.DestroyImmediate(t.gameObject);
        var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        // Only existing opaque, dark, solid rectangular terrain; never forms or triggers.
        var solids = renderers.Where(r => (r.color.r < .3f && r.color.g < .3f && r.color.b < .3f || r.color == Hex("44473B")) && r.TryGetComponent<BoxCollider2D>(out var c) && !c.isTrigger &&
            r.GetComponent<PossessableBody>() == null).Select(r => r.GetComponent<BoxCollider2D>()).ToList();
        var waters = renderers.Where(r => r.gameObject.activeInHierarchy && r.GetComponent<WaterZone>() != null)
            .Select(r => r.GetComponent<BoxCollider2D>()).ToList();
        var material = solids[0].GetComponent<SpriteRenderer>().sharedMaterial;
        foreach (var col in solids)
        {
            if (col.name == "EntranceTitleWall") continue; // Keep the title backing quiet and untextured.
            Bounds b = WorldBounds(col);
            col.GetComponent<SpriteRenderer>().color = Hex("44473B");
            var surface = new GameObject("Woodland Surface").transform;
            surface.SetParent(col.transform, false);
            Add(surface, "Soil", "SoilTile.png", b.center, b.size, 0, -.01f, true, material);
            var spans = new List<Vector2> { new Vector2(b.min.x, b.max.x) };
            foreach (var other in solids.Concat(waters))
            {
                if (other == col) continue;
                Bounds o = WorldBounds(other);
                if (o.min.y <= b.max.y + .005f && o.max.y > b.max.y + .005f) Subtract(spans, o.min.x, o.max.x);
            }
            foreach (var span in spans)
                Add(surface, "Grass", "GrassEdge.png", new Vector2((span.x + span.y) / 2, b.max.y - .125f),
                    new Vector2(span.y - span.x, .25f), 0, -.02f, true, material);
        }
        // Moving mechanisms retain a lighter timber tint and their existing silhouettes.
        foreach (var door in renderers.Where(r => r.GetComponent<RatchetingCeiling>() != null))
            door.color = Hex("8B8862");
        foreach (var water in waters)
        {
            var surface = new GameObject("Woodland Surface").transform;
            surface.SetParent(water.transform, false);
            // Water stays behind solid dividers and form sprites, including at overlaps.
            int order = water.GetComponent<SpriteRenderer>().sortingOrder;
            Add(surface, "Water depth", "PoolDepth.png", water.bounds.center, water.bounds.size, order, -.005f, false, material);
        }
        Camera camera = Camera.main;
        var bg = Add(null, "Woodland Backdrop", "ForestBackground.png", camera.transform.position,
            new Vector2(camera.orthographicSize * 2 * camera.aspect, camera.orthographicSize * 2), -100, 1, false, material);
        var fitting = bg.gameObject.AddComponent<WoodlandCameraBackdrop>();
        fitting.view = camera; fitting.artwork = bg;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // Screen UI keeps its existing panel contrast. Only world labels are themed.
                if (text.GetComponentInParent<Canvas>() != null) continue;
                bool wall = solids.Any(c => WorldBounds(c).Contains(new Vector3(
                    text.transform.position.x, text.transform.position.y, WorldBounds(c).center.z)));
                bool onMechanism = text.GetComponentInParent<RatchetingCeiling>() != null;
                text.color = text.name == "LevelOneTitle" ? Hex("FFD34D") : wall || onMechanism ? Cream : Ink;
            }
            foreach (var hint in root.GetComponentsInChildren<MovableDoorContactHint>(true))
            {
                var so = new SerializedObject(hint);
                so.FindProperty("hintInk").colorValue = Ink;
                so.FindProperty("buttonInk").colorValue = Cream;
                so.FindProperty("hintFace").colorValue = Ink;
                so.FindProperty("hintPulse").colorValue = Hex("203A30");
                so.FindProperty("previewColor").colorValue = new Color(.96f, .86f, .64f, .45f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        camera.backgroundColor = Hex("789B8E");
    }
    static Bounds WorldBounds(BoxCollider2D col)
    {
        Vector3 scale = col.transform.lossyScale;
        return new Bounds(col.transform.TransformPoint(col.offset),
            new Vector3(Mathf.Abs(col.size.x * scale.x), Mathf.Abs(col.size.y * scale.y), .1f));
    }

    static SpriteRenderer Add(Transform parent, string name, string asset, Vector2 center, Vector2 size,
        int order, float z, bool tiled, Material material)
    {
        var obj = new GameObject(name);
        obj.transform.position = new Vector3(center.x, center.y, z);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + asset); sr.sharedMaterial = material; sr.sortingOrder = order;
        if (tiled) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = size; }
        else obj.transform.localScale = new Vector3(size.x / sr.sprite.bounds.size.x, size.y / sr.sprite.bounds.size.y, 1);
        if (parent != null) obj.transform.SetParent(parent, true);
        return sr;
    }
    static void Subtract(List<Vector2> spans, float lo, float hi)
    {
        for (int i = spans.Count - 1; i >= 0; i--)
        {
            Vector2 s = spans[i]; if (hi <= s.x || lo >= s.y) continue;
            spans.RemoveAt(i);
            if (lo - s.x > .005f) spans.Add(new Vector2(s.x, Mathf.Min(lo, s.y)));
            if (s.y - hi > .005f) spans.Add(new Vector2(Mathf.Max(hi, s.x), s.y));
        }
    }
    static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
}
