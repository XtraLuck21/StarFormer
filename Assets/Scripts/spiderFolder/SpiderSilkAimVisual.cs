using System.Collections.Generic;
using UnityEngine;

// Procedural, translucent skill indicator; geometry stays in world units on scaled bodies.
internal sealed class SpiderSilkAimVisual
{
    private readonly GameObject root;
    private readonly Mesh mesh;
    private readonly Material material;
    private readonly List<Vector3> vertices = new List<Vector3>(1600);
    private readonly List<Color> colors = new List<Color>(1600);
    private readonly List<Vector2> uvs = new List<Vector2>(1600);
    private readonly List<int> triangles = new List<int>(2400);
    private Vector2 center, forward, side;
    private float z;
    private float pixelSize;
    private static readonly Color Outline = new Color(0.015f, 0.07f, 0.12f, 0.65f);

    public SpiderSilkAimVisual(Transform parent, SpriteRenderer source)
    {
        root = new GameObject("Silk Aim Preview", typeof(MeshFilter), typeof(MeshRenderer));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        mesh = new Mesh { name = "Silk Range and Connected Arrow", hideFlags = HideFlags.HideAndDontSave };
        mesh.MarkDynamic();
        material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
        material.mainTexture = Texture2D.whiteTexture;
        root.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + 4;
        root.SetActive(false);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void Draw(Vector2 origin, float depth, Vector2 direction, float range, float distance,
        float bodyRadius, bool hit)
    {
        center = origin;
        z = depth;
        forward = direction.normalized;
        side = Vector2.Perpendicular(forward);
        Camera camera = Camera.main;
        pixelSize = camera != null && camera.orthographic
            ? 2f * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight) : 0.012f;
        vertices.Clear(); colors.Clear(); uvs.Clear(); triangles.Clear();
        Color blue = new Color(0.08f, 0.65f, 0.95f);
        Color ink = hit ? new Color(1f, 0.82f, 0.06f) : new Color(0.22f, 0.85f, 1f);

        // A soft full-range wash, with a hairline broken edge instead of a dominant hoop.
        const int segments = 128;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Triangle(Vector2.zero, p * range, q * range, Alpha(blue, 0.008f), Alpha(blue, 0.025f), Alpha(blue, 0.025f));
            if (i % 8 != 0)
            {
                float rim = Mathf.Max(0.015f, pixelSize * 0.85f);
                Quad(p * (range - rim), p * range, q * range, q * (range - rim), Alpha(blue, 0.48f));
            }
        }

        // One connected silhouette: a solid shaft, broad arrowhead and rails meeting its shoulders.
        float length = Mathf.Clamp(distance, 0f, range);
        float scale = Mathf.Min(1f, length / 0.9f);
        float start = Mathf.Min(bodyRadius + 0.03f, length * 0.35f);
        float shoulderX = length - 0.38f * scale;
        float shoulderWidth = 0.17f * scale;
        float rearWidth = 0.18f * scale;
        float railWidth = 0.25f * scale;
        float bendX = Mathf.Max(start, shoulderX - 0.32f * scale);
        Vector2 upperStart = new Vector2(start, rearWidth);
        Vector2 lowerStart = new Vector2(start, -rearWidth);
        Vector2 upperBend = new Vector2(bendX, railWidth);
        Vector2 lowerBend = new Vector2(bendX, -railWidth);
        Vector2 upperShoulder = new Vector2(shoulderX, shoulderWidth);
        Vector2 lowerShoulder = new Vector2(shoulderX, -shoulderWidth);
        Color wash = Alpha(ink, 0.18f);
        Quad(lowerStart, upperStart, upperBend, lowerBend, wash);
        Stroke(upperStart, upperBend, 0.018f, Alpha(ink, 0.9f));
        Stroke(lowerStart, lowerBend, 0.018f, Alpha(ink, 0.9f));

