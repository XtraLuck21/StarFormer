using TMPro;
using UnityEngine;

/// <summary>Styles only the selected Spider hint on the object carrying this component.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class SpiderIntroductionHintCard : MonoBehaviour
{
    [SerializeField] private Color cardColor = new Color32(25, 43, 62, 245);
    [SerializeField] private Vector2 padding = new Vector2(.28f, .18f);
    [SerializeField] private string titleObjectName = "FormIntroductionText";
    [SerializeField] private string subtitleObjectName = "SpiderSubtitle";
    [SerializeField] private bool includeKeyIcon;

    private TextMeshPro title;
    private TextMeshPro subtitle;
    private Color originalTitleColor;
    private Color originalSubtitleColor;
    private SpriteRenderer card;
    private Sprite sprite;
    private Texture2D texture;
    private Material material;
    private string measuredTitle;
    private string measuredSubtitle;
    private Vector2 measuredTitleSize;
    private Vector2 measuredSubtitleSize;

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (title == null)
        {
            foreach (TextMeshPro text in GetComponentsInChildren<TextMeshPro>(true))
            {
                if (text.name == titleObjectName) title = text;
                else if (text.name == subtitleObjectName) subtitle = text;
            }
            if (title == null) return;
            originalTitleColor = title.color;
            if (subtitle != null) originalSubtitleColor = subtitle.color;
            measuredTitle = measuredSubtitle = null;
        }

        SetWhite(title);
        if (subtitle != null) SetWhite(subtitle);
        if (card == null)
        {
            if (sprite == null) CreateSprite();
            var obj = new GameObject(includeKeyIcon ? "Spider Key Hint Card" : "Spider Introduction Card", typeof(SpriteRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            obj.transform.SetParent(title.transform.parent, false);
            card = obj.GetComponent<SpriteRenderer>();
            card.sprite = sprite;
            card.sharedMaterial = material;
            card.drawMode = SpriteDrawMode.Sliced;
            card.sortingLayerID = title.renderer.sortingLayerID;
            card.sortingOrder = title.renderer.sortingOrder - 1;
            if (includeKeyIcon)
                foreach (SpriteRenderer part in title.transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
                    if (part != card) card.sortingOrder = Mathf.Min(card.sortingOrder, part.sortingOrder - 1);
            measuredTitle = null;
        }

        card.enabled = title.isActiveAndEnabled;
        if (!card.enabled)
        {
            // Inactive TMP objects may not have run Awake or generated any glyphs yet.
            // Never retain that provisional measurement for the next visible frame.
            measuredTitle = null;
            return;
        }
        Color color = cardColor;
        color.a *= Mathf.Max(title.alpha, subtitle != null ? subtitle.alpha : 0f);
        card.color = color;

        Vector2 subtitleSize = subtitle != null ? subtitle.rectTransform.rect.size : Vector2.zero;
        string subtitleText = subtitle != null ? subtitle.text : string.Empty;
        if (measuredTitle == title.text && measuredSubtitle == subtitleText &&
            measuredTitleSize == title.rectTransform.rect.size && measuredSubtitleSize == subtitleSize) return;

        bool found = false;
        Bounds bounds = default;
        bool geometryReady = IncludeText(title, card.transform.parent, ref bounds, ref found, includeKeyIcon);
        if (subtitle != null) geometryReady &= IncludeText(subtitle, card.transform.parent, ref bounds, ref found);
        if (includeKeyIcon)
        {
            foreach (SpriteRenderer part in card.transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (part == card) continue;
                Bounds partBounds = part.bounds;
                foreach (float x in new[] { partBounds.min.x, partBounds.max.x })
                foreach (float y in new[] { partBounds.min.y, partBounds.max.y })
                {
                    Vector3 point = card.transform.parent.InverseTransformPoint(new Vector3(x, y, partBounds.center.z));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
        }
        if (!found) { card.enabled = false; return; }
        card.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0f);
        card.size = new Vector2(Mathf.Max(.5f, bounds.size.x + padding.x * 2f),
            Mathf.Max(.4f, bounds.size.y + padding.y * 2f));
        measuredTitle = geometryReady ? title.text : null;
        measuredSubtitle = subtitleText;
        measuredTitleSize = title.rectTransform.rect.size;
        measuredSubtitleSize = subtitleSize;
    }

    private static void SetWhite(TextMeshPro text)
    {
        Color white = new Color(1f, 1f, 1f, text.alpha);
        if (text.color != white) text.color = white;
    }

    private static bool IncludeText(TextMeshPro text, Transform parent, ref Bounds bounds, ref bool found,
        bool includeTextArea = false)
    {
        if (string.IsNullOrWhiteSpace(text.text)) return true;
        text.ForceMeshUpdate(true);
        Bounds glyphs = text.textBounds;
        bool ready = text.textInfo != null && text.textInfo.characterCount > 0 &&
            glyphs.size.x > 0f && glyphs.size.y > 0f;
        if (ready) IncludeBounds(glyphs, text.transform, parent, ref bounds, ref found);
        if (includeTextArea || !ready)
        {
            Rect area = text.rectTransform.rect;
            IncludeBounds(new Bounds(area.center, area.size), text.transform, parent, ref bounds, ref found);
        }
        return ready;
    }

    private static void IncludeBounds(Bounds local, Transform source, Transform parent, ref Bounds bounds, ref bool found)
    {
        foreach (float x in new[] { local.min.x, local.max.x })
        foreach (float y in new[] { local.min.y, local.max.y })
        {
            Vector3 point = parent.InverseTransformPoint(source.TransformPoint(new Vector3(x, y, 0f)));
            if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
            else bounds.Encapsulate(point);
        }
    }

    private void CreateSprite()
    {
        const int side = 64;
        const int radius = 16;
        texture = new Texture2D(side, side, TextureFormat.RGBA32, false)
        {
            name = "Spider introduction rounded card mask",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[side * side];
        for (int y = 0; y < side; y++)
        for (int x = 0; x < side; x++)
        {
            Vector2 p = new Vector2(x + .5f, y + .5f);
            Vector2 nearest = new Vector2(Mathf.Clamp(p.x, radius, side - radius),
                Mathf.Clamp(p.y, radius, side - radius));
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + .5f - Vector2.Distance(p, nearest)) * 255f);
            pixels[y * side + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        sprite = Sprite.Create(texture, new Rect(0, 0, side, side), Vector2.one * .5f, 100f,
            0, SpriteMeshType.FullRect, Vector4.one * radius);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
    }

    private void OnDisable()
    {
        RestoreColor(title, originalTitleColor);
        RestoreColor(subtitle, originalSubtitleColor);
        if (card != null) { card.gameObject.SetActive(false); Release(card.gameObject); }
        card = null;
        title = subtitle = null;
    }

    private static void RestoreColor(TextMeshPro text, Color original)
    {
        if (text == null) return;
        original.a = text.alpha;
        text.color = original;
    }

    private void OnDestroy()
    {
        Release(sprite);
        Release(texture);
        Release(material);
    }

    private static void Release(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }
}
