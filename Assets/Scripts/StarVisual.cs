using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class StarVisual : MonoBehaviour
{
    [SerializeField] private Color starColor =
        new Color(1f, 0.82f, 0.15f, 1f);

    private const int Resolution = 128;
    private const float VisibleHeight = 0.8f;

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
            spriteRenderer.color = starColor;
        }
    }

    private void OnDisable()
    {
        ReleaseSprite();
    }

    private void BuildSprite()
    {
        ReleaseSprite();

        Vector2[] outline = CreateOutline();
        Color32[] pixels = new Color32[Resolution * Resolution];

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / Resolution,
                    (y + 0.5f) / Resolution
                );

                byte alpha = IsInside(point, outline)
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

        generatedTexture.name = "RuntimeStarTexture";
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

        generatedSprite.name = "RuntimeStarSprite";
        generatedSprite.hideFlags = HideFlags.HideAndDontSave;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.sprite = generatedSprite;
        spriteRenderer.color = starColor;
        spriteRenderer.sortingOrder = 10;
    }

    private static Vector2[] CreateOutline()
    {
        Vector2[] points = new Vector2[10];
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f - i * 36f) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? 0.48f : 0.22f;

            points[i] = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;

            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        // Center the visible shape and match the player's collider height.
        float scale = VisibleHeight / (maxY - minY);
        float centerY = (minY + maxY) * 0.5f;

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector2(
                0.5f + points[i].x * scale,
                0.5f + (points[i].y - centerY) * scale
            );
        }

        return points;
    }

    private static bool IsInside(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1;
             i < polygon.Length;
             j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            if ((a.y > point.y) == (b.y > point.y))
            {
                continue;
            }

            float crossingX =
                a.x + (point.y - a.y) * (b.x - a.x) / (b.y - a.y);

            if (point.x < crossingX)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private void ReleaseSprite()
    {
        if (generatedSprite != null)
        {
            DestroyObject(generatedSprite);
            generatedSprite = null;
        }

        if (generatedTexture != null)
        {
            DestroyObject(generatedTexture);
            generatedTexture = null;
        }
    }

    private static void DestroyObject(Object target)
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
