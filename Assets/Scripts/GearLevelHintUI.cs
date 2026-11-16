using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearLevelHintUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerPossessionController playerController;

    [Header("Room")]
    [SerializeField]
    private string checkpointId = "SEESAW_ROOM";

    [Header("Timing")]
    [SerializeField, Min(0.1f)]
    private float hintDuration = 10f;

    [Header("Movement")]
    [SerializeField, Min(0.001f)]
    private float movementDistance = 0.05f;

    [Header("Appearance")]
    [SerializeField]
    private float topOffset = -100f;

    [SerializeField, Min(12f)]
    private float fontSize = 28f;

    private const string FanMessage =
        "Press F to move the ball by blowing it with a fan";

    private const string ButtonMessage =
        "Press the buttons, or have same-colored buttons collide, to set all the gears in motion.";

    private enum Hint { None, Fan, Buttons }

    private MobileFormControls mobileControls;
    private MobileControlGraphic windIcon;
    private GameObject hudRoot;
    private TMP_Text hintText;

    private bool hintsStarted;
    private bool movementDetected;
    private bool fanHintPending;
    private bool anyButtonTriggered;
    private bool buttonHintPending;
    private Hint currentHint;

    private float displayTimeRemaining;
    private float movedDistance;

    private PossessableBody trackedBody;
    private Vector3 previousPosition;

    private void Start()
    {
        if (playerController == null)
        {
            UnityEngine.Debug.LogError(
                "GearLevelHintUI: Assign Player Controller.",
                this
            );

            enabled = false;
            return;
        }

        mobileControls = playerController.GetComponent<MobileFormControls>();
        CreateHUD();
    }

    private void CreateHUD()
    {
        hudRoot = new GameObject(
            "Gear Hint HUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        hudRoot.transform.SetParent(transform, false);

        Canvas canvas = hudRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = hudRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject(
            "Gear Hint Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(hudRoot.transform, false);

        hintText = textObject.GetComponent<TextMeshProUGUI>();
        hintText.text = string.Empty;
        hintText.fontSize = fontSize;
        hintText.enableAutoSizing = true;
        hintText.fontSizeMin = 18f;
        hintText.fontSizeMax = fontSize;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = Color.white;
        hintText.outlineColor = new Color32(0, 0, 0, 255);
        hintText.outlineWidth = 0.15f;
        hintText.raycastTarget = false;

        RectTransform rect = hintText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, topOffset);
        rect.sizeDelta = new Vector2(1500f, 110f);

        var iconRect = (RectTransform)new GameObject("Wind Icon", typeof(RectTransform)).transform;
        iconRect.SetParent(rect, false);
        iconRect.pivot = new Vector2(0.5f, 0f);
        iconRect.anchoredPosition = Vector2.up * (fontSize * 0.7f);
        iconRect.sizeDelta = Vector2.one * (fontSize * 1.75f);
        windIcon = iconRect.gameObject.AddComponent<MobileControlGraphic>();
        windIcon.Icon = MobileControlGraphic.Symbol.Wind;
        windIcon.raycastTarget = false;
        windIcon.enabled = false;
    }

    private void LateUpdate()
    {
        if (playerController == null || hintText == null)
            return;

        bool targetCheckpointActive =
            playerController.IsCurrentCheckpoint(checkpointId);

        // Activate hints after reaching this room's checkpoint.
        if (!hintsStarted)
        {
            if (!targetCheckpointActive)
                return;

            BeginHints();
        }

        // Hide and pause hints while another checkpoint is current.
        bool visible = targetCheckpointActive && !playerController.IsDead;
        if (hudRoot.activeSelf != visible) hudRoot.SetActive(visible);
        if (!visible)
        {
            trackedBody = null;
            return;
        }

        AdvanceHints();
        bool touchFan = currentHint == Hint.Fan && mobileControls != null && mobileControls.MobileMode;
        windIcon.enabled = touchFan;
        hintText.text = currentHint switch
        {
            Hint.Fan => touchFan ? "Tap to blow the ball." : FanMessage,
            Hint.Buttons => ButtonMessage,
            _ => string.Empty
        };
    }

    private void AdvanceHints()
    {
        // Pause the hint timers when gameplay is paused.
        if (Time.timeScale <= 0f)
            return;

        PossessableBody body = playerController.CurrentBody;

        if (body == null || !body.isActiveAndEnabled)
        {
            trackedBody = null;
            return;
        }

        DetectMovement(body);

        // Allow the current hint to finish before showing another.
        if (currentHint != Hint.None)
        {
            displayTimeRemaining -= Time.deltaTime;

            if (displayTimeRemaining > 0f)
                return;

            currentHint = Hint.None;
        }

        if (fanHintPending)
        {
            fanHintPending = false;
            ShowHint(Hint.Fan);
        }
        else if (buttonHintPending)
        {
            buttonHintPending = false;
            ShowHint(Hint.Buttons);
        }
    }

    public void BeginHints()
    {
        if (hintsStarted)
            return;

        hintsStarted = true;
        trackedBody = null;
        movedDistance = 0f;
    }

    private void DetectMovement(PossessableBody body)
    {
        Vector3 currentPosition = body.transform.position;

        if (trackedBody != body)
        {
            trackedBody = body;
            previousPosition = currentPosition;
            movedDistance = 0f;
            return;
        }

        // Require horizontal input and actual movement.
        if (!movementDetected &&
            Mathf.Abs(playerController.HorizontalInput) > 0.1f)
        {
            movedDistance += Mathf.Abs(
                currentPosition.x - previousPosition.x
            );

            if (movedDistance >= movementDistance)
            {
                movementDetected = true;
                fanHintPending = true;
            }
        }

        previousPosition = currentPosition;
    }

    private void ShowHint(Hint hint)
    {
        currentHint = hint;
        displayTimeRemaining = hintDuration;
    }

    // Connect each mechanism's On Pressed event to this method.
    public void NotifyButtonTriggered()
    {
        if (!isActiveAndEnabled ||
            playerController == null ||
            playerController.IsDead ||
            Time.timeScale <= 0f ||
            !playerController.IsCurrentCheckpoint(checkpointId))
        {
            return;
        }

        // Queue the button hint only on the first activation.
        if (anyButtonTriggered)
            return;

        anyButtonTriggered = true;
        buttonHintPending = true;
    }

    private void OnDisable()
    {
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (hudRoot != null)
            UnityEngine.Object.Destroy(hudRoot);
    }
}