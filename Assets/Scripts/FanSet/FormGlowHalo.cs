using UnityEngine;

public enum FormHaloShape
{
    Auto,
    Circle,
    Box,
    Star,
    Hexagon
}

/// <summary>
/// Breathing outline glow. Attach on a form root; the band follows that
/// form's silhouette (star / box / circle / hexagon). Hidden while possessed.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class FormGlowHalo : MonoBehaviour
{
    [SerializeField] private Color haloColor = Color.white;

    [Header("Shape")]
    [SerializeField] private FormHaloShape shape = FormHaloShape.Auto;
    [SerializeField] private float gap = 0f;
    [SerializeField] private float glowThickness = 0.2f;

    [Header("Breathing")]
    [SerializeField, Min(0f)] private float pulseSpeed = 6f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.8f;

    private const int Resolution = 256;

    private PossessableBody possessable;
    private SpriteRenderer haloRenderer;
    private Texture2D haloTexture;
    private Sprite haloSprite;
    private Vector2 bodyHalf;
    private FormHaloShape bakedShape;
    private Sprite lastBodySprite;

    private void OnEnable()
    {
        possessable = GetComponent<PossessableBody>();
        lastBodySprite = CurrentBodySprite();
        bodyHalf = EstimateBodyHalf();
        BuildHalo();
    }

    private void LateUpdate()
    {
        // Visuals (StarVisual / FanVisual) may generate their sprite after
        // this component enables. Rebuild when the body sprite changes.
        Sprite current = CurrentBodySprite();

        if (current != lastBodySprite)
        {
            lastBodySprite = current;
            bodyHalf = EstimateBodyHalf();
            BuildHalo();
        }
    }

    private Sprite CurrentBodySprite()
    {
        SpriteRenderer sr = FindBodyRenderer();
        return sr != null ? sr.sprite : null;
    }

    private void OnDisable()
    {
        ReleaseHalo();
    }

    private void OnValidate()
    {
        gap = Mathf.Max(0f, gap);
        glowThickness = Mathf.Max(0.05f, glowThickness);

        if (isActiveAndEnabled)
        {
            bodyHalf = EstimateBodyHalf();
            BuildHalo();
        }
    }

    private void Update()
    {
        if (haloRenderer == null)
        {
            return;
        }

        bool show = possessable == null || !possessable.IsControlled;
        haloRenderer.enabled = show;

        if (!show)
        {
            return;
        }

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        wave = wave * wave * (3f - 2f * wave);

        Color color = haloColor;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
        haloRenderer.color = color;
    }

    private void BuildHalo()
    {
        ReleaseHalo();
        bakedShape = ResolveShape();

        Vector2 shapeHalf = bodyHalf + Vector2.one * gap;
        Vector2 totalHalf = shapeHalf + Vector2.one * glowThickness;

        haloTexture = CreateOutlineTexture(bakedShape, shapeHalf, totalHalf);
        haloTexture.name = "FormHaloTexture";
        haloTexture.hideFlags = HideFlags.HideAndDontSave;

        haloSprite = Sprite.Create(
            haloTexture,
            new Rect(0f, 0f, Resolution, Resolution),
            new Vector2(0.5f, 0.5f),
            Resolution
        );

        haloSprite.name = "FormHaloSprite";
        haloSprite.hideFlags = HideFlags.HideAndDontSave;

        Transform haloTransform = transform.Find("Halo");

        if (haloTransform == null)
        {
            GameObject go = new GameObject("Halo");
            haloTransform = go.transform;
            haloTransform.SetParent(transform, false);
        }

        haloTransform.localPosition = Vector3.zero;
        haloTransform.localRotation = Quaternion.identity;
        haloTransform.localScale = new Vector3(totalHalf.x * 2f, totalHalf.y * 2f, 1f);

        haloRenderer = haloTransform.GetComponent<SpriteRenderer>();

        if (haloRenderer == null)
        {
            haloRenderer = haloTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        haloRenderer.sprite = haloSprite;
        haloRenderer.color = haloColor;
        haloRenderer.sortingOrder = -1;
    }

    private Vector2 EstimateBodyHalf()
    {
        SpriteRenderer sr = FindBodyRenderer();

        if (sr != null && sr.sprite != null)
        {
            // Prefer opaque art bounds: generated sprites have transparent
            // padding, and sprite.bounds would leave a gap around the form.
            Vector2 artHalf = OpaqueSpriteHalf(sr.sprite);

            if (artHalf.x > 0.01f && artHalf.y > 0.01f)
            {
                Vector3 s = sr.transform.lossyScale;
                Vector3 root = transform.lossyScale;
                float sx = Mathf.Abs(root.x) > 1e-4f ? Mathf.Abs(root.x) : 1f;
                float sy = Mathf.Abs(root.y) > 1e-4f ? Mathf.Abs(root.y) : 1f;

                return new Vector2(
                    Mathf.Abs(artHalf.x * s.x / sx),
                    Mathf.Abs(artHalf.y * s.y / sy)
                );
            }

            Bounds b = sr.sprite.bounds;
            Vector3 ls = sr.transform.lossyScale;
            Vector3 rs = transform.lossyScale;
            float fsx = Mathf.Abs(rs.x) > 1e-4f ? Mathf.Abs(rs.x) : 1f;
            float fsy = Mathf.Abs(rs.y) > 1e-4f ? Mathf.Abs(rs.y) : 1f;

            return new Vector2(
                Mathf.Abs(b.size.x * ls.x * 0.5f / fsx),
                Mathf.Abs(b.size.y * ls.y * 0.5f / fsy)
            );
        }

        Collider2D col = GetComponent<Collider2D>();

        if (col == null)
        {
            col = GetComponentInChildren<Collider2D>();
        }

        if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector2(b.size.x * 0.5f, b.size.y * 0.5f);
        }

        return new Vector2(0.4f, 0.4f);
    }

    /// <summary>
    /// Half-size of the sprite's opaque pixels in sprite-local units.
    /// </summary>
    private static Vector2 OpaqueSpriteHalf(Sprite sprite)
    {
        Texture2D tex = sprite.texture;

        if (tex == null)
        {
            return Vector2.zero;
        }

        Color32[] pixels;
        int texW = tex.width;
        int texH = tex.height;
        Rect rect = sprite.rect;

        if (tex.isReadable)
        {
            pixels = tex.GetPixels32();
        }
        else
        {
            try
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture rt = RenderTexture.GetTemporary(texW, texH, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;

                Texture2D readable = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, texW, texH), 0, 0);
                readable.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                pixels = readable.GetPixels32();
                DestroyGenerated(readable);
            }
            catch
            {
                return Vector2.zero;
            }
        }

        if (pixels == null || pixels.Length != texW * texH)
        {
            return Vector2.zero;
        }

        int minX = texW, maxX = -1, minY = texH, maxY = -1;
        int originX = (int)rect.x;
        int originY = (int)rect.y;
        int endX = originX + (int)rect.width;
        int endY = originY + (int)rect.height;

        for (int y = originY; y < endY && y < texH; y++)
        {
            for (int x = originX; x < endX && x < texW; x++)
            {
                if (pixels[y * texW + x].a >= 32)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return Vector2.zero;
        }

        // Convert pixel span to sprite-local units via pixelsPerUnit.
        float ppu = sprite.pixelsPerUnit > 1e-4f ? sprite.pixelsPerUnit : 1f;
        float widthUnits = (maxX - minX + 1) / ppu;
        float heightUnits = (maxY - minY + 1) / ppu;

        return new Vector2(widthUnits * 0.5f, heightUnits * 0.5f);
    }

    private SpriteRenderer FindBodyRenderer()
    {
        SpriteRenderer root = GetComponent<SpriteRenderer>();

        if (root != null && root.gameObject.name != "Halo")
        {
            return root;
        }

        foreach (SpriteRenderer child in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (child != null && child.gameObject.name != "Halo")
            {
                return child;
            }
        }

        return null;
    }

    private FormHaloShape ResolveShape()
    {
        if (shape != FormHaloShape.Auto)
        {
            return shape;
        }

        if (possessable != null)
        {
            switch (possessable.Form)
            {
                case BodyForm.Star:
                    return FormHaloShape.Star;
                case BodyForm.Box:
                case BodyForm.SpikeWall:
                    return FormHaloShape.Box;
                case BodyForm.Spider:
                    return FormHaloShape.Hexagon;
                default:
                    return FormHaloShape.Circle;
            }
        }

        return FormHaloShape.Circle;
    }

    private Texture2D CreateOutlineTexture(
        FormHaloShape haloShape,
        Vector2 shapeHalf,
        Vector2 totalHalf)
    {
        Texture2D texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[Resolution * Resolution];

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float u = (x + 0.5f) / Resolution * 2f - 1f;
                float v = (y + 0.5f) / Resolution * 2f - 1f;
                Vector2 p = new Vector2(u * totalHalf.x, v * totalHalf.y);

                float signed = SignedDistance(haloShape, p, shapeHalf);
                float alpha = 0f;

                // Glow band: 0 at the silhouette (after gap), fading out.
                if (signed >= 0f)
                {
                    float t = Mathf.Clamp01(1f - signed / glowThickness);
                    t = t * t * (3f - 2f * t);
                    alpha = t;
                }

                pixels[y * Resolution + x] =
                    new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(alpha)));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static float SignedDistance(FormHaloShape haloShape, Vector2 p, Vector2 half)
    {
        switch (haloShape)
        {
            case FormHaloShape.Box:
                {
                    Vector2 q = new Vector2(Mathf.Abs(p.x) - half.x, Mathf.Abs(p.y) - half.y);
                    Vector2 outside = Vector2.Max(q, Vector2.zero);
                    float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                    return outside.magnitude + inside;
                }

            case FormHaloShape.Star:
                return PolygonDistance(StarPolygon(half), p);

            case FormHaloShape.Hexagon:
                return PolygonDistance(HexagonPolygon(half), p);

            default:
                {
                    float r = Mathf.Sqrt(
                        (p.x * p.x) / (half.x * half.x) +
                        (p.y * p.y) / (half.y * half.y));

                    return (r - 1f) * Mathf.Min(half.x, half.y);
                }
        }
    }

    private static float PolygonDistance(Vector2[] polygon, Vector2 p)
    {
        float best = float.PositiveInfinity;
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            if ((a.y > p.y) != (b.y > p.y))
            {
                float crossX = a.x + (p.y - a.y) * (b.x - a.x) / (b.y - a.y);

                if (p.x < crossX)
                {
                    inside = !inside;
                }
            }

            best = Mathf.Min(best, SegmentDistance(p, a, b));
        }

        return inside ? -best : best;
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;

        if (lenSq <= 1e-6f)
        {
            return Vector2.Distance(p, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>Flat-top hexagon fitted to the body (Spider).</summary>
    private static Vector2[] HexagonPolygon(Vector2 half)
    {
        Vector2[] pts = new Vector2[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            pts[i] = new Vector2(Mathf.Cos(angle) * half.x, Mathf.Sin(angle) * half.y);
        }

        return pts;
    }

    /// <summary>Same 5-point outline StarVisual uses, fitted to the body.</summary>
    private static Vector2[] StarPolygon(Vector2 half)
    {
        Vector2[] raw = new Vector2[10];
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity, maxAbsX = 0f;

        for (int i = 0; i < 10; i++)
        {
            float angle = (90f - i * 36f) * Mathf.Deg2Rad;
            float radius = (i % 2 == 0) ? 0.48f : 0.22f;
            raw[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            minY = Mathf.Min(minY, raw[i].y);
            maxY = Mathf.Max(maxY, raw[i].y);
            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(raw[i].x));
        }

        float halfH = Mathf.Max(1e-4f, (maxY - minY) * 0.5f);
        float centerY = (minY + maxY) * 0.5f;
        Vector2[] pts = new Vector2[10];

        for (int i = 0; i < 10; i++)
        {
            pts[i] = new Vector2(
                raw[i].x / maxAbsX * half.x,
                (raw[i].y - centerY) / halfH * half.y);
        }

        return pts;
    }

    private void ReleaseHalo()
    {
        if (haloSprite != null)
        {
            DestroyGenerated(haloSprite);
            haloSprite = null;
        }

        if (haloTexture != null)
        {
            DestroyGenerated(haloTexture);
            haloTexture = null;
        }
    }

    private static void DestroyGenerated(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
