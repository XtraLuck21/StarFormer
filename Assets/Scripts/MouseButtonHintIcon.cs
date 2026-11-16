using System;
using UnityEngine;
using UnityEngine.UI;

// One outline for both world-space lesson sprites and Canvas prompts.
internal static class MouseButtonHintIcon
{
    public static void Draw(Transform parent, Sprite sprite, Vector2 center, float unit,
        bool rightButton, Color ink, int order)
    {
        var root = new GameObject(rightButton ? "Right mouse button" : "Left mouse button");
        root.hideFlags = parent.gameObject.hideFlags;
        root.transform.SetParent(parent, false);
        root.transform.localPosition = center;
        root.transform.localScale = Vector3.one * unit;

        void Rectangle(string name, Vector2 position, Vector2 size, float angle, Color color)
        {
            var part = new GameObject(name);
            part.hideFlags = root.hideFlags;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        DrawShape(rightButton, ink, Rectangle);
    }

    public static void DrawOnCanvas(RectTransform parent, Vector2 centerFromTop, float unit,
        bool rightButton, Color ink)
    {
        var icon = new GameObject(rightButton ? "Right mouse button" : "Left mouse button", typeof(RectTransform));
        var root = (RectTransform)icon.transform;
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
        root.anchoredPosition = centerFromTop;
        root.sizeDelta = Vector2.zero;

        void Rectangle(string name, Vector2 position, Vector2 size, float angle, Color color)
        {
            var part = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)part.transform;
            rect.SetParent(root, false);
            rect.anchoredPosition = position * unit;
            rect.sizeDelta = size * unit;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            var image = part.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        DrawShape(rightButton, ink, Rectangle);
    }

    private static void DrawShape(bool rightButton, Color ink,
        Action<string, Vector2, Vector2, float, Color> rectangle)
    {

        void Stroke(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            rectangle("Outline", (from + to) * 0.5f, new Vector2(delta.magnitude, 0.04f),
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, ink);
        }

        // Bevel the corners of the mouse silhouette and divide the buttons above the palm.
        Vector2[] outline =
        {
            new Vector2(-0.16f, 0.33f), new Vector2(0.16f, 0.33f),
            new Vector2(0.25f, 0.24f), new Vector2(0.25f, -0.21f),
            new Vector2(0.14f, -0.33f), new Vector2(-0.14f, -0.33f),
            new Vector2(-0.25f, -0.21f), new Vector2(-0.25f, 0.24f)
        };
        for (int i = 0; i < outline.Length; i++)
            Stroke(outline[i], outline[(i + 1) % outline.Length]);
        Stroke(Vector2.zero, Vector2.up * 0.33f);
        Stroke(Vector2.left * 0.25f, Vector2.right * 0.25f);
        rectangle("Button fill", new Vector2(rightButton ? 0.125f : -0.125f, 0.14f),
            new Vector2(0.18f, 0.22f), 0f, Color.white);
    }
}
