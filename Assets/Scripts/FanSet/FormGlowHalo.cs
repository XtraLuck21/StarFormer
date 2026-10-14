using UnityEngine;

public enum FormHaloShape
{
    Auto,
    Circle,
    Box,
    Star
}

/// <summary>
/// Breathing outline glow around a form while it is not possessed.
/// Drop this on any PossessableBody root; shape follows the form.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class FormGlowHalo : MonoBehaviour
{
    [SerializeField] private bool useFormColor = true;
    [SerializeField] private Color haloColor = Color.white;

    [Header("Shape")]
    [SerializeField] private FormHaloShape shape = FormHaloShape.Auto;

    [Tooltip("Empty space between the body edge and the start of the glow.")]
    [SerializeField] private float gap = 0f;

    [Tooltip("Thickness of the glowing band outside the gap.")]
    [SerializeField] private float glowThickness = 0.28f;

    [Header("Breathing")]
    [SerializeField, Min(0f)] private float pulseSpeed = 1.2f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.3f;

    private const int Resolution = 256;

    private PossessableBody possessable;
    private SpriteRenderer haloRenderer;
    private Texture2D haloTexture;
    private Sprite haloSprite;
    private Vector2 bodyHalf;
    private FormHaloShape bakedShape;

    private void OnEnable()
    {
        possessable = GetComponent<PossessableBody>();
        CacheBodySize();
        BuildHalo();
        ApplyVisibility();
    }

    private void OnDisable()
    {
        ReleaseHalo();
    }

    private void OnValidate()
    {
        gap = Mathf.Max(0f, gap);
        glowThickness = Mathf.Max(0.02f, glowThickness);

        if (isActiveAndEnabled)
        {
            CacheBodySize();
            BuildHalo();
            ApplyVisibility();
        }
    }

    private void Update()
    {
        if (haloRenderer == null)
        {
            return;
        }

        ApplyVisibility();

        if (!haloRenderer.enabled)
        {
            return;
        }

        Color color = ResolveColor();
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);

        // Smoothstep eases the ends so the breath does not look steppy.
        wave = wave * wave * (3f - 2f * wave);
        color.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
        haloRenderer.color = color;
    }

    private void ApplyVisibility()
    {
        if (haloRenderer == null)
        {
            return;
        }

        bool show = possessable == null || !possessable.IsControlled;
        haloRenderer.enabled = show;
    }

    private Color ResolveColor()
    {
        if (!useFormColor || possessable == null)
        {
            return haloColor;
        }

        switch (possessable.Form)
        {
            case BodyForm.Star:
                return new Color(1f, 0.86f, 0.35f, 1f);
            case BodyForm.Box:
                return new Color(1f, 0.72f, 0.38f, 1f);
            case BodyForm.Bubble:
                return new Color(0.55f, 0.9f, 1f, 1f);
            case BodyForm.SpikeWall:
                return new Color(1f, 0.42f, 0.38f, 1f);
            case BodyForm.Fan:
                return new Color(0.62f, 0.86f, 1f, 1f);
            case BodyForm.Spider:
                return new Color(0.86f, 0.55f, 1f, 1f);
            case BodyForm.gravityBall:
                return new Color(0.45f, 1f, 0.72f, 1f);
            default:
                return haloColor;
        }
    }

    private void CacheBodySize()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col == null)
        {
            col = GetComponentInChildren<Collider2D>();
        }

        if (col != null)
        {
            Bounds bounds = col.bounds;
            Vector3 lossy = transform.lossyScale;
            float sx = lossy.x > 0.0001f ? lossy.x : 1f;
            float sy = lossy.y > 0.0001f ? lossy.y : 1f;
            bodyHalf = new Vector2(bounds.size.x / sx * 0.5f, bounds.size.y / sy * 0.5f);
        }
        else
        {
            bodyHalf = new Vector2(0.5f, 0.5f);
        }

        bodyHalf.x = Mathf.Max(0.05f, bodyHalf.x);
        bodyHalf.y = Mathf.Max(0.05f, bodyHalf.y);
    }

    private FormHaloShape ResolveShape()
    {
        if (shape != FormHaloShape.Auto || possessable == null)
        {
            return shape == FormHaloShape.Auto ? FormHaloShape.Circle : shape;
        }

        switch (possessable.Form)
        {
            case BodyForm.Star:
                return FormHaloShape.Star;
            case BodyForm.Box:
            case BodyForm.SpikeWall:
                return FormHaloShape.Box;
            default:
                return FormHaloShape.Circle;
        }
    }

    private void BuildHalo()
    {
        ReleaseHalo();
        bakedShape = ResolveShape();

        Vector2 half = bodyHalf + Vector2.one * gap;
        float extend = glowThickness;

        haloTexture = CreateHaloTexture(bakedShape, half, extend);
        haloTexture.name = "RuntimeFormHaloTexture";
        haloTexture.hideFlags = HideFlags.HideAndDontSave;

        float worldWidth = (half.x + extend) * 2f;
        float worldHeight = (half.y + extend) * 2f;

        haloSprite = Sprite.Create(
            haloTexture,
            new Rect(0f, 0f, Resolution, Resolution),
            new Vector2(0.5f, 0.5f),
            Resolution
        );

        haloSprite.name = "RuntimeFormHaloSprite";
        haloSprite.hideFlags = HideFlags.HideAndDontSave;

        Transform haloTransform = transform.Find("Halo");

        if (haloTransform == null)
        {
            GameObject haloObject = new GameObject("Halo");
            haloTransform = haloObject.transform;
            haloTransform.SetParent(transform, false);
        }

        haloTransform.localPosition = Vector3.zero;
        haloTransform.localRotation = Quaternion.identity;
        haloTransform.localScale = new Vector3(
            worldWidth,
            worldHeight,
            1f
        );

        haloRenderer = haloTransform.GetComponent<SpriteRenderer>();

        if (haloRenderer == null)
        {
            haloRenderer = haloTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        haloRenderer.sprite = haloSprite;
        haloRenderer.color = ResolveColor();
        haloRenderer.sortingOrder = -1;
    }

    private static Texture2D CreateHaloTexture(
        FormHaloShape haloShape,
        Vector2 half,
        float extend)
    {
        Texture2D texture = new Texture2D(
            Resolution,
            Resolution,
            TextureFormat.RGBA32,
            false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[Resolution * Resolution];
        Vector2 maxHalf = half + Vector2.one * extend;

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float u = (x + 0.5f) / Resolution * 2f - 1f;
                float v = (y + 0.5f) / Resolution * 2f - 1f;

                Vector2 p = new Vector2(u * maxHalf.x, v * maxHalf.y);
                float signed = SignedDistance(haloShape, p, half);

                // signed < 0 inside body; glow starts after the gap.
                float band = signed - 0f;
                float alpha = 0f;

                if (band >= 0f)
                {
                    float outer = extend;
                    float t = Mathf.Clamp01(1f - band / Mathf.Max(outer, 0.01f));

                    // Smootherstep band: soft inner and outer edges.
                    t = t * t * t * (t * (t * 6f - 15f) + 10f);
                    alpha = t;
                }

                byte a = (byte)(255f * Mathf.Clamp01(alpha));
                pixels[y * Resolution + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Signed distance to the body silhouette. Negative inside the body,
    /// zero at the body edge, positive outside. Gap is applied by the caller
    /// through the half-size already expanded by <see cref="gap"/>.
    /// </summary>
    private static float SignedDistance(
        FormHaloShape haloShape,
        Vector2 p,
        Vector2 bodyHalf)
    {
        switch (haloShape)
        {
            case FormHaloShape.Box:
                {
                    Vector2 q = new Vector2(
                        Mathf.Abs(p.x) - bodyHalf.x,
                        Mathf.Abs(p.y) - bodyHalf.y
                    );

                    Vector2 outside = Vector2.Max(q, Vector2.zero);
                    float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                    return outside.magnitude + inside;
                }

            case FormHaloShape.Star:
                return StarSignedDistance(p, bodyHalf);

            default:
                {
                    // Ellipse approximated by scaled circle.
                    float r = Mathf.Sqrt(
                        (p.x * p.x) / (bodyHalf.x * bodyHalf.x) +
                        (p.y * p.y) / (bodyHalf.y * bodyHalf.y)
                    );

                    return (r - 1f) * Mathf.Min(bodyHalf.x, bodyHalf.y);
                }
        }
    }

    private static float StarSignedDistance(Vector2 p, Vector2 half)
    {
        Vector2[] polygon = CreateStarPolygon(half);
        float best = float.PositiveInfinity;
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1;
             i < polygon.Length;
             j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            if ((a.y > p.y) != (b.y > p.y))
            {
                float crossingX =
                    a.x + (p.y - a.y) * (b.x - a.x) / (b.y - a.y);

                if (p.x < crossingX)
                {
                    inside = !inside;
                }
            }

            best = Mathf.Min(best, DistanceToSegment(p, a, b));
        }

        return inside ? -best : best;
    }

    private static Vector2[] CreateStarPolygon(Vector2 half)
    {
        Vector2[] points = new Vector2[10];
        float sx = half.x * 0.92f;
        float sy = half.y * 0.92f;

        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f - i * 36f) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? 1f : 0.46f;

            points[i] = new Vector2(
                Mathf.Cos(angle) * sx * radius,
                Mathf.Sin(angle) * sy * radius
            );
        }

        return points;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;

        if (lengthSq <= 0.0001f)
        {
            return Vector2.Distance(p, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
        return Vector2.Distance(p, a + ab * t);
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
