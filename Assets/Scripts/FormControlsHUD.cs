using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FormControlsHUD : MonoBehaviour
{
    [SerializeField] private HelpBookContent formBook;

    private static readonly Color Background = new Color32(25, 43, 62, 255);
    private static readonly Color Highlight = new Color32(79, 68, 49, 255);
    private static readonly Color Accent = new Color32(255, 198, 68, 255);
    private static readonly Color KeyFace = new Color32(244, 247, 251, 255);
    private const float ActionX = 83f;
    private const float BindingX = 157f;
    private const float RowHeight = 30f;

    private MobileFormControls mobileControls;
    private PlayerPossessionController player;
    private NavigationStarController navigation;
    private RectTransform bar;
    private Image background;
    private PossessableBody lastBody;
    private bool lastSwimming;
    private float changedAt;
    private RectTransform content;
    private Sprite rounded;
    private Texture2D cornerTexture;

    private void Awake()
    {
        player = GetComponent<PlayerPossessionController>();
        mobileControls = GetComponent<MobileFormControls>();
        navigation = GetComponent<NavigationStarController>();
        var hud = new GameObject("Form Controls HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0f;

        bar = Element("Controls", hud.transform);
        bar.anchorMin = bar.anchorMax = bar.pivot = Vector2.up;
        bar.anchoredPosition = new Vector2(20f, -18f);
        bar.localScale = Vector3.one * 1.25f;
        rounded = CreateRoundedSprite();
        Round(bar, Accent);
        content = Element("Content", bar);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.one * 2f;
        content.offsetMax = Vector2.one * -2f;
        background = Round(content, Background);
        if (navigation != null) BuildCard(BodyForm.Star, false);
    }

    private void OnDisable()
    {
        if (bar != null) bar.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (navigation != null)
        {
            bool canNavigate = (mobileControls == null || !mobileControls.MobileMode) &&
                navigation.isActiveAndEnabled && !navigation.ControlsBlocked && !navigation.IsPreviewing;
            if (bar.gameObject.activeSelf != canNavigate) bar.gameObject.SetActive(canNavigate);
            return;
        }
        var body = player.CurrentBody;
        bool visible = (mobileControls == null || !mobileControls.MobileMode) &&
            body != null && body.isActiveAndEnabled && body.Form != BodyForm.gravityBall && !player.IsDead && Time.timeScale > 0f;
        if (bar.gameObject.activeSelf != visible) bar.gameObject.SetActive(visible);
        if (!visible) return;

        bool swimming = body.Form == BodyForm.Bubble && body.IsInWater;
        if (body != lastBody || swimming != lastSwimming)
        {
            if (body != lastBody) changedAt = Time.unscaledTime;
            if (lastBody == null || body.Form != lastBody.Form || swimming != lastSwimming)
                BuildCard(body.Form, swimming);
            lastBody = body;
            lastSwimming = swimming;
        }
        background.color = Color.Lerp(Highlight, Background, (Time.unscaledTime - changedAt) / 0.45f);
    }

    private void BuildCard(BodyForm form, bool swimming)
    {
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        int rows = form switch
        {
            BodyForm.Box or BodyForm.Fan => 3,
            BodyForm.BouncyBall => 1,
            _ => navigation != null ? 3 : 2
        };
        float height = 42f + rows * RowHeight;
        bar.sizeDelta = new Vector2(256f, height);

        var iconRect = Slot("Form icon", 13f, (height - 48f) / 2f, 44f, 44f);
        if (form == BodyForm.Box)
        {
            Round(iconRect, new Color32(251, 155, 37, 255));
        }
        else
        {
            var icon = Paint(iconRect, Color.white);
            foreach (var page in formBook.Pages)
            {
                if (page.unlockId != form.ToString()) continue;
                icon.sprite = page.icon;
                break;
            }
            icon.preserveAspect = true;
        }
        Paint(Slot("Divider", 69f, 9f, 1.5f, height - 22f), new Color32(175, 187, 202, 255));
        string name = form == BodyForm.BouncyBall ? "Bouncy Ball" : form.ToString();
        var title = Text(Slot("Form name", ActionX, 5f, 164f, 27f), name, 22f, true);
        title.fontStyle |= FontStyles.UpperCase;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        string movement = swimming ? "WASD" : "A/D";
        ActionRow(0, swimming ? "Swim" : "Move", movement);
        switch (form)
        {
            case BodyForm.Star:
                ActionRow(1, "Jump", "Space");
                if (navigation != null) ActionRow(2, "Enter", null, false);
                break;
            case BodyForm.Box:
                ActionRow(1, "Grow", "J", false);
                ActionRow(2, "Shrink", "K", true);
                break;
            case BodyForm.Fan:
                ActionRow(1, "Blow", "F", false);
                ActionRow(2, "Leave", "Q", true);
                break;
            case BodyForm.Spider:
                ActionRow(1, "Aim", null, false);
                break;
            case BodyForm.Bubble:
                ActionRow(1, "Pop", "Q", true);
                break;
        }
    }

    private void ActionRow(int row, string action, string key, bool? rightMouse = null)
    {
        float top = 33f + row * RowHeight;
        var label = Text(Slot(action, ActionX, top, BindingX - ActionX - 8f, 26f), action, 20f, true);
        label.alignment = TextAlignmentOptions.MidlineLeft;

        float bindingX = BindingX;
        if (key != null)
        {
            var cap = Slot(key + " key", bindingX, top, 26f, 26f);
            Round(cap, KeyFace);
            var letter = Text(cap, key, 18f, true);
            letter.color = Background;
            float width = Mathf.Max(26f, Mathf.Ceil(letter.preferredWidth) + 12f);
            cap.sizeDelta = new Vector2(width, 26f);
            bindingX += width + 4f;
        }
        if (rightMouse.HasValue)
        {
            if (key != null)
            {
                Text(Slot("Slash", bindingX, top, 14f, 26f), "/", 19f, false);
                bindingX += 18f;
            }
            var mouse = Slot("Mouse binding", bindingX, top, 24f, 26f);
            MouseButtonHintIcon.DrawOnCanvas(mouse, Vector2.down * 13f, 34f, rightMouse.Value, Color.white);
        }
    }

    private static TextMeshProUGUI Text(RectTransform parent, string value, float size, bool bold)
    {
        var rect = Element("Text", parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform Slot(string name, float x, float top, float width, float height)
    {
        var rect = Element(name, content);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = new Vector2(x, -top);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private Image Round(RectTransform rect, Color color)
    {
        var image = Paint(rect, color);
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        return image;
    }

    // Slice a small antialiased mask so cards and keycaps keep crisp corners at any width.
    private Sprite CreateRoundedSprite()
    {
        const int side = 24;
        const int radius = 8;
        cornerTexture = new Texture2D(side, side, TextureFormat.RGBA32, false);
        cornerTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < side; y++)
        for (int x = 0; x < side; x++)
        {
            var point = new Vector2(x + 0.5f, y + 0.5f);
            var nearest = new Vector2(Mathf.Clamp(point.x, radius, side - radius),
                Mathf.Clamp(point.y, radius, side - radius));
            float alpha = Mathf.Clamp01(radius + 0.5f - Vector2.Distance(point, nearest));
            cornerTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        cornerTexture.Apply(false, true);
        return Sprite.Create(cornerTexture, new Rect(0, 0, side, side), Vector2.one * 0.5f, 100f,
            0, SpriteMeshType.FullRect, Vector4.one * radius);
    }

    private void OnDestroy()
    {
        if (rounded != null) Destroy(rounded);
        if (cornerTexture != null) Destroy(cornerTexture);
    }

    private static RectTransform Element(string name, Transform parent)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image Paint(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