        // Round each side into the arrow's shoulder; nothing surrounds or extends past its tip.
        Vector2 previousUpper = upperBend;
        Vector2 previousLower = lowerBend;
        for (int i = 1; i <= 16; i++)
        {
            float t = i / 16f;
            float u = 1f - t;
            Vector2 upper = u * u * upperBend + 2f * u * t * new Vector2(shoulderX, railWidth) + t * t * upperShoulder;
            Vector2 lower = new Vector2(upper.x, -upper.y);
            Quad(previousLower, previousUpper, upper, lower, wash);
            Stroke(previousUpper, upper, 0.018f, Alpha(ink, 0.9f));
            Stroke(previousLower, lower, 0.018f, Alpha(ink, 0.9f));
            previousUpper = upper;
            previousLower = lower;
        }

        float shaftHalfWidth = Mathf.Max(0.040f * scale, pixelSize * 0.9f);
        Vector2 shaftTop = new Vector2(shoulderX, shaftHalfWidth);
        Vector2 shaftBottom = new Vector2(shoulderX, -shaftHalfWidth);
        Vector2 tailTop = new Vector2(start, shaftHalfWidth);
        Vector2 tailBottom = new Vector2(start, -shaftHalfWidth);
        Vector2 tip = new Vector2(length, 0f);
        Quad(tailBottom, tailTop, shaftTop, shaftBottom, Alpha(ink, 0.95f));
        Triangle(tip, upperShoulder, lowerShoulder, Alpha(ink, 1f), Alpha(ink, 1f), Alpha(ink, 1f));
        // Outline only the external silhouette, so no line cuts across the shaft/head join.
        float edge = Mathf.Max(0.012f, pixelSize * 0.7f);
        RawStroke(tailTop, shaftTop, edge, Outline);
        RawStroke(shaftTop, upperShoulder, edge, Outline);
        RawStroke(upperShoulder, tip, edge, Outline);
        RawStroke(tip, lowerShoulder, edge, Outline);
        RawStroke(lowerShoulder, shaftBottom, edge, Outline);
        RawStroke(shaftBottom, tailBottom, edge, Outline);

        // Small inset at the neck matches the reference without adding a separate hook or reticle.
        Vector2 inset = new Vector2(shoulderX - 0.07f * scale, 0f);
        float notch = 0.045f * scale;
        Color notchInk = new Color(0.03f, 0.3f, 0.42f, 0.7f);
        RawStroke(inset - Vector2.right * notch, inset + Vector2.up * notch, edge, notchInk);
        RawStroke(inset + Vector2.up * notch, inset + Vector2.right * notch, edge, notchInk);
        RawStroke(inset + Vector2.right * notch, inset - Vector2.up * notch, edge, notchInk);
        RawStroke(inset - Vector2.up * notch, inset - Vector2.right * notch, edge, notchInk);

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        root.SetActive(true);
    }

    private static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
    private void Vertex(Vector2 p, Color color)
    {
        Vector2 world = center + forward * p.x + side * p.y;
        vertices.Add(root.transform.InverseTransformPoint(new Vector3(world.x, world.y, z)));
        colors.Add(color);
        uvs.Add(Vector2.zero);
    }
    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
    {
        int i = vertices.Count;
        Vertex(a, ca); Vertex(b, cb); Vertex(c, cc);
        triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
    }
    private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        int i = vertices.Count;
        Vertex(a, color); Vertex(b, color); Vertex(c, color); Vertex(d, color);
        triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
    }
    private void Stroke(Vector2 a, Vector2 b, float width, Color color)
    {
        // Keep fine aim details legible when the level camera is zoomed out.
        width = Mathf.Max(width, pixelSize * 0.9f);
        RawStroke(a, b, width + pixelSize * 0.9f, Outline);
        RawStroke(a, b, width, color);
    }
    private void RawStroke(Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 normal = Vector2.Perpendicular((b - a).normalized) * width * 0.5f;
        Quad(a + normal, b + normal, b - normal, a - normal, color);
    }
    public void Dispose()
    {
        Release(root); Release(mesh); Release(material);
    }
    private static void Release(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Object.Destroy(item);
        else Object.DestroyImmediate(item);
    }
}
