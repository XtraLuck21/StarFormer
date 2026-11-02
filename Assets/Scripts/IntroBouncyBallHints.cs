using TMPro;
using UnityEngine;

// Scene-local lessons; observe the existing ball physics without changing its motion.
public class IntroBouncyBallHints : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Rect quickBounceArea = new Rect(2.35f, -2.85f, 2.6f, 2.35f);
    [SerializeField] private Vector2 entryAnchor = new Vector2(-4.9f, 3.5f);
    [SerializeField] private Vector2 waterAnchor = new Vector2(-4.2f, 2.8f);
    [SerializeField] private Vector2 landAnchor = new Vector2(2.15f, 3.5f);
    [SerializeField] private Vector2 quickBounceAnchor = new Vector2(2.15f, 2.8f);
    [SerializeField, Min(1f)] private float holdSeconds = 6f;

    private bool spikeDeathShown;
    private bool introduced;
    private bool floatingShown;
    private bool landingShown;
    private bool quickBounceShown;
    private bool awaitingLandBounce;
    private bool wasFloating;
    private float floatingSeconds;
    private float quickBounceSeconds;
    private float age;
    private Vector2 anchor;
    private Color normalColor;
    private float labelHalfWidth = 310f;
    private Canvas canvas;
    private Camera sceneCamera;

    private void Start()
    {
        if (playerController == null || titleText == null || subtitleText == null)
        {
            Debug.LogError("Assign the ball tutorial controller and both hint labels.", this);
            enabled = false;
            return;
        }

        canvas = titleText.canvas;
        sceneCamera = Camera.main;
        ConfigureLabel(titleText, 36f, new Vector2(620f, 60f));
        normalColor = titleText.color;
        subtitleText.text = string.Empty;
        subtitleText.enabled = false;
        Show("A little more bounce?", entryAnchor);
    }

    private static void ConfigureLabel(TMP_Text label, float fontSize, Vector2 size)
    {
        label.raycastTarget = false;
        label.enableAutoSizing = false;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
        label.rectTransform.pivot = new Vector2(.5f, .5f);
        label.rectTransform.sizeDelta = size;
    }

    private void LateUpdate()
    {
        if (playerController == null || titleText == null || subtitleText == null) return;
        if (playerController.IsDead)
        {
            // Death freezes game time for two seconds before reloading this tutorial.
            // Keep the warning readable during that existing pause, without delaying respawn.
            if (playerController.LastDeathCause == AnalyticsDeathCause.Spike)
            {
                if (!spikeDeathShown)
                {
                    spikeDeathShown = true;
                    Show("Ouch! Spikes\naren't for hugs.", new Vector2(1.17f, 1.1f), true);
                }
                PositionLabels();
            }
            else
            {
                titleText.alpha = 0f;
            }
            return;
        }
        if (Time.timeScale <= 0f) return;

        PossessableBody ball = playerController.CurrentBody;
        if (ball != null && ball.IsControlled && ball.Form == BodyForm.BouncyBall &&
            ball.BodyCollider != null && ball.Body != null)
        {
            if (!introduced)
            {
                introduced = true;
                Show("Born to bounce.", entryAnchor);
            }

            Bounds bounds = ball.BodyCollider.bounds;
            bool floating = WaterZone.TryGetSurfaceY(bounds, out float surfaceY) &&
                Mathf.Abs(bounds.min.y - surfaceY) < .12f &&
                Mathf.Abs(ball.Body.linearVelocity.y) < .2f;
            floatingSeconds = floating ? floatingSeconds + Time.deltaTime : 0f;
            if (floating) awaitingLandBounce = true;

            if (!floatingShown && floatingSeconds >= .35f)
            {
                floatingShown = true;
                Show("On water, we float!", waterAnchor);
            }
            else if (!landingShown && awaitingLandBounce && !floating && !wasFloating &&
                ball.Body.linearVelocity.y > .5f)
            {
                landingShown = true;
                awaitingLandBounce = false;
                Show("Dry land.\nBounce time!", landAnchor, true);
            }

            bool inQuickArea = !floating && quickBounceArea.Contains(ball.Body.position);
            quickBounceSeconds = inQuickArea ? quickBounceSeconds + Time.deltaTime : 0f;
            if (!quickBounceShown && quickBounceSeconds >= .12f)
            {
                quickBounceShown = true;
                Show("Small space.\nSpeedy bounces!", quickBounceAnchor, true);
            }
            wasFloating = floating;
        }
        else
        {
            floatingSeconds = quickBounceSeconds = 0f;
            wasFloating = awaitingLandBounce = false;
        }

        age += Time.deltaTime;
        titleText.alpha = subtitleText.alpha = 1f - Mathf.Clamp01(age - holdSeconds);
        PositionLabels();
    }

    private void Show(string title, Vector2 position, bool onRoof = false)
    {
        titleText.text = title;
        titleText.color = onRoof ? Color.white : normalColor;
        labelHalfWidth = onRoof ? 150f : 310f;
        titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, onRoof ? 100f : 60f);
        titleText.alpha = subtitleText.alpha = 1f;
        anchor = position;
        age = 0f;
    }

    private void PositionLabels()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (sceneCamera == null || canvas == null) return;
        var canvasRect = (RectTransform)canvas.transform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            sceneCamera.WorldToScreenPoint(anchor), uiCamera, out Vector2 point)) return;

        // Keep the headline in the room, rather than following each bounce.
        float halfWidth = Mathf.Min(labelHalfWidth, canvasRect.rect.width * .5f - 20f);
        titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, halfWidth * 2f);
        point.x = Mathf.Clamp(point.x, canvasRect.rect.xMin + halfWidth + 16f,
            canvasRect.rect.xMax - halfWidth - 16f);
        point.y = Mathf.Clamp(point.y, canvasRect.rect.yMin + 130f, canvasRect.rect.yMax - 50f);
        titleText.rectTransform.anchoredPosition = point;
    }

    private void OnDisable()
    {
        if (titleText != null) titleText.text = string.Empty;
        if (subtitleText != null) subtitleText.text = string.Empty;
    }
}
