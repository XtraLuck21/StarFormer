using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fan control keycaps shown only while this fan is possessed, anchored
/// a little above the star's scene-start position. Attach next to FanBlower.
/// Key letters, captions, layout and border style are inspector-tweakable.
/// </summary>
public class FanPossessionHint : MonoBehaviour
{
    public enum MouseBinding { None, Left, Right }

    [System.Serializable]
    public struct KeyHint
    {
        public string key;
        public string caption;
        public MouseBinding mouseButton;
    }

    [SerializeField] private MobileFormControls mobileControls;

    [Header("Content")]
    [SerializeField] private KeyHint[] hints =
    {
        new KeyHint { key = "F", caption = "Activate air flow", mouseButton = MouseBinding.Left },
        new KeyHint { key = "Q", caption = "Quit form", mouseButton = MouseBinding.Right }
    };

    [Header("Anchor")]
    [Tooltip("Optional fixed world anchor. Leave empty to use the star spawn.")]
    [SerializeField] private Transform customAnchor;
    [Tooltip("World offset above the anchor point.")]
    [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Text Size")]
    [SerializeField, Min(0.1f)] private float keySize = 64f;
    [SerializeField, Min(0.1f)] private float keyFontSize = 34f;
    [Tooltip("Same size for every caption so they always match.")]
    [SerializeField, Min(0.1f)] private float captionFontSize = 18f;
    [SerializeField, Min(0.1f)] private float captionGap = 12f;
    [SerializeField, Min(1f)] private float columnWidth = 200f;
    [SerializeField, Min(0.1f)] private float keySpacing = 36f;

    [Header("Text Color")]
    [SerializeField] private Color textColor = Color.white;

    [Header("Hint Card")]
    [SerializeField] private bool showHintCard;
    [SerializeField] private Color hintCardColor = new Color32(25, 43, 62, 245);

    [Header("Border")]
    [Tooltip("Outline color of the keycap ring.")]
    [SerializeField] private Color borderColor = Color.white;
    [Tooltip("Fill inside the keycap, under the letter.")]
    [SerializeField] private Color fillColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField, Range(2f, 20f)] private float borderThickness = 7f;
    [SerializeField, Range(0f, 24f)] private float cornerRadius = 14f;
    [Tooltip("Texture resolution of the generated keycap.")]
    [SerializeField, Range(48, 128)] private int borderTextureSize = 96;

    private PossessableBody body;
    private RectTransform desktopRow;
    private RectTransform touchRow;
    private Vector3 starSpawn;
    private bool hasAnchor;
    private GameObject labelRoot;
    private RectTransform labelRect;
    private Sprite borderSprite;
    private FanHintCard hintCard;

    private float CaptionHeight => showHintCard ? Mathf.Max(40f, captionFontSize * 2.6f) : 40f;

