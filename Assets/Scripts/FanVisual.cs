using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class FanVisual : MonoBehaviour
{
    [SerializeField] private Color fanColor =
        new Color(0.55f, 0.78f, 0.95f, 1f);

    [SerializeField] private float spinSpeed = 540f;

    private const int Resolution = 128;
    private const float VisibleHeight = 0.8f;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;
    private FanBlower blower;

    private void OnEnable()
    {
        blower = GetComponentInParent<FanBlower>();
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
            spriteRenderer.color = fanColor;
        }
    }

    private void OnDisable()
    {
        ReleaseSprite();
    }

    private void Update()
    {
        // Keep the editor view static so level layout stays easy to read.
        if (!Application.isPlaying || blower == null)
        {
            return;
        }

        transform.localScale = new Vector3(
            blower.Facing >= 0 ? 1f : -1f,
            1f,
            1f
        );

        if (blower.IsBlowing)
        {
            transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
    }

    private void BuildSprite()
    {
        ReleaseSprite();

        Color32[] pixels = new Color32[Resolution * Resolution];

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / Resolution,
                    (y + 0.5f) / Resolution
                );

                byte alpha = IsInside(point)
                    ? (byte)255
                    : (byte)0;

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

        generatedTexture.name = "RuntimeFanTexture";
        generatedTexture.filterMode = FilterMode.Bilinear;
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        generatedTexture.hideFlags = HideFlags.HideAndDontSave;
        generatedTexture.SetPixels32(pixels);
        generatedTexture.Apply();

        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(0f, 0f, Resolution, Resolution),
            new Vector2(0.5f, 0.5f),
            Resolution,
            0,
            SpriteMeshType.FullRect
        );

        generatedSprite.name = "RuntimeFanSprite";
        generatedSprite.hideFlags = HideFlags.HideAndDontSave;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.sprite = generatedSprite;
        spriteRenderer.color = fanColor;
        spriteRenderer.sortingOrder = 5;
    }

    // Fan face: outer ring + four tapered blades + center hub.
    private static bool IsInside(Vector2 point)
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        float dist = Vector2.Distance(point, center);

        // VisibleHeight is the outer diameter.
        float rNorm = dist / (VisibleHeight * 0.5f);

        if (rNorm > 1f)
        {
            return false;
        }

        if (rNorm >= 0.86f)
        {
            return true;
        }

        if (rNorm <= 0.16f)
        {
            return true;
        }

        if (rNorm < 0.22f || rNorm > 0.78f)
        {
            return false;
        }

        float angle = Mathf.Atan2(point.y - center.y, point.x - center.x)
            * Mathf.Rad2Deg;

        float t = (rNorm - 0.22f) / (0.78f - 0.22f);
        float halfWidth = Mathf.Lerp(32f, 18f, t);

        for (int blade = 0; blade < 4; blade++)
        {
            float bladeCenter = 45f + blade * 90f;
            float delta = Mathf.Abs(
                Mathf.DeltaAngle(angle, bladeCenter)
            );

            if (delta <= halfWidth)
            {
                return true;
            }
        }

        return false;
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
