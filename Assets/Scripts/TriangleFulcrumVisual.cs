using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class TriangleFulcrumVisual : MonoBehaviour
{
    private Sprite triangle;
    private Texture2D texture;
    private bool rebuild;
    private static readonly Vector2[] Points = { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0f, 0.5f) };

    private void OnEnable() => Build();
    private void OnValidate() => rebuild = true;
    private void Update() { if (rebuild) Build(); }

    private void Build()
    {
        rebuild = false;
        Release();
        const int size = 256;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Triangle Fulcrum Texture",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = (x + 0.5f) / size;
            float py = (y + 0.5f) / size;
            float edge = Mathf.Min(px - py * 0.5f, 1f - py * 0.5f - px);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(edge * size + 0.5f)));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        triangle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        triangle.name = "Triangle Fulcrum";
        triangle.hideFlags = HideFlags.HideAndDontSave;
        GetComponent<SpriteRenderer>().sprite = triangle;
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
        collider.pathCount = 1;
        collider.SetPath(0, Points);
    }

    private void OnDisable() => Release();
    private void Release()
    {
        if (triangle != null)
        {
            if (Application.isPlaying) Destroy(triangle);
            else DestroyImmediate(triangle);
        }
        if (texture != null)
        {
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
        triangle = null;
        texture = null;
    }
}