    private void Awake()
    {
        body = GetComponent<PossessableBody>();

        if (body == null)
        {
            Debug.LogError("FanPossessionHint requires a PossessableBody on the same object.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        starSpawn = FindStarSpawn();
        hasAnchor = true;
        borderSprite = CreateKeycapSprite();
        CreateLabel();
        labelRoot.SetActive(false);
    }

    private void Update()
    {
        if (labelRoot == null) return;
        bool controlled = body != null && body.IsControlled;
        if (labelRoot.activeSelf != controlled) labelRoot.SetActive(controlled);
        if (!controlled) return;

        bool touch = mobileControls != null && mobileControls.MobileMode;
        if (touchRow.gameObject.activeSelf != touch) touchRow.gameObject.SetActive(touch);
        if (desktopRow.gameObject.activeSelf == touch) desktopRow.gameObject.SetActive(!touch);
    }

    private void LateUpdate()
    {
        if (labelRect == null || !hasAnchor || labelRoot == null || !labelRoot.activeSelf)
        {
            return;
        }

        PositionLabel();
    }

    private static Vector3 FindStarSpawn()
    {
        PossessableBody[] bodies =
            FindObjectsByType<PossessableBody>(FindObjectsSortMode.None);

        foreach (PossessableBody candidate in bodies)
        {
            if (candidate.Form == BodyForm.Star)
            {
                return candidate.transform.position;
            }
        }

        return Vector3.zero;
    }

    private void CreateLabel()
    {
        labelRoot = new GameObject(
            "Fan Possession Hint",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        Canvas labelCanvas = labelRoot.GetComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        labelCanvas.sortingOrder = 31000;

        CanvasScaler scaler = labelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(labelRoot.transform, false);
        labelRect = row.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);

        float columnHeight = keySize + captionGap + CaptionHeight;
        float totalWidth = hints.Length * columnWidth + (hints.Length - 1) * keySpacing;
        labelRect.sizeDelta = new Vector2(totalWidth, columnHeight);

        desktopRow = CreateRow("Desktop");
        touchRow = CreateRow("Touch");
        AddTouchColumn(0, MobileControlGraphic.Symbol.Wind, "Wind on/off");
        AddTouchColumn(1, MobileControlGraphic.Symbol.Leave, "Leave Fan");

        for (int i = 0; i < hints.Length; i++)
        {
            RectTransform colRect = CreateColumn(desktopRow, "Column " + hints[i].key, i, hints.Length);
            bool hasMouse = hints[i].mouseButton != MouseBinding.None;
            var capObject = new GameObject("Key " + hints[i].key, typeof(RectTransform), typeof(Image));
            capObject.transform.SetParent(colRect, false);

            RectTransform capRect = capObject.GetComponent<RectTransform>();
            capRect.anchorMin = capRect.anchorMax = new Vector2(0.5f, 1f);
            capRect.pivot = new Vector2(0.5f, 1f);
            capRect.anchoredPosition = hasMouse ? Vector2.left * keySize * 0.85f : Vector2.zero;
            capRect.sizeDelta = new Vector2(keySize, keySize);

            Image image = capObject.GetComponent<Image>();
            image.sprite = borderSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;

            var letterObject = new GameObject("Letter", typeof(RectTransform), typeof(TextMeshProUGUI));
            letterObject.transform.SetParent(capObject.transform, false);

            RectTransform letterRect = letterObject.GetComponent<RectTransform>();
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.offsetMin = letterRect.offsetMax = Vector2.zero;

            TextMeshProUGUI letter = letterObject.GetComponent<TextMeshProUGUI>();
            letter.text = hints[i].key;
            letter.fontSize = keyFontSize;
            letter.alignment = TextAlignmentOptions.Center;
            letter.color = textColor;
            letter.raycastTarget = false;

            if (hasMouse)
            {
                var separator = new GameObject("or", typeof(RectTransform), typeof(TextMeshProUGUI));
                var rect = (RectTransform)separator.transform;
                rect.SetParent(colRect, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.down * (keySize * 0.5f);
                rect.sizeDelta = new Vector2(keySize * 0.7f, keySize);
                var text = separator.GetComponent<TextMeshProUGUI>();
                text.text = "or";
                text.fontSize = keyFontSize * 0.75f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = textColor;
                text.raycastTarget = false;

                MouseButtonHintIcon.DrawOnCanvas(colRect, new Vector2(0.85f, -0.5f) * keySize,
                    keySize * 1.3f, hints[i].mouseButton == MouseBinding.Right, borderColor, textColor);
            }

            AddCaption(colRect, hints[i].caption);
        }
        if (showHintCard) hintCard = FanHintCard.Add(labelRect, hintCardColor, false);
    }

    private RectTransform CreateRow(string name)
    {
        var row = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        row.SetParent(labelRect, false);
        return row;
    }

    private RectTransform CreateColumn(RectTransform row, string name, int index, int count)
    {
        var column = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        column.SetParent(row, false);
        column.anchoredPosition = Vector2.right * ((index - (count - 1) * 0.5f) * (columnWidth + keySpacing));
        column.sizeDelta = new Vector2(columnWidth, labelRect.sizeDelta.y);
        return column;
    }

    private void AddTouchColumn(int index, MobileControlGraphic.Symbol symbol, string caption)
    {
        var column = CreateColumn(touchRow, symbol.ToString(), index, 2);
        var icon = (RectTransform)new GameObject("Icon", typeof(RectTransform)).transform;
        icon.SetParent(column, false);
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 1f);
        icon.sizeDelta = Vector2.one * keySize;
        var graphic = icon.gameObject.AddComponent<MobileControlGraphic>();
        graphic.Icon = symbol;
        graphic.raycastTarget = false;
        AddCaption(column, caption);
    }

    private void AddCaption(RectTransform colRect, string text)
    {
        // Caption: fixed size, no auto-scale, identical for every column.
        var captionObject = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
        captionObject.transform.SetParent(colRect, false);

        RectTransform captionRect = captionObject.GetComponent<RectTransform>();
        captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 1f);
        captionRect.pivot = new Vector2(0.5f, 1f);
        captionRect.anchoredPosition = new Vector2(0f, -(keySize + captionGap));
        captionRect.sizeDelta = new Vector2(columnWidth, CaptionHeight);

        TextMeshProUGUI caption = captionObject.GetComponent<TextMeshProUGUI>();
        caption.text = text;
        caption.fontSize = captionFontSize;
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = textColor;
        caption.enableAutoSizing = false;
        caption.raycastTarget = false;
    }

    private void PositionLabel()
    {
        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            return;
        }

        Vector3 world = (customAnchor != null
                ? customAnchor.position
                : starSpawn)
            + labelWorldOffset;

        Vector3 screen = worldCamera.WorldToScreenPoint(world);
        if (screen.z <= 0f)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            labelRect.parent as RectTransform,
            screen,
            null,
            out Vector2 local
        );
        labelRect.anchoredPosition = local;
        if (hintCard != null) hintCard.Refresh();
    }

    /// <summary>
    /// One closed keycap: optional translucent fill, then a solid rounded
    /// outline ring. The ring is drawn by filling the outer rounded rect
    /// and punching out the inner one, so the border is always continuous.
    /// </summary>
    private Sprite CreateKeycapSprite()
    {
        int size = borderTextureSize;
        float thickness = Mathf.Clamp(borderThickness * size / 64f, 2f, size * 0.25f);
        float pad = thickness + 2f;
        float outerSize = size - pad * 2f;
        float innerSize = outerSize - thickness * 2f;
        if (innerSize < 2f)
        {
            innerSize = 2f;
        }

        float radius = Mathf.Clamp(cornerRadius * size / 64f, 1f, outerSize * 0.5f);
        float innerRadius = Mathf.Max(1f, radius - thickness);

        var fillColor32 = new Color32(
            (byte)Mathf.RoundToInt(Mathf.Clamp01(fillColor.r) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(fillColor.g) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(fillColor.b) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(fillColor.a) * 255f)
        );
        var borderColor32 = new Color32(
            (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.r) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.g) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.b) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.a) * 255f)
        );

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - pad;
                float py = y + 0.5f - pad;

                Color32 c = new Color32(0, 0, 0, 0);

                if (px >= 0f && px <= outerSize && py >= 0f && py <= outerSize)
                {
                    float outer = RoundedRectSdf(px, py, outerSize, radius);

                    if (outer <= 0f)
                    {
                        // Inside the keycap: ring first, then fill.
                        float inner = RoundedRectSdf(px, py, innerSize, innerRadius);
                        c = inner > 0f ? borderColor32 : fillColor32;
                    }
                }

                pixels[y * size + x] = c;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private static float RoundedRectSdf(float px, float py, float rectSize, float r)
    {
        r = Mathf.Max(r, 0.5f);
        float half = rectSize * 0.5f;
        float qx = Mathf.Abs(px - half) - (half - r);
        float qy = Mathf.Abs(py - half) - (half - r);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outside + inside - r;
    }

    private void OnDisable()
    {
        if (labelRoot != null)
        {
            labelRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (labelRoot != null) Destroy(labelRoot);
        if (borderSprite != null)
        {
            Destroy(borderSprite.texture);
            Destroy(borderSprite);
        }
    }
}
