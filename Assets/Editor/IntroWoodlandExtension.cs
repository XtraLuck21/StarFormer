using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IntroWoodlandExtension
{
    const string Art = "Assets/Art/IntroBouncyBallForest/";
    static Color Ink => Hex("070F0C");
    static Color Cream => Hex("F5DBA3");
    [MenuItem("StarFormer/Art/Apply Woodland to Box and Bubble")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string name in new[] { "IntroBox", "IntroBubble" })
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            ApplyScene(scene);
            EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/IntroBox.unity");
        Debug.Log("WOODLAND EXTENSION: Box and Bubble saved; gameplay and collision components retained.");
    }
    static void ApplyScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true).Reverse())
            if (t.name == "Woodland Surface" || t.name == "Woodland Backdrop") Object.DestroyImmediate(t.gameObject);
        var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        // Only existing opaque, dark, solid rectangular terrain; never forms or triggers.
        var solids = renderers.Where(r => r.gameObject.activeInHierarchy && r.color.r < .3f && r.color.g < .3f &&
            r.color.b < .3f && r.TryGetComponent<BoxCollider2D>(out var c) && !c.isTrigger &&
            r.GetComponent<PossessableBody>() == null).Select(r => r.GetComponent<BoxCollider2D>()).ToList();
        var waters = renderers.Where(r => r.gameObject.activeInHierarchy && r.GetComponent<WaterZone>() != null)
            .Select(r => r.GetComponent<BoxCollider2D>()).ToList();
        var material = solids[0].GetComponent<SpriteRenderer>().sharedMaterial;
        foreach (var col in solids)
        {
            Bounds b = col.bounds;
            col.GetComponent<SpriteRenderer>().color = Hex("44473B");
            var surface = new GameObject("Woodland Surface").transform;
            surface.SetParent(col.transform, false);
            Add(surface, "Soil", "SoilTile.png", b.center, b.size, 0, -.01f, true, material);
            var spans = new List<Vector2> { new Vector2(b.min.x, b.max.x) };
            foreach (var other in solids.Concat(waters))
            {
                if (other == col) continue;
                Bounds o = other.bounds;
                if (o.min.y <= b.max.y + .005f && o.max.y > b.max.y + .005f) Subtract(spans, o.min.x, o.max.x);
            }
            foreach (var span in spans)
                Add(surface, "Grass", "GrassEdge.png", new Vector2((span.x + span.y) / 2, b.max.y - .125f),
                    new Vector2(span.y - span.x, .25f), 0, -.02f, true, material);
        }
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
            foreach (var hint in root.GetComponentsInChildren<BubbleMovementHint>(true))
            {
                bool wall = solids.Any(c => c.bounds.Contains(new Vector3(hint.transform.position.x, hint.transform.position.y, c.bounds.center.z)));
                var so = new SerializedObject(hint);
                so.FindProperty("inkColor").colorValue = wall ? Cream : Ink;
                so.FindProperty("keyBackground").colorValue = wall ? Ink : Cream;
                so.FindProperty("popDescriptionColor").colorValue = wall ? Cream : Ink;
                so.FindProperty("sortingOrder").intValue = 30;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var hint in root.GetComponentsInChildren<BoxGrowHint>(true))
            {
                var so = new SerializedObject(hint); so.FindProperty("frameColor").colorValue = Cream;
                so.FindProperty("sortingOrder").intValue = 30; so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "Collection Book Label") { text.gameObject.SetActive(false); continue; }
                if (text.name == "Touch Title") continue;
                // Existing labels already choose dark ink on scenery and pale ink on walls.
                if (text.color.r < .4f) text.color = Ink;
                else if (text.color.r > .9f && text.color.g > .9f) text.color = Cream;
            }
        }
        foreach (var sr in renderers)
            if (sr.name.Contains("dash") || sr.name.Contains("arrow") || sr.name.Contains("shaft")) sr.color = Cream;
        var button = GameObject.Find("Collection Book Button").GetComponent<RectTransform>();
        button.anchoredPosition = new Vector2(-140, -24); button.sizeDelta = new Vector2(88, 88);
        var icon = button.GetComponentInChildren<CollectionBookGraphic>();
        icon.rectTransform.anchoredPosition = Vector2.zero; icon.rectTransform.sizeDelta = new Vector2(88, 88);
        var settings = new SerializedObject(icon); settings.FindProperty("squareCover").boolValue = true;
        settings.ApplyModifiedPropertiesWithoutUndo();
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
