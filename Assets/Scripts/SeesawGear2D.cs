using UnityEngine;

/// <summary>A colored, decorative gear that latches on when its button is pressed.</summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SeesawGear2D : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 60f;
    public bool IsRunning { get; private set; }
    private Texture2D texture;
    private Sprite sprite;

    public void StartTurning() => IsRunning = true;

    private void OnEnable()
    {
        // A neutral silhouette lets all four gears match their button colors exactly.
        const int size = 256;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Seesaw Gear Silhouette",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = (x + .5f) / size - .5f;
            float py = (y + .5f) / size - .5f;
            float radius = Mathf.Sqrt(px * px + py * py);
            float tooth = Mathf.Cos(Mathf.Atan2(py, px) * 10f);
            float edge = Mathf.Lerp(.37f, .48f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tooth + .3f) / .6f)));
            float alpha = Mathf.Clamp01(Mathf.Min(edge - radius, radius - .18f) * size + .5f);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        sprite.name = "Seesaw Gear";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    private void Update()
    {
        if (Application.isPlaying && IsRunning)
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }

    private void OnDisable()
    {
        if (sprite != null)
        {
            if (Application.isPlaying) Destroy(sprite);
            else DestroyImmediate(sprite);
        }
        if (texture != null)
        {
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
    }
}
