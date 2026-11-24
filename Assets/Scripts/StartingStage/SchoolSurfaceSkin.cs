using UnityEngine;

/// <summary>
/// Draws a school-themed surface over a plain square SpriteRenderer (floors, walls, covers) without
/// touching its collider. The skin follows the source renderer: hidden when it is disabled, faded
/// with its alpha (tunnel covers) and rebuilt when its transform changes (the extending floor).
/// Ground patterns use world coordinates, so neighboring blocks join without seams.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SchoolSurfaceSkin : MonoBehaviour
{
    public enum Style { PlaygroundGround, EarthenTunnel, SchoolBrick, BannerBeam, RoofEaves, WoodenBeam }

    [SerializeField] private Style style;
    [Tooltip("World height of the running-track surface for PlaygroundGround.")]
    [SerializeField] private float surfaceTop = -23.25f;
    [Tooltip("Optional earthen tunnel interior drawn behind gameplay (world rect). Zero size = none.")]
    [SerializeField] private Rect backdrop;
    [SerializeField] private int backdropOrder = -1;
    [SerializeField] private Vector2 spritePadding;

    private static readonly Color Track = new Color32(162, 73, 61, 255);
    private static readonly Color TrackShade = new Color32(135, 56, 48, 255);
    private static readonly Color Chalk = new Color32(246, 236, 214, 255);
    private static readonly Color Earth = new Color32(161, 117, 77, 255);
    private static readonly Color EarthShade = new Color32(113, 77, 54, 255);
    private static readonly Color EarthEdge = new Color32(181, 140, 93, 255);
    private static readonly Color TunnelDeep = new Color32(72, 49, 39, 255);
    private static readonly Color TunnelSoil = new Color32(111, 80, 57, 255);
    private static readonly Color Brick = new Color32(150, 71, 58, 255);
    private static readonly Color BrickLight = new Color32(165, 82, 65, 255);
    private static readonly Color Mortar = new Color32(184, 130, 109, 255);
    private static readonly Color Teal = new Color32(55, 104, 102, 255);
    private static readonly Color Cream = new Color32(255, 234, 190, 255);
    private static readonly Color Slate = new Color32(92, 108, 116, 255);
    private static readonly Color SlateDark = new Color32(70, 84, 92, 255);
    private const float TrackLeft = 16.7f;
    private const float LaneWidth = 22.25f / 7f;
    private static readonly Vector2[][] LaneDigits =
    {
        new[] { new Vector2(-.16f, .19f), new Vector2(0f, .33f), new Vector2(0f, -.33f), new Vector2(-.19f, -.33f), new Vector2(.20f, -.33f) },
        new[] { new Vector2(-.21f, .22f), new Vector2(-.13f, .33f), new Vector2(.13f, .33f), new Vector2(.22f, .21f), new Vector2(.19f, .08f), new Vector2(-.21f, -.33f), new Vector2(.23f, -.33f) },
        new[] { new Vector2(-.21f, .25f), new Vector2(-.1f, .33f), new Vector2(.14f, .33f), new Vector2(.23f, .2f), new Vector2(.16f, .073f), new Vector2(0f, .02f), new Vector2(.17f, -.04f), new Vector2(.24f, -.18f), new Vector2(.13f, -.33f), new Vector2(-.13f, -.33f), new Vector2(-.22f, -.24f) },
        new[] { new Vector2(.10f, .33f), new Vector2(-.23f, -.10f), new Vector2(.26f, -.10f), new Vector2(.13f, -.10f), new Vector2(.13f, .18f), new Vector2(.13f, -.33f) },
        new[] { new Vector2(.22f, .33f), new Vector2(-.17f, .33f), new Vector2(-.21f, .02f), new Vector2(.13f, .02f), new Vector2(.23f, -.1f), new Vector2(.22f, -.22f), new Vector2(.10f, -.33f), new Vector2(-.13f, -.33f), new Vector2(-.22f, -.26f) },
        new[] { new Vector2(.17f, .32f), new Vector2(-.02f, .32f), new Vector2(-.16f, .22f), new Vector2(-.22f, .07f), new Vector2(-.22f, -.21f), new Vector2(-.11f, -.33f), new Vector2(.12f, -.33f), new Vector2(.22f, -.20f), new Vector2(.20f, -.05f), new Vector2(.09f, .02f), new Vector2(-.22f, .02f) },
        new[] { new Vector2(-.23f, .33f), new Vector2(.23f, .33f), new Vector2(-.08f, -.33f) },
    };

    /// <summary>The flat color a blank source block should use (also tints shatter fragments).</summary>
    public static Color BaseColor(Style style) => style switch
    {
        Style.PlaygroundGround => Track,
        Style.EarthenTunnel => Earth,
        Style.SchoolBrick => Brick,
        Style.BannerBeam => Teal,
        Style.WoodenBeam => new Color32(147, 90, 51, 255),
        _ => Slate,
    };

    private SpriteRenderer source;
    private GameObject skin, back;
    private Mesh skinMesh, backMesh;
    private Material material, backMaterial;
    private Matrix4x4 builtMatrix;
    private float shownAlpha = -1f;
    private bool rebuild;

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;

    /// <summary>Rebuilds now instead of on the next editor tick (used by the Level02 builder).</summary>
    public void Refresh()
    {
        rebuild = true;
        LateUpdate();
    }

    internal void ApplyTo(GameObject surface)
    {
        var skin = surface.AddComponent<SchoolSurfaceSkin>();
        skin.style = style;
        skin.surfaceTop = surfaceTop;
        skin.spritePadding = spritePadding;
        skin.Refresh();
    }

    private void LateUpdate()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        if (rebuild || transform.localToWorldMatrix != builtMatrix) Build();
        if (skin == null) return;
        bool visible = source.enabled && source.color.a > 0.001f;
        if (skin.activeSelf != visible) skin.SetActive(visible);
        if (!Mathf.Approximately(shownAlpha, source.color.a))
        {
            shownAlpha = source.color.a;
            material.color = new Color(1f, 1f, 1f, shownAlpha);
        }
    }

    private void Build()
    {
        rebuild = false;
        Clear();
        source = GetComponent<SpriteRenderer>();
        builtMatrix = transform.localToWorldMatrix;
        if (source.sprite == null) return;

        Bounds sb = source.sprite.bounds;
        sb.Expand((Vector3)(-2f * spritePadding / source.sprite.pixelsPerUnit));
        Vector3 scale = transform.lossyScale;
        float w = Mathf.Abs(sb.size.x * scale.x), h = Mathf.Abs(sb.size.y * scale.y);
        if (w < 0.01f || h < 0.01f) return;
        Vector2 center = transform.TransformPoint(sb.center);
        bool upright = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f)) < 0.5f;

        var art = new SchoolSceneDrawing();
        switch (style)
        {
            case Style.PlaygroundGround: Ground(art, w, h, center, upright); break;
            case Style.EarthenTunnel: EarthSurface(art, w, h); break;
            case Style.SchoolBrick: Bricks(art, w, h, center, upright); break;
            case Style.BannerBeam: Banner(art, w, h); break;
            case Style.WoodenBeam: Wood(art, w, h); break;
            default: Eaves(art, w, h); break;
        }

        material = NewMaterial("School surface skin");
        skin = art.Build("School surface skin", transform, material, source.sortingOrder, out skinMesh);
        skin.GetComponent<MeshRenderer>().sortingLayerID = source.sortingLayerID;
        // Drawn in world-unit (u, v) space around the block center; map into the block's local space.
        ToLocal(skinMesh, sb, w, h);
        // Slightly nearer the camera, so the skin always wins the tie with its own source sprite.
        skin.transform.localPosition = new Vector3(0f, 0f, -0.01f / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        shownAlpha = -1f;

        if (backdrop.width > 0.01f && backdrop.height > 0.01f)
        {
            var inside = new SchoolSceneDrawing();
            EarthInterior(inside, backdrop);
            backMaterial = NewMaterial("School earthen tunnel interior");
            back = inside.Build("Earthen tunnel interior", null, backMaterial, backdropOrder, out backMesh);
            back.GetComponent<MeshRenderer>().sortingLayerID = source.sortingLayerID;
            FixColors(backMesh);
        }
    }

    private static Material NewMaterial(string name) => new Material(Shader.Find("Sprites/Default"))
    {
        name = name,
        mainTexture = Texture2D.whiteTexture,
        hideFlags = HideFlags.HideAndDontSave
    };

    private void ToLocal(Mesh mesh, Bounds sb, float w, float h)
    {
        Vector3[] v = mesh.vertices;
        for (int i = 0; i < v.Length; i++)
            v[i] = new Vector3(sb.center.x + v[i].x / w * sb.size.x, sb.center.y + v[i].y / h * sb.size.y, 0f);
        mesh.vertices = v;
        mesh.RecalculateBounds();
        FixColors(mesh);
    }

    private static void FixColors(Mesh mesh)
    {
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) return;
        Color[] c = mesh.colors;
        for (int i = 0; i < c.Length; i++) c[i] = c[i].linear;
        mesh.colors = c;
    }

    // ---------- Styles. Coordinates are world units relative to the block center. ----------

    private void Ground(SchoolSceneDrawing a, float w, float h, Vector2 center, bool upright)
    {
        float left = -w * .5f, right = w * .5f, bottom = -h * .5f, top = h * .5f;
        Color TrackAt(float worldY) => Color.Lerp(TrackShade, Track, Mathf.InverseLerp(surfaceTop - 4.2f, surfaceTop, worldY));
        a.ShadedRect(left, bottom, right, top, TrackAt(center.y + bottom), TrackAt(center.y + top));
        if (!upright) return;

        Rect face = Rect.MinMaxRect(left, bottom, right, top);
        TrackGrain(a, face, center);
        // Align lanes in world space so the covers and floor sections read as one track.
        const float halfStripe = .042f;
        for (int i = 0; i < LaneDigits.Length; i++)
        {
            float boundary = TrackLeft + (i + 1) * LaneWidth - center.x;
            float stripeLeft = Mathf.Max(left, boundary - halfStripe), stripeRight = Mathf.Min(right, boundary + halfStripe);
            if (stripeRight > stripeLeft)
                a.Rect((stripeLeft + stripeRight) * .5f, 0f, stripeRight - stripeLeft, h, Chalk);

            var number = new Vector2(boundary - LaneWidth * .5f, surfaceTop - 3.38f - center.y);
            // Covers and their backing blocks may overlap, so skip marks that straddle an edge.
            if (!face.Contains(number - new Vector2(.56f, .43f)) || !face.Contains(number + new Vector2(.56f, .65f)))
                continue;
            a.Rect(number.x, number.y + .60f, 1.10f, .046f, Chalk);
            Vector2[] stroke = LaneDigits[i];
            for (int j = 1; j < stroke.Length; j++)
            {
                a.Line(number + stroke[j - 1], number + stroke[j], .10f, Chalk);
                a.Circle(number + stroke[j], .05f, Chalk);
            }
        }
    }

    private static void TrackGrain(SchoolSceneDrawing a, Rect face, Vector2 center)
    {
        const float cell = .18f;
        int firstX = Mathf.FloorToInt((face.xMin + center.x) / cell);
        int lastX = Mathf.FloorToInt((face.xMax + center.x) / cell);
        int firstY = Mathf.FloorToInt((face.yMin + center.y) / cell);
        int lastY = Mathf.FloorToInt((face.yMax + center.y) / cell);
        Color bright = new Color32(181, 95, 80, 255);
        Color dark = new Color32(115, 48, 43, 255);
        for (int y = firstY; y <= lastY; y++)
        for (int x = firstX; x <= lastX; x++)
        {
            uint hash = Hash(x, y);
            if ((hash & 7) >= 5) continue;
            var point = new Vector2((x + ((hash >> 8) & 255) / 256f) * cell,
                (y + ((hash >> 16) & 255) / 256f) * cell) - center;
            float size = .018f + ((hash >> 24) & 7) * .005f;
            Vector2 radius = new Vector2(size * .5f, size * .38f);
            if (!face.Contains(point - radius) || !face.Contains(point + radius)) continue;
            a.Rect(point.x, point.y, radius.x * 2f, radius.y * 2f, (hash & 8) == 0 ? bright : dark);
        }
    }

    private static void EarthSurface(SchoolSceneDrawing a, float w, float h)
    {
        float left = -w * .5f, right = w * .5f, top = h * .5f;
        a.ShadedRect(left, -top, right, top, EarthShade, Earth);
        if (w < h) return;

        float rim = Mathf.Min(.105f, h * .25f);
        a.Rect(0f, top - rim * .5f, w, rim, EarthEdge);
        for (float x = left + .24f; x < right - .15f; x += .56f)
        {
            uint speck = Hash(Mathf.RoundToInt(x * 12f), 7);
            float y = Mathf.Lerp(-top + .07f, top - rim - .05f, (speck & 255) / 255f);
            Ellipse(a, new Vector2(x, y), .065f, .030f, (speck & 256) == 0 ? EarthShade : EarthEdge);
        }
    }

    private static void EarthInterior(SchoolSceneDrawing a, Rect area)
    {
        a.ShadedRect(area.xMin, area.yMin, area.xMax, area.yMax, TunnelDeep, TunnelSoil);
        Color stratum = new Color32(92, 63, 49, 255);
        for (int band = 1; band <= 3; band++)
        {
            float y = area.yMin + area.height * band * .24f;
            for (float x = area.xMin; x < area.xMax; x += .36f)
            {
                float next = Mathf.Min(x + .36f, area.xMax);
                a.Line(new Vector2(x, y + .074f * Mathf.Sin(x * 1.25f + band)),
                    new Vector2(next, y + .074f * Mathf.Sin(next * 1.25f + band)), .042f, stratum);
            }
        }
        for (float x = area.xMin + .38f; x < area.xMax - .16f; x += .87f)
        {
            uint fleck = Hash(Mathf.RoundToInt(x * 12f), 16);
            float y = Mathf.Lerp(area.yMin + .13f, area.yMax - .13f, (fleck & 255) / 255f);
            Ellipse(a, new Vector2(x, y), .095f, .048f, new Color32(123, 91, 68, 255));
        }
    }

    private static void Bricks(SchoolSceneDrawing a, float w, float h, Vector2 center, bool upright)
    {
        float l = -w / 2, r = w / 2, b = -h / 2, t = h / 2;
        a.ShadedRect(l, b, r, t, Mortar, Mortar);
        const float row = .39f, length = .78f, gap = .027f;
        int first = Mathf.FloorToInt((center.y + b) / row), last = Mathf.CeilToInt((center.y + t) / row);
        for (int iy = first; iy <= last; iy++)
        {
            float yb = Mathf.Max(b, iy * row - center.y + gap / 2), yt = Mathf.Min(t, (iy + 1) * row - center.y - gap / 2);
            if (yt <= yb) continue;
            float offset = (iy & 1) == 0 ? 0f : length / 2;
            int bx0 = Mathf.FloorToInt((center.x + l - offset) / length) - 1, bx1 = Mathf.CeilToInt((center.x + r - offset) / length);
            for (int ix = bx0; ix <= bx1; ix++)
            {
                float xl = Mathf.Max(l, ix * length + offset - center.x + gap / 2);
                float xr = Mathf.Min(r, (ix + 1) * length + offset - center.x - gap / 2);
                if (xr <= xl) continue;
                a.ShadedRect(xl, yb, xr, yt, Brick, (Hash(ix, iy) & 3) == 0 ? BrickLight : Brick);
            }
        }
        if (!upright) return;
        // Cream stone cap on top, like a school gate pillar.
        a.Rect(0f, t - .07f, w, .14f, Cream);
    }

    private static void Wood(SchoolSceneDrawing a, float w, float h)
    {
        float left = -w * .5f, right = w * .5f, top = h * .5f;
        a.ShadedRect(left, -top, right, top, new Color32(117, 67, 42, 255), new Color32(181, 123, 75, 255));
        a.Rect(0f, top - .055f, w, .11f, new Color32(205, 151, 98, 255));
        a.Rect(0f, -top + .065f, w, .13f, new Color32(101, 59, 39, 255));
        Color grain = new Color32(140, 87, 54, 255);
        for (int row = 0; row < 3; row++)
        {
            float y = (row - 1f) * h * .19f;
            for (float x = left; x < right; x += .65f)
            {
                float next = Mathf.Min(x + .65f, right);
                a.Line(new Vector2(x, y + .045f * Mathf.Sin(x * .8f + row)),
                    new Vector2(next, y + .045f * Mathf.Sin(next * .8f + row)), .027f, grain);
            }
        }
    }

    private static void Banner(SchoolSceneDrawing a, float w, float h)
    {
        a.ShadedRect(-w / 2, -h / 2, w / 2, h / 2, Teal, Teal);
        float trim = Mathf.Min(.07f, h * .15f);
        a.Rect(0f, h / 2 - trim, w, trim, new Color32(112, 146, 137, 255));
    }

    private static void Eaves(SchoolSceneDrawing a, float w, float h)
    {
        a.ShadedRect(-w / 2, -h / 2, w / 2, h / 2, SlateDark, Slate);
        a.Rect(0f, h / 2 - .04f, w, .08f, Cream);
        // Scalloped tile edge along the underside.
        float radius = Mathf.Min(.2f, h * .4f);
        for (float x = -w / 2 + radius; x < w / 2 - radius * .5f; x += radius * 2f)
        {
            Ellipse(a, new Vector2(x, -h / 2 + radius * .55f), radius, radius * .7f, SlateDark);
            Ellipse(a, new Vector2(x, -h / 2 + radius * .7f), radius * .8f, radius * .5f, Slate);
        }
    }

    private static void Ellipse(SchoolSceneDrawing a, Vector2 c, float rx, float ry, Color color)
    {
        var points = new Vector2[12];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / points.Length;
            points[i] = c + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);
        }
        a.Polygon(color, points);
    }

    private static uint Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();

    private void Clear()
    {
        SchoolSceneDrawing.Release(skin);
        SchoolSceneDrawing.Release(skinMesh);
        SchoolSceneDrawing.Release(material);
        SchoolSceneDrawing.Release(back);
        SchoolSceneDrawing.Release(backMesh);
        SchoolSceneDrawing.Release(backMaterial);
        skin = back = null;
        skinMesh = backMesh = null;
        material = backMaterial = null;
    }
}
