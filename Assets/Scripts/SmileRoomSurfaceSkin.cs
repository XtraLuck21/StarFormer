using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Surface finishes for the school playground's Smiley Face room. All artwork stays inside the
/// original sprite and follows its visibility/alpha; this component never changes gameplay geometry.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SmileRoomSurfaceSkin : MonoBehaviour
{
    public enum Finish { Brick, Timber, PoolTile, IvoryTooth, PaintedMetal, FlagPole, Button, PoolFloor }

    [SerializeField] private Finish finish;
    [Tooltip("Slope of the painted smile at this tooth, from left to right.")]
    [SerializeField] private float toothSlope;
    [Tooltip("World X range of the water above a PoolFloor. Only this part receives ceramic tiles.")]
    [SerializeField] private Vector2 poolTileSpan;

    private static readonly Color Brick = new Color32(150, 71, 58, 255);
    private static readonly Color BrickLight = new Color32(165, 82, 65, 255);
    private static readonly Color Mortar = new Color32(184, 130, 109, 255);
    private static readonly Color Cream = new Color32(246, 236, 214, 255);
    private static readonly Color Teal = new Color32(55, 104, 102, 255);

    private SpriteRenderer source;
    private GameObject art;
    private Mesh mesh;
    private Material material;
    private Vector3 builtScale, builtPosition;
    private Quaternion builtRotation;
    private Sprite builtSprite;
    private bool rebuild;
    private bool hidToothSprite;

    public static Color BaseColor(Finish value) => value switch
    {
        Finish.Timber => new Color32(147, 90, 51, 255),
        Finish.PoolTile => Cream,
        Finish.IvoryTooth => new Color32(255, 248, 224, 255),
        Finish.PaintedMetal => Teal,
        Finish.FlagPole => new Color32(139, 158, 158, 255),
        Finish.Button => Color.white,
        _ => Brick
    };

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;

    public void Refresh()
    {
        rebuild = true;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        bool worldPattern = finish == Finish.Brick || finish == Finish.PoolFloor || finish == Finish.PoolTile;
        if (rebuild || builtSprite != source.sprite || builtScale != transform.lossyScale ||
            builtRotation != transform.rotation || (worldPattern && builtPosition != transform.position)) Build();
        if (art == null) return;

        bool visible = source.enabled && source.color.a > .001f;
        if (art.activeSelf != visible) art.SetActive(visible);
        Color tint = finish == Finish.Button ? source.color : new Color(1f, 1f, 1f, source.color.a);
        if (material.color != tint) material.color = tint;
        var renderer = art.GetComponent<MeshRenderer>();
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
    }

    private void Build()
    {
        rebuild = false;
        Clear();
        source = GetComponent<SpriteRenderer>();
        builtSprite = source.sprite;
        builtScale = transform.lossyScale;
        builtPosition = transform.position;
        builtRotation = transform.rotation;
        if (builtSprite == null) return;

        Bounds bounds = builtSprite.bounds;
        float w = Mathf.Abs(bounds.size.x * builtScale.x), h = Mathf.Abs(bounds.size.y * builtScale.y);
        if (w < .001f || h < .001f) return;
        var drawing = new SchoolSceneDrawing();
        var face = new Rect(-w / 2f, -h / 2f, w, h);
        Vector2 center = transform.TransformPoint(bounds.center);
        switch (finish)
        {
            case Finish.Brick:
                Masonry(drawing, face, center, false);
                break;
            case Finish.Timber:
                Timber(drawing, face);
                break;
            case Finish.PoolTile:
                Masonry(drawing, face, center, true);
                break;
            case Finish.PoolFloor:
                Masonry(drawing, face, center, false);
                float left = Mathf.Max(face.xMin, poolTileSpan.x - center.x);
                float right = Mathf.Min(face.xMax, poolTileSpan.y - center.x);
                if (right > left) Masonry(drawing, Rect.MinMaxRect(left, face.yMin, right, face.yMax), center, true);
                break;
            case Finish.IvoryTooth:
                Tooth(drawing, face, toothSlope);
                break;
            case Finish.Button:
                Bevel(drawing, face, new Color(.66f, .7f, .64f), Color.white);
                break;
            default:
                Metal(drawing, face, finish == Finish.FlagPole);
                break;
        }

        material = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Smile room surface finish",
            mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave
        };
        art = drawing.Build("Smile room surface finish", transform, material, source.sortingOrder, out mesh);
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(bounds.center.x + vertices[i].x / w * bounds.size.x,
                bounds.center.y + vertices[i].y / h * bounds.size.y, bounds.center.z);
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            Color[] colors = mesh.colors;
            for (int i = 0; i < colors.Length; i++) colors[i] = colors[i].linear;
            mesh.colors = colors;
        }
        art.transform.localPosition = new Vector3(0f, 0f, -.012f / Mathf.Max(.0001f, Mathf.Abs(builtScale.z)));
        if (finish == Finish.IvoryTooth && !source.forceRenderingOff)
        {
            source.forceRenderingOff = true;
            hidToothSprite = true;
        }
    }

    private void Masonry(SchoolSceneDrawing a, Rect face, Vector2 center, bool tile)
    {
        // Clip a world-aligned bond to the existing rectangle, including the rotated climb walls.
        // Brick rows stay horizontal and the art never extends into the playable clearance.
        float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 Rotate(Vector2 p, float radians) => new Vector2(
            p.x * Mathf.Cos(radians) - p.y * Mathf.Sin(radians),
            p.x * Mathf.Sin(radians) + p.y * Mathf.Cos(radians));
        var corners = new[] { face.min, new Vector2(face.xMin, face.yMax), face.max, new Vector2(face.xMax, face.yMin) };
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = -min;
        foreach (Vector2 corner in corners)
        {
            Vector2 p = center + Rotate(corner, angle);
            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
        }
        a.Rect(face.center.x, face.center.y, face.width, face.height,
            tile ? new Color32(185, 188, 169, 255) : Mortar);
        float row = tile ? .46f : .39f, length = tile ? .46f : .78f, gap = tile ? .024f : .027f;
        for (int y = Mathf.FloorToInt(min.y / row); y <= Mathf.CeilToInt(max.y / row); y++)
        {
            float stagger = !tile && (y & 1) != 0 ? length / 2f : 0f;
            for (int x = Mathf.FloorToInt((min.x - stagger) / length); x <= Mathf.CeilToInt((max.x - stagger) / length); x++)
            {
                float l = x * length + stagger + gap / 2f, r = l + length - gap;
                float b = y * row + gap / 2f, t = b + row - gap;
                var polygon = new List<Vector2>
                {
                    Rotate(new Vector2(l, b) - center, -angle), Rotate(new Vector2(l, t) - center, -angle),
                    Rotate(new Vector2(r, t) - center, -angle), Rotate(new Vector2(r, b) - center, -angle)
                };
                polygon = Clip(polygon, 0, face.xMin, true);
                polygon = Clip(polygon, 0, face.xMax, false);
                polygon = Clip(polygon, 1, face.yMin, true);
                polygon = Clip(polygon, 1, face.yMax, false);
                if (polygon.Count < 3) continue;
                bool alternate = ((x * 17 + y * 31) & 3) == 0;
                Color color = tile ? (alternate ? new Color32(233, 226, 203, 255) : Cream) : (alternate ? BrickLight : Brick);
                a.Polygon(color, polygon.ToArray());
            }
        }
        // A thin finish on the existing edge, entirely inside the source rectangle.
        float trim = Mathf.Min(.055f, face.height * .16f);
        a.Rect(face.center.x, face.yMax - trim / 2f, face.width, trim,
            tile ? new Color32(255, 246, 221, 255) : new Color32(207, 157, 120, 255));
    }

    private static List<Vector2> Clip(List<Vector2> input, int axis, float edge, bool greater)
    {
        var result = new List<Vector2>();
        if (input.Count == 0) return result;
        Vector2 previous = input[input.Count - 1];
        bool Inside(Vector2 p) => greater ? p[axis] >= edge : p[axis] <= edge;
        foreach (Vector2 current in input)
        {
            bool a = Inside(previous), b = Inside(current);
            if (a != b)
            {
                float fraction = (edge - previous[axis]) / (current[axis] - previous[axis]);
                result.Add(Vector2.LerpUnclamped(previous, current, fraction));
            }
            if (b) result.Add(current);
            previous = current;
        }
        return result;
    }

    private static void Timber(SchoolSceneDrawing a, Rect r)
    {
        Bevel(a, r, new Color32(117, 67, 42, 255), new Color32(181, 123, 75, 255));
        float margin = Mathf.Min(.12f, r.width * .1f);
        for (int row = 0; row < 2; row++)
        {
            float y = r.center.y + (row == 0 ? -.16f : .16f) * r.height;
            a.Line(new Vector2(r.xMin + margin, y), new Vector2(r.xMax - margin, y),
                Mathf.Min(.022f, r.height * .07f), new Color32(143, 88, 53, 255));
        }
        float nail = Mathf.Min(.035f, r.height * .12f);
        a.Circle(new Vector2(r.xMin + margin, r.center.y), nail, new Color32(92, 77, 64, 255));
        a.Circle(new Vector2(r.xMax - margin, r.center.y), nail, new Color32(92, 77, 64, 255));
    }

    private static void Tooth(SchoolSceneDrawing a, Rect r, float slope)
    {
        // The painted teeth share a flat upper edge and follow the grin along the bottom.
        const int segments = 32;
        float radius = Mathf.Min(r.width * .08f, r.height * .05f);
        Vector2 previousBottom = default, previousTop = default;
        for (int i = 0; i <= segments; i++)
        {
            float u = (float)i / segments;
            float x = Mathf.Lerp(r.xMin, r.xMax, u);
            float edgeDistance = Mathf.Min(u, 1f - u) * r.width;
            float inset = Mathf.Max(0f, radius - edgeDistance);
            float corner = radius - Mathf.Sqrt(Mathf.Max(0f, radius * radius - inset * inset));
            float offset = u - .5f;
            float rise = r.width * (Mathf.Abs(slope) * .5f + slope * offset + .16f * offset * offset);
            Vector2 bottom = new Vector2(x, r.yMin + rise + corner);
            Vector2 top = new Vector2(x, r.yMax - corner);
            if (i > 0)
                a.ShadedQuad(previousBottom, previousTop, top, bottom,
                    new Color32(197, 195, 195, 255), new Color32(202, 200, 200, 255));
            previousBottom = bottom;
            previousTop = top;
        }
    }

    private static void Metal(SchoolSceneDrawing a, Rect r, bool pole)
    {
        Bevel(a, r, pole ? new Color32(96, 119, 121, 255) : new Color32(43, 80, 79, 255),
            pole ? new Color32(179, 195, 189, 255) : Teal);
        if (pole) return;
        float inset = Mathf.Min(.12f, Mathf.Min(r.width, r.height) * .18f);
        Color seam = new Color32(99, 137, 128, 255);
        for (float y = r.yMin + 1.25f; y < r.yMax - inset; y += 1.25f)
            a.Line(new Vector2(r.xMin + inset, y), new Vector2(r.xMax - inset, y), .022f, seam);
        float bolt = Mathf.Min(.035f, inset * .35f);
        foreach (float x in new[] { r.xMin + inset, r.xMax - inset })
            foreach (float y in new[] { r.yMin + inset, r.yMax - inset }) a.Circle(new Vector2(x, y), bolt, Cream);
    }

    private static void Bevel(SchoolSceneDrawing a, Rect r, Color dark, Color light)
    {
        a.ShadedRect(r.xMin, r.yMin, r.xMax, r.yMax, dark, light);
        float edge = Mathf.Min(.06f, Mathf.Min(r.width, r.height) * .16f);
        a.Rect(r.center.x, r.yMax - edge / 2f, r.width, edge, Color.Lerp(light, Color.white, .3f));
        a.Rect(r.center.x, r.yMin + edge / 2f, r.width, edge, dark);
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();

    private void Clear()
    {
        if (hidToothSprite && source != null) source.forceRenderingOff = false;
        hidToothSprite = false;
        SchoolSceneDrawing.Release(art);
        SchoolSceneDrawing.Release(mesh);
        SchoolSceneDrawing.Release(material);
        art = null; mesh = null; material = null;
    }
}
