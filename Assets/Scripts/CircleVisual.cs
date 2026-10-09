using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class CircleVisual : MonoBehaviour
{
    [SerializeField] private Color circleColor = Color.red;

    private const int Resolution = 64;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;

    private void OnEnable()
    {
        BuildSprite();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && generatedSprite != null)
        {
            spriteRenderer.color = circleColor;
        }
    }

    private void OnDisable()
    {
        ReleaseSprite();
    }

    private void BuildSprite()
    {
        ReleaseSprite();

        Color32[] pixels = new Color32[Resolution * Resolution];
        float radius = Resolution * 0.5f;

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                byte alpha = dist <= 0.92f ? (byte)255 : (byte)0;
                pixels[y * Resolution + x] =
                    new Color32(255, 255, 255, alpha);
            }
        }

        generatedTexture = new Texture2D(
            Resolution,
            Resolution,
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
            new Rect(0f, 0f, Resolution, Resolution),
            new Vector2(0.5f, 0.5f),
            Resolution
        );

        generatedSprite.name = "RuntimeCircleSprite";
        generatedSprite.hideFlags = HideFlags.HideAndDontSave;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.sprite = generatedSprite;
        spriteRenderer.color = circleColor;
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
