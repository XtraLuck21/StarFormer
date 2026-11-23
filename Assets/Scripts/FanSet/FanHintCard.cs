using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A rounded, non-interactive backdrop for the Fan tutorial's existing labels.</summary>
[DisallowMultipleComponent]
public sealed class FanHintCard : MonoBehaviour
{
    private readonly Vector2 padding = new Vector2(30f, 22f);
    private RectTransform content;
    private RectTransform canvasRect;
    private RectTransform card;
    private TMP_Text label;
    private Sprite sprite;
    private Texture2D texture;
    private string measuredText;
    private float measuredWidth = -1f;
    private float measuredFontSize = -1f;

    public static FanHintCard Add(RectTransform content, Color color, bool fitText)
    {
        var backdrop = content.gameObject.AddComponent<FanHintCard>();
        backdrop.content = content;
        backdrop.canvasRect = content.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        backdrop.label = fitText ? content.GetComponent<TMP_Text>() : null;
        var obj = new GameObject("Hint Card", typeof(RectTransform), typeof(Image));
        backdrop.card = obj.GetComponent<RectTransform>();
        backdrop.card.SetParent(content.parent, false);
        backdrop.card.SetSiblingIndex(content.GetSiblingIndex());
        backdrop.card.anchorMin = backdrop.card.anchorMax = Vector2.one * .5f;
        var image = obj.GetComponent<Image>();
        backdrop.sprite = backdrop.CreateRoundedSprite();
        image.sprite = backdrop.sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        backdrop.Refresh();
        return backdrop;
    }

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (card == null || content == null || canvasRect == null) return;
        bool visible = isActiveAndEnabled && (label == null || label.enabled);
        if (card.gameObject.activeSelf != visible) card.gameObject.SetActive(visible);
        if (!visible) return;

        if (label != null)
        {
            float maxWidth = Mathf.Max(160f, Mathf.Min(1400f, canvasRect.rect.width - padding.x * 2 - 48f));
            if (measuredText != label.text || Mathf.Abs(measuredWidth - maxWidth) > .5f || measuredFontSize != label.fontSize)
            {
                Vector2 preferred = label.GetPreferredValues(label.text, maxWidth, Mathf.Infinity);
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Min(maxWidth, Mathf.Ceil(preferred.x) + 2f));
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(preferred.y) + 4f);
                measuredText = label.text;
                measuredWidth = maxWidth;
                measuredFontSize = label.fontSize;
            }
        }

        // A sibling stays behind both standalone TMP labels and control rows.
        card.sizeDelta = content.rect.size + padding * 2f;
        card.localScale = content.localScale;
        card.rotation = content.rotation;
        card.position = content.TransformPoint(content.rect.center);
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, card);
        Rect screen = canvasRect.rect;
        Vector3 shift = Vector3.zero;
        if (bounds.min.x < screen.xMin + 24f) shift.x = screen.xMin + 24f - bounds.min.x;
        else if (bounds.max.x > screen.xMax - 24f) shift.x = screen.xMax - 24f - bounds.max.x;
        if (bounds.min.y < screen.yMin + 24f) shift.y = screen.yMin + 24f - bounds.min.y;
        else if (bounds.max.y > screen.yMax - 24f) shift.y = screen.yMax - 24f - bounds.max.y;
        Vector3 worldShift = canvasRect.TransformVector(shift);
        content.position += worldShift;
        card.position += worldShift;
    }

    private Sprite CreateRoundedSprite()
    {
        const int side = 64;
        const int radius = 24;
        texture = new Texture2D(side, side, TextureFormat.RGBA32, false)
        {
            name = "Fan hint card rounded mask",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[side * side];
        for (int y = 0; y < side; y++)
        for (int x = 0; x < side; x++)
        {
            Vector2 point = new Vector2(x + .5f, y + .5f);
            Vector2 nearest = new Vector2(Mathf.Clamp(point.x, radius, side - radius),
                Mathf.Clamp(point.y, radius, side - radius));
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + .5f - Vector2.Distance(point, nearest)) * 255f);
            pixels[y * side + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, side, side), Vector2.one * .5f, 100f,
            0, SpriteMeshType.FullRect, Vector4.one * radius);
    }

    private void OnEnable()
    {
        if (card != null) card.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (card != null) card.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Release(card != null ? card.gameObject : null);
        Release(sprite);
        Release(texture);
    }

    private static void Release(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }
}
