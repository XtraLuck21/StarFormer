using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Only first-use ability reminders and the inescapable Ball pocket need guidance here.
public sealed class Level01WaterRouteHints : MonoBehaviour
{
    public PlayerPossessionController player;
    public AdvancedLevelCamera2D view;
    public TMP_Text lesson;
    public TMP_Text recovery;
    public Rect area = new Rect(7.4f, .75f, 12.9f, 9.8f);
    [SerializeField, Min(1)] private float reminderSeconds = 5f;
    PossessableBody previous;
    BouncyBallAbility ball;
    MobileFormControls mobile;
    Camera worldCamera;
    Canvas canvas;
    RectTransform card;
    Vector2 cardAnchor;
    TMP_Text heading, instruction;
    float reminderRemaining;
    bool sawBall, sawBubble, activeReminder;
    public string LessonText { get; private set; } = "";
    public string RecoveryText { get; private set; } = "";

    void Start()
    {
        mobile = player != null ? player.GetComponent<MobileFormControls>() : null;
        worldCamera = view != null ? view.GetComponent<Camera>() : Camera.main;
        BuildUI();
    }

    void Update()
    {
        if (lesson != null) lesson.enabled = false;
        if (recovery != null) recovery.enabled = false;
        if (card == null) return;
        card.gameObject.SetActive(false);
        LessonText = RecoveryText = "";
        var body = player != null ? player.CurrentBody : null;
        bool available = body != null && player.isActiveAndEnabled && !player.IsDead &&
            !player.IsRestarting && Time.timeScale > 0 && (view == null || !view.IsCinematic);
        // Pause/cinematics hide reminders without consuming their display time.
        if (!available) return;
        Vector2 pos = body.Body.position;
        if (!area.Contains(pos) || (pos.x > 18.8f && pos.y < 3.8f))
        {
            activeReminder = false;
            return;
        }
        if (previous != body)
        {
            reminderRemaining = 0; activeReminder = false;
            previous = body; ball = body.GetComponent<BouncyBallAbility>();
        }
        bool newForm = (body.Form == BodyForm.BouncyBall && !sawBall) ||
            (body.Form == BodyForm.Bubble && !sawBubble);
        if (newForm)
        {
            sawBall |= body.Form == BodyForm.BouncyBall;
            sawBubble |= body.Form == BodyForm.Bubble;
            reminderRemaining = reminderSeconds; activeReminder = true;
        }
        // The narrow right-hand pocket has no usable Ball launch surface or manual release.
        if (body.Form == BodyForm.BouncyBall && pos.x > 16.8f && pos.x < 18.2f &&
            pos.y < 7.5f && body.IsInWater)
        {
            activeReminder = false;
            RecoveryText = RestartAction();
            Show(new Vector2(17.5f, 7.4f), "No ground to bounce.\n" + RecoveryText, false);
            return;
        }
        bool reminding = activeReminder && (reminderRemaining > 0 ||
            (body.Form == BodyForm.BouncyBall && ball != null && ball.IsRisingInWater));
        if (!reminding) { activeReminder = false; return; }
        reminderRemaining -= Time.deltaTime;
        Show(new Vector2(11.6f, 6.9f), body.Form == BodyForm.BouncyBall
            ? "Bouncy balls float!" : "Bubbles swim in water\nand float in air.");
    }

    string RestartAction() => mobile != null && mobile.MobileMode ? "Home > Restart Level" : "Press R to restart.";

    void Show(Vector2 anchor, string text, bool showRemember = true)
    {
        cardAnchor = anchor;
        heading.gameObject.SetActive(showRemember);
        heading.text = "REMEMBER?";
        card.sizeDelta = new Vector2(400, showRemember ? 124 : 96);
        instruction.rectTransform.anchoredPosition = new Vector2(0, showRemember ? -77 : -48);
        instruction.text = text;
        LessonText = showRemember ? heading.text + "\n" + text : text;
        card.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (worldCamera == null || card == null || !card.gameObject.activeSelf) return;
        // Scene anchors stay still when the possessed body bounces or floats upward.
        Vector2 screen = worldCamera.WorldToScreenPoint(cardAnchor);
        Rect safe = Screen.safeArea;
        float scale = canvas.scaleFactor;
        Vector2 half = card.sizeDelta * scale * .5f;
        screen.x = Mathf.Clamp(screen.x, safe.xMin + half.x + 12f * scale, safe.xMax - half.x - 12f * scale);
        screen.y = Mathf.Clamp(screen.y, safe.yMin + half.y + 65f * scale, safe.yMax - half.y - 24f * scale);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out var local);
        card.anchoredPosition = local;
    }

    void BuildUI()
    {
        var obj = new GameObject("Water lesson HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        obj.transform.SetParent(transform, false);
        canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scale = obj.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1280, 720); scale.matchWidthOrHeight = .5f;
        var panel = new GameObject("Current lesson", typeof(RectTransform), typeof(Image), typeof(Outline));
        card = panel.GetComponent<RectTransform>(); card.SetParent(canvas.transform, false);
        card.sizeDelta = new Vector2(400, 124); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
        var bg = panel.GetComponent<Image>(); bg.color = new Color32(12, 35, 28, 245); bg.raycastTarget = false;
        var outline = panel.GetComponent<Outline>(); outline.effectColor = new Color32(255, 211, 77, 255); outline.effectDistance = new Vector2(2, -2);
        heading = Label("Heading", new Vector2(0, -24), new Vector2(364, 30), 18, new Color32(255, 211, 77, 255));
        heading.fontStyle = FontStyles.Bold;
        instruction = Label("Instruction", new Vector2(0, -77), new Vector2(364, 72), 24, new Color32(255, 244, 214, 255));
        card.gameObject.SetActive(false);
    }

    TMP_Text Label(string name, Vector2 position, Vector2 size, float font, Color color)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform; rect.SetParent(card, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        if (lesson != null) text.font = lesson.font;
        text.fontSize = font; text.color = color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }
    void OnDisable()
    {
        if (lesson != null) lesson.enabled = false;
        if (recovery != null) recovery.enabled = false;
        if (card != null) card.gameObject.SetActive(false);
        LessonText = RecoveryText = "";
    }
}
