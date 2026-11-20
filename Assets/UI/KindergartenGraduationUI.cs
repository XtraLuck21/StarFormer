using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only. LevelTransitionUI owns the pause and scene transition.
public sealed class KindergartenGraduationUI : MonoBehaviour
{
    private static readonly Color Gold = new Color(1f, .82f, .15f);
    private static readonly Color Forest = new Color(.055f, .18f, .15f);
    private Texture2D roundedTexture, sparkleTexture;
    private Sprite roundedSprite, sparkleSprite;
    private RectTransform panel, hero;
    private CanvasGroup fade;
    private Button next;
    public Button NextLevelButton => next;
    private readonly RectTransform[] sparkles = new RectTransform[6];
    private readonly Vector2[] sparklePositions = {
        new Vector2(-240, 245), new Vector2(230, 260), new Vector2(-265, 110),
        new Vector2(265, 120), new Vector2(-210, 20), new Vector2(220, 25)
    };

    public void Initialize(Sprite graduate, Action continueToNextLevel)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        fade = gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;

        Image dim = Image("Backdrop", transform, Vector2.zero, Vector2.zero, new Color(.02f, .08f, .06f, .65f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true;
        roundedSprite = MakeSprite(false, out roundedTexture);
        sparkleSprite = MakeSprite(true, out sparkleTexture);
        panel = Image("Graduation Card", transform, Vector2.zero, new Vector2(1120, 920), Gold, true).rectTransform;
        Image("Card Fill", panel, Vector2.zero, new Vector2(1110, 910), Forest, true);
        Label("Level Up", "LEVEL UP!", panel, new Vector2(0, 360), new Vector2(1000, 100), 66, Gold);
        hero = Image("Graduate Star", panel, new Vector2(0, 165), new Vector2(430, 360), Color.white).rectTransform;
        var art = hero.GetComponent<Image>(); art.sprite = graduate; art.preserveAspect = true;
        for (int i = 0; i < sparkles.Length; i++)
        {
            var sparkle = Image("Sparkle " + i, panel, sparklePositions[i], Vector2.one * (i % 2 == 0 ? 45 : 30), Gold);
            sparkle.sprite = sparkleSprite;
            sparkles[i] = sparkle.rectTransform;
        }
        Label("Graduation Title", "Kindergarten Complete!", panel, new Vector2(0, -65), new Vector2(1040, 105), 62, Gold);
        Label("Graduation Message", "You've graduated!\nReady for a new challenge?", panel,
            new Vector2(0, -190), new Vector2(1000, 140), 45, new Color(1, .94f, .76f));
        Image buttonImage = Image("Next Level", panel, new Vector2(0, -350), new Vector2(630, 110), Gold, true);
        buttonImage.raycastTarget = true;
        next = buttonImage.gameObject.AddComponent<Button>();
        next.targetGraphic = buttonImage;
        next.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = next.colors;
        colors.highlightedColor = new Color(1, .95f, .72f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.8f, .82f, .7f);
        colors.disabledColor = Color.white;
        next.colors = colors;
        Label("Button Label", "Next Level  →", buttonImage.transform, Vector2.zero, new Vector2(600, 100), 52, Forest);
        next.interactable = false;
        next.onClick.AddListener(() => { if (next.interactable) continueToNextLevel(); });
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        float elapsed = 0;
        while (elapsed < .95f)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Clamp01(elapsed / .3f);
            float p = Mathf.Clamp01(elapsed / .55f) - 1;
            float ease = 1 + 2.70158f * p * p * p + 1.70158f * p * p;
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(.9f, 1, ease);
            float hop = Mathf.Sin(Mathf.Clamp01((elapsed - .12f) / .7f) * Mathf.PI);
            hero.anchoredPosition = new Vector2(0, 165 + hop * 42);
            hero.localRotation = Quaternion.Euler(0, 0, -5 * hop);
            for (int i = 0; i < sparkles.Length; i++)
            {
                float progress = Mathf.Clamp01((elapsed - .15f - i * .035f) / .35f);
                sparkles[i].localScale = Vector3.one * Mathf.SmoothStep(0, 1, progress);
            }
            yield return null;
        }
        fade.alpha = 1; panel.localScale = Vector3.one;
        hero.anchoredPosition = new Vector2(0, 165); hero.localRotation = Quaternion.identity;
        foreach (var sparkle in sparkles) sparkle.localScale = Vector3.one;
        next.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
        // Remain here indefinitely; only the button can advance the level.
    }

    private Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool rounded = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size;
        image.color = color; image.raycastTarget = false;
        if (rounded) { image.sprite = roundedSprite; image.type = UnityEngine.UI.Image.Type.Sliced; }
        return image;
    }

    private static void Label(string name, string value, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
        text.text = value; text.fontSize = fontSize; text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static Sprite MakeSprite(bool sparkle, out Texture2D texture)
    {
        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = sparkle ? "Graduation Sparkle" : "Graduation Rounded Panel";
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Abs(x + .5f - 32), dy = Mathf.Abs(y + .5f - 32);
            float distance = sparkle ? (Mathf.Sqrt(dx / 31) + Mathf.Sqrt(dy / 31) - 1) * 20 :
                new Vector2(Mathf.Max(dx - 16, 0), Mathf.Max(dy - 16, 0)).magnitude - 16;
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(.5f - distance));
        }
        texture.SetPixels(pixels); texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
            SpriteMeshType.FullRect, sparkle ? Vector4.zero : Vector4.one * 18);
    }

    private void OnDestroy()
    {
        if (roundedSprite != null) Destroy(roundedSprite);
        if (sparkleSprite != null) Destroy(sparkleSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
        if (sparkleTexture != null) Destroy(sparkleTexture);
    }
}
