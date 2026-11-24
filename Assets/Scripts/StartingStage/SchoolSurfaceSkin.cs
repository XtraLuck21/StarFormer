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
    public enum Style { PlaygroundGround, ConcretePipe, SchoolBrick, BannerBeam, RoofEaves }

    [SerializeField] private Style style;
    [Tooltip("World height of the running-track surface for PlaygroundGround.")]
    [SerializeField] private float surfaceTop = -23.25f;
    [Tooltip("Optional dark pipe interior drawn behind gameplay (world rect). Zero size = none.")]
    [SerializeField] private Rect backdrop;
    [SerializeField] private int backdropOrder = -1;

    private static readonly Color Track = new Color32(196, 108, 90, 255);
    private static readonly Color TrackEdge = new Color32(228, 150, 124, 255);
    private static readonly Color Chalk = new Color32(246, 236, 214, 255);
    private static readonly Color SoilTop = new Color32(150, 108, 78, 255);
    private static readonly Color SoilDeep = new Color32(96, 68, 52, 255);
    private static readonly Color Strata = new Color32(118, 84, 62, 255);
    private static readonly Color PebbleLight = new Color32(186, 166, 144, 255);
    private static readonly Color PebbleDark = new Color32(132, 116, 102, 255);
    private static readonly Color Concrete = new Color32(184, 182, 172, 255);
    private static readonly Color ConcreteShade = new Color32(150, 150, 142, 255);
    private static readonly Color ConcreteJoint = new Color32(120, 122, 118, 255);
    private static readonly Color PipeInside = new Color32(70, 84, 92, 255);
    private static readonly Color PipeInsideLight = new Color32(98, 112, 118, 255);
    private static readonly Color Brick = new Color32(178, 98, 80, 255);
    private static readonly Color BrickLight = new Color32(196, 118, 96, 255);
    private static readonly Color Mortar = new Color32(222, 198, 174, 255);
    private static readonly Color Teal = new Color32(87, 151, 149, 255);
    private static readonly Color Cream = new Color32(255, 234, 190, 255);
    private static readonly Color Honey = new Color32(244, 181, 69, 255);
    private static readonly Color Slate = new Color32(92, 108, 116, 255);
    private static readonly Color SlateDark = new Color32(70, 84, 92, 255);

    /// <summary>The flat color a blank source block should use (also tints shatter fragments).</summary>
    public static Color BaseColor(Style style) => style switch
    {
        Style.PlaygroundGround => (Color)new Color32(124, 88, 64, 255),
        Style.ConcretePipe => Concrete,
        Style.SchoolBrick => Brick,
        Style.BannerBeam => Teal,
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
        Vector3 scale = transform.lossyScale;
        float w = Mathf.Abs(sb.size.x * scale.x), h = Mathf.Abs(sb.size.y * scale.y);
        if (w < 0.01f || h < 0.01f) return;
        Vector2 center = transform.TransformPoint(sb.center);
        bool upright = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f)) < 0.5f;

        var art = new SchoolSceneDrawing();
        switch (style)
        {
            case Style.PlaygroundGround: Ground(art, w, h, center, upright); break;
            case Style.ConcretePipe: Pipe(art, w, h); break;
            case Style.SchoolBrick: Bricks(art, w, h, center, upright); break;
            case Style.BannerBeam: Banner(art, w, h); break;
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
            PipeInterior(inside, backdrop);
            backMaterial = NewMaterial("School pipe interior");
            back = inside.Build("Drain pipe interior", null, backMaterial, backdropOrder, out backMesh);
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
        float l = -w / 2, r = w / 2, b = -h / 2, t = h / 2;
        float worldBottom = center.y + b, worldTop = center.y + t;
        // Soil darkens with depth below the track, continuous across neighboring blocks.
        Color SoilAt(float worldY) => Color.Lerp(SoilDeep, SoilTop, Mathf.InverseLerp(surfaceTop - 4f, surfaceTop - .3f, worldY));
        a.ShadedRect(l, b, r, t, SoilAt(worldBottom), SoilAt(worldTop));
        if (!upright) return;

        // Wavy strata at fixed world depths.
        foreach (float depth in new[] { 1.05f, 2.05f, 3.15f })
        {
            float y = surfaceTop - depth - center.y;
            if (y < b + .08f || y > t - .08f) continue;
            const float step = .5f;
            for (float x = l; x < r - .01f; x += step)
            {
                float x2 = Mathf.Min(r, x + step);
                float ya = y + Mathf.Sin((center.x + x) * 1.3f + depth) * .06f;
                float yb = y + Mathf.Sin((center.x + x2) * 1.3f + depth) * .06f;
                a.Line(new Vector2(x, ya), new Vector2(x2, yb), .05f, Strata);
            }
        }

        // Pebbles on a world grid, so they never jump when a block is resized.
        const float cell = .85f;
        int x0 = Mathf.FloorToInt((center.x + l) / cell), x1 = Mathf.CeilToInt((center.x + r) / cell);
        int y0 = Mathf.FloorToInt((center.y + b) / cell), y1 = Mathf.CeilToInt((center.y + t) / cell);
        for (int ix = x0; ix <= x1; ix++)
        for (int iy = y0; iy <= y1; iy++)
        {
            uint hash = Hash(ix, iy);
            if (hash % 3 == 0) continue;
            float jx = (hash & 255) / 255f, jy = ((hash >> 8) & 255) / 255f;
            float radius = .07f + ((hash >> 16) & 7) * .014f;
            Vector2 p = new Vector2((ix + jx) * cell - center.x, (iy + jy) * cell - center.y);
            if (p.x - radius < l + .04f || p.x + radius > r - .04f || p.y - radius < b + .04f || p.y + radius > t - .04f)
                continue;
            if (center.y + p.y + radius > surfaceTop - .34f) continue;
            Ellipse(a, p, radius * 1.35f, radius, (hash & 1) == 0 ? PebbleLight : PebbleDark);
        }

        // Running track along the room floor, with a white lane line.
        float trackTop = surfaceTop - center.y, trackBottom = trackTop - .26f;
        float bandTop = Mathf.Min(t, trackTop), bandBottom = Mathf.Max(b, trackBottom);
        if (bandTop > bandBottom + .005f)
        {
            a.ShadedRect(l, bandBottom, r, bandTop, Track, Track);
            float edge = Mathf.Max(bandBottom, trackTop - .045f);
            if (bandTop > edge) a.ShadedRect(l, edge, r, bandTop, TrackEdge, TrackEdge);
            float lane = trackTop - .14f;
            if (lane > b + .02f && lane < t - .02f) a.Rect(0f, lane, w, .035f, Chalk);
            // A darker lip where the track meets the soil.
            float lip = trackBottom;
            if (lip > b && lip < t) a.Rect(0f, lip + .015f, w, .03f, new Color32(150, 82, 68, 255));
        }
    }

    private static void Pipe(SchoolSceneDrawing a, float w, float h)
    {
        bool alongX = w >= h;
        float length = alongX ? w : h, thick = alongX ? h : w;
        a.ShadedRect(-w / 2, -h / 2, w / 2, h / 2, alongX ? ConcreteShade : Concrete, Concrete);
        // A lighter top face and ring joints across the long axis.
        if (alongX) a.Rect(0f, h / 2 - Mathf.Min(.06f, h * .2f), w, Mathf.Min(.06f, h * .2f) * 2f, new Color32(206, 204, 196, 255));
        else a.Rect(-w / 2 + Mathf.Min(.05f, w * .2f), 0f, Mathf.Min(.05f, w * .2f) * 2f, h, new Color32(206, 204, 196, 255));
        int joints = Mathf.Max(1, Mathf.RoundToInt(length / 1.1f));
        for (int i = 1; i < joints; i++)
        {
            float s = -length / 2 + length * i / joints;
            if (alongX) a.Rect(s, 0f, .05f, h, ConcreteJoint);
            else a.Rect(0f, s, w, .05f, ConcreteJoint);
        }
        // Small bolts at each joint make it read as a built pipe, not a rock.
        float boltOffset = thick * .25f;
        for (int i = 1; i < joints; i++)
        {
            float s = -length / 2 + length * i / joints;
            Vector2 p = alongX ? new Vector2(s + .1f, boltOffset) : new Vector2(boltOffset, s + .1f);
            if (thick > .3f) Ellipse(a, p, .035f, .035f, ConcreteJoint);
        }
    }

    private static void PipeInterior(SchoolSceneDrawing a, Rect r)
    {
        a.ShadedRect(r.xMin, r.yMin, r.xMax, r.yMax, PipeInside, PipeInsideLight);
        // Curved rings suggest the inside of a round drain pipe.
        for (float x = r.xMin + .8f; x < r.xMax - .3f; x += 1.6f)
        {
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps, t1 = (i + 1) / (float)steps;
                float y0 = Mathf.Lerp(r.yMin, r.yMax, t0), y1 = Mathf.Lerp(r.yMin, r.yMax, t1);
                float bulge0 = Mathf.Sin(t0 * Mathf.PI) * .22f, bulge1 = Mathf.Sin(t1 * Mathf.PI) * .22f;
                a.Line(new Vector2(x + bulge0, y0), new Vector2(x + bulge1, y1), .07f, PipeInsideLight);
            }
        }
        // A thin trickle of water along the bottom.
        a.Rect(r.center.x, r.yMin + .05f, r.width, .08f, new Color32(126, 168, 182, 255));
    }

    private static void Bricks(SchoolSceneDrawing a, float w, float h, Vector2 center, bool upright)
    {
        float l = -w / 2, r = w / 2, b = -h / 2, t = h / 2;
        a.ShadedRect(l, b, r, t, Mortar, Mortar);
        const float row = .3f, length = .62f, gap = .045f;
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

    private static void Banner(SchoolSceneDrawing a, float w, float h)
    {
        a.ShadedRect(-w / 2, -h / 2, w / 2, h / 2, Teal, Teal);
        float trim = Mathf.Min(.07f, h * .15f);
        a.Rect(0f, h / 2 - trim, w, trim, Cream);
        a.Rect(0f, -h / 2 + trim, w, trim, Cream);
        int stars = Mathf.Max(1, Mathf.FloorToInt(w / 2.2f));
        for (int i = 0; i < stars; i++)
            a.Star(new Vector2(-w / 2 + w * (i + .5f) / stars, 0f), h * .28f, Honey);
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
