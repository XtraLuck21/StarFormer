using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only; LevelTransitionUI owns the pause and both exits.
public sealed class ElementaryAdmissionUI : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, .82f, .15f);
    static readonly Color Forest = new Color(.055f, .18f, .15f);
    static readonly Color Cream = new Color(1f, .95f, .82f);
    Texture2D roundedTexture;
    Sprite roundedSprite;
    RectTransform panel, hero, letter, flap;
    CanvasGroup fade;
    Button next;
    public Button NextLevelButton => next;

    public void Initialize(Sprite studentStar, Action continueToNextLevel)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        fade = gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        var dim = Image("Backdrop", transform, Vector2.zero, Vector2.zero, new Color(.02f, .08f, .06f, .65f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true;
        MakeRoundedSprite();
        // Leave the top-right Home control outside the card even on narrow screens.
        panel = Image("Admission Card", transform, new Vector2(0, -35), new Vector2(1420, 850), Gold, true).rectTransform;
        Image("Card Fill", panel, Vector2.zero, new Vector2(1410, 840), Forest, true);
        Label("Level Up", "LEVEL UP!", panel, new Vector2(0, 320), new Vector2(1250, 110), 88, Gold);
        Label("Completion Title", "Level 1 Complete!", panel, new Vector2(0, 235), new Vector2(1250, 70), 48, Cream);

        letter = Image("Acceptance Letter", panel, new Vector2(0, -20), new Vector2(1270, 450), Cream, true).rectTransform;
        Image("Letter Rule", letter, Vector2.zero, new Vector2(1248, 428), new Color(.77f, .55f, .08f), true);
        Image("Letter Paper", letter, Vector2.zero, new Vector2(1242, 422), Cream, true);
        Label("Letter Heading", "LETTER OF ACCEPTANCE", letter, new Vector2(215, 160), new Vector2(760, 60), 37, Forest);
        Label("Recipient", "Dear Star,", letter, new Vector2(215, 75), new Vector2(760, 65), 44, Forest);
        Label("Accepted", "You're accepted!", letter, new Vector2(215, -15), new Vector2(780, 85), 64, Forest);
        Label("School", "Welcome to\nElementary School", letter, new Vector2(215, -130), new Vector2(780, 135), 49, Forest);

        var back = Envelope("Envelope Back", panel, new Vector2(-440, -145), false);
        flap = Envelope("Opening Flap", back, new Vector2(0, 100), false);
        flap.sizeDelta = new Vector2(440, 110);
        flap.pivot = new Vector2(.5f, 0);
        flap.GetComponent<AdmissionEnvelopeGraphic>().flapOnly = true;
        hero = Image("Student Star", panel, new Vector2(-440, -5), new Vector2(450, 460), Color.white).rectTransform;
        var art = hero.GetComponent<Image>(); art.sprite = studentStar; art.preserveAspect = true;
        Envelope("Envelope Front", panel, new Vector2(-440, -145), true);
        var seal = Envelope("Envelope Seal", panel, new Vector2(-440, -175), false);
        seal.sizeDelta = new Vector2(72, 72);
        seal.GetComponent<AdmissionEnvelopeGraphic>().sealOnly = true;

        var buttonImage = Image("Next Level", panel, new Vector2(0, -330), new Vector2(700, 108), Gold, true);
        buttonImage.raycastTarget = true;
        next = buttonImage.gameObject.AddComponent<Button>(); next.targetGraphic = buttonImage;
        next.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = next.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1, .95f, .72f);
        colors.pressedColor = new Color(.8f, .82f, .7f); colors.disabledColor = Color.white;
        next.colors = colors;
        Label("Button Label", "Next Level  →", buttonImage.transform, Vector2.zero, new Vector2(660, 100), 54, Forest);
        next.interactable = false;
        next.onClick.AddListener(() => { if (next.interactable) continueToNextLevel(); });
        StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        float elapsed = 0;
        while (elapsed < 1.05f)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Clamp01(elapsed / .3f);
            panel.localScale = Vector3.one * Mathf.Lerp(.94f, 1, Mathf.SmoothStep(0, 1, elapsed / .5f));
            float open = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .65f));
            flap.localScale = new Vector3(1, Mathf.Lerp(-1, 1, open), 1);
            letter.localScale = new Vector3(1, Mathf.Lerp(.86f, 1, open), 1);
            float hop = Mathf.Sin(Mathf.Clamp01((elapsed - .15f) / .8f) * Mathf.PI);
            hero.anchoredPosition = new Vector2(-440, -5 + 30 * hop);
            hero.localRotation = Quaternion.Euler(0, 0, -4 * hop);
            yield return null;
        }
        fade.alpha = 1; panel.localScale = letter.localScale = flap.localScale = Vector3.one;
        hero.anchoredPosition = new Vector2(-440, -5); hero.localRotation = Quaternion.identity;
        next.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
        // No timer and no looping animation. Wait until the player chooses an exit.
    }

    RectTransform Envelope(string name, Transform parent, Vector2 position, bool front)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(AdmissionEnvelopeGraphic));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(440, 200);
        var graphic = obj.GetComponent<AdmissionEnvelopeGraphic>(); graphic.front = front; graphic.raycastTarget = false;
        return rect;
    }

    Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool rounded = false)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>(); image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size; image.color = color; image.raycastTarget = false;
        if (rounded) { image.sprite = roundedSprite; image.type = UnityEngine.UI.Image.Type.Sliced; }
        return image;
    }
    static void Label(string name, string value, Transform parent, Vector2 position, Vector2 size, float font, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>(); text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size; text.text = value; text.fontSize = font;
        text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center;
        text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
    }
    void MakeRoundedSprite()
    {
        const int size = 64;
        roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        roundedTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Abs(x + .5f - 32), dy = Mathf.Abs(y + .5f - 32);
            float d = new Vector2(Mathf.Max(dx - 16, 0), Mathf.Max(dy - 16, 0)).magnitude - 16;
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(.5f - d));
        }
        roundedTexture.SetPixels(pixels); roundedTexture.Apply(false, true);
        roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * 18);
    }
    void OnDestroy()
    {
        if (roundedSprite != null) Destroy(roundedSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
}
