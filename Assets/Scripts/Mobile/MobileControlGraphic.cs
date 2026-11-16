using UnityEngine;
using UnityEngine.UI;

// Draw in a 2-by-2 design square so the same symbols stay sharp at any canvas scale.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MobileControlGraphic : MaskableGraphic, ICanvasRaycastFilter
{
    internal enum Symbol { Base, Thumb, Jump, Grow, Shrink }
    internal Symbol Icon { get; set; }
    private static readonly Color32 Navy = new(21, 40, 57, 200);
    private static readonly Color32 Gold = new(255, 193, 63, 245);
    private static readonly Color32 Ink = new(250, 251, 252, 255);
    private bool directional;
    private bool highlighted;

    // World hints share the button artwork, but cannot intercept touches.
    internal static RectTransform CreateWorldHint(Transform parent, Symbol icon, Vector2 center, float diameter, int order)
    {
        var root = new GameObject(icon + " Touch Hint", typeof(RectTransform), typeof(Canvas));
        var rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.localPosition = center;
        rect.sizeDelta = Vector2.one * diameter;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = order;
        var graphic = root.AddComponent<MobileControlGraphic>();
        graphic.Icon = icon;
        graphic.raycastTarget = false;
        return rect;
    }

    internal void HighlightLesson(bool value)
    {
        if (highlighted == value) return;
        highlighted = value;
        SetVerticesDirty();
    }

    private const int Segments = 48;

    internal void ShowDirection(bool show)
    {
        if (directional == show) return;
        directional = show;
        SetVerticesDirty();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera camera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, camera, out var p);
        return (p - rectTransform.rect.center).sqrMagnitude <= Mathf.Pow(rectTransform.rect.width * 0.5f, 2);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float scale = rectTransform.rect.width * 0.5f;
        Vector2 center = rectTransform.rect.center;

        void Vertex(Vector2 p, Color32 tint) => mesh.AddVert(center + p * scale, tint, Vector2.zero);
        void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 tint)
        {
            int i = mesh.currentVertCount;
            Vertex(a, tint);
            Vertex(b, tint);
            Vertex(c, tint);
            Vertex(d, tint);
            mesh.AddTriangle(i, i + 1, i + 2);
            mesh.AddTriangle(i, i + 2, i + 3);
        }
        Vector2 Radial(float radians) => new(Mathf.Cos(radians), Mathf.Sin(radians));
        void Ring(float inner, float outer, Color32 tint)
        {
            Vector2 prev = Vector2.right;
            for (int j = 1; j <= Segments; j++)
            {
                Vector2 next = Radial(j * 2f * Mathf.PI / Segments);
                Quad(prev * inner, prev * outer, next * outer, next * inner, tint);
                prev = next;
            }
        }
        void Line(Vector2 a, Vector2 b, float width, Color32 tint)
        {
            Vector2 normal = Vector2.Perpendicular((b - a).normalized) * (width * 0.5f);
            Quad(a + normal, b + normal, b - normal, a - normal, tint);
        }
        void Arrow(Vector2 tail, Vector2 tip, float head)
        {
            Vector2 along = (tip - tail).normalized;
            Vector2 back = tip - along * head;
            Vector2 side = Vector2.Perpendicular(along) * head;
            Line(tail, tip, 0.077f, Ink);
            Line(back + side, tip, 0.077f, Ink);
            Line(back - side, tip, 0.077f, Ink);
        }

        bool joystick = Icon == Symbol.Base || Icon == Symbol.Thumb;
        Color32 face = Navy;
        if (Icon == Symbol.Base) face = new Color32(24, 43, 62, 86);
        if (Icon == Symbol.Thumb && !directional) face = new Color32(180, 204, 222, 218);
        Ring(0, 0.96f, face);
        if (highlighted) Ring(0.77f, 0.96f, Gold);
        Ring(0.955f, 1f, joystick ? new Color32(255, 255, 255, 168) : Gold);
        if (Icon == Symbol.Base) return;
        if (Icon == Symbol.Thumb)
        {
            if (directional) Arrow(new Vector2(-0.38f, 0), new Vector2(0.4f, 0), 0.28f);
            return;
        }
        if (Icon == Symbol.Jump)
        {
            Vector2 star = new(-0.18f, -0.18f);
            for (int j = 0; j < 10; j++)
            {
                float angle = Mathf.PI * (0.5f + j / 5f);
                bool outerPoint = j % 2 == 0;
                Vector2 a = star + Radial(angle) * (outerPoint ? 0.4f : 0.18f);
                Vector2 b = star + Radial(angle + Mathf.PI / 5f) * (outerPoint ? 0.18f : 0.4f);
                int i = mesh.currentVertCount;
                Vertex(star, Ink);
                Vertex(a, Ink);
                Vertex(b, Ink);
                mesh.AddTriangle(i, i + 1, i + 2);
            }
            Arrow(new Vector2(0.08f, 0.02f), new Vector2(0.37f, 0.52f), 0.18f);
            return;
        }
        float s = 0.22f;
        Quad(new Vector2(-s, -s), new Vector2(-s, s), new Vector2(s, s), new Vector2(s, -s), Ink);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector2 dir = new Vector2(sign, sign);
            bool grow = Icon == Symbol.Grow;
            Arrow(dir * (grow ? 0.33f : 0.58f), dir * (grow ? 0.58f : 0.33f), 0.15f);
        }
    }
}
