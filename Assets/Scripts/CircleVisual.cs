using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class CircleVisual : MonoBehaviour
{
    [SerializeField] private Color circleColor = Color.red;

    [Header("Outline")]
    [SerializeField] private bool showOutline;
    [SerializeField] private Color outlineColor = new Color(23f / 255f, 74f / 255f, 112f / 255f, 1f);
    [Tooltip("Border thickness as a fraction of the visible circle diameter. Drawn inward without changing the outer size.")]
    [SerializeField, Range(0.01f, 0.15f)] private float outlineWidth = 0.045f;

    private const int Resolution = 64;
    private const int OutlinedResolution = 128;
    private const float VisibleRadius = 0.92f;

    private bool rebuildRequested;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;

    private void OnEnable()
    {
        BuildSprite();
    }

    private void OnValidate()
    {
        // Rebuild on the next update, outside Unity's serialization callbacks.
        rebuildRequested = true;
    }

    private void Update()
    {
        if (rebuildRequested)
        {
            BuildSprite();
        }
    }

    private void OnDisable()
    {
        ReleaseSprite();
    }

    private void BuildSprite()
    {
        rebuildRequested = false;
        ReleaseSprite();

        int resolution = showOutline ? OutlinedResolution : Resolution;
        Color32[] pixels = new Color32[resolution * resolution];
        float radius = resolution * 0.5f;
        float innerRadius = VisibleRadius * (1f - 2f * outlineWidth);
        float edgeWidth = 1f / radius;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                if (showOutline)
                {
                    // Both colors are baked into one sprite so the border is not tinted by the fill.
                    float borderBlend = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(innerRadius - edgeWidth * 0.5f,
                            innerRadius + edgeWidth * 0.5f, dist));
                    Color pixel = Color.Lerp(circleColor, outlineColor, borderBlend);
                    pixel.a *= 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(VisibleRadius - edgeWidth, VisibleRadius, dist));
                    pixels[y * resolution + x] = pixel;
                }
                else
                {
                    byte alpha = dist <= VisibleRadius ? (byte)255 : (byte)0;
                    pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
                }
            }
        }

        generatedTexture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        generatedTexture.name = "RuntimeCircleTexture";
        generatedTexture.filterMode = FilterMode.Bilinear;
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        generatedTexture.hideFlags = HideFlags.HideAndDontSave;
        generatedTexture.SetPixels32(pixels);
        generatedTexture.Apply();

        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution
        );

        generatedSprite.name = "RuntimeCircleSprite";
        generatedSprite.hideFlags = HideFlags.HideAndDontSave;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.sprite = generatedSprite;
        spriteRenderer.color = showOutline ? Color.white : circleColor;
        spriteRenderer.sortingOrder = 2;
    }

    private void ReleaseSprite()
    {
        if (generatedSprite != null)
        {
            DestroyGenerated(generatedSprite);
            generatedSprite = null;
        }

        if (generatedTexture != null)
        {
            DestroyGenerated(generatedTexture);
            generatedTexture = null;
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
