using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime HUD shared by positional and Bouncy Ball checkpoints.</summary>
public sealed class CheckpointFeedbackUI : MonoBehaviour
{
    [Header("Level 1 Bubble Reminder")]
    [SerializeField, Min(0.5f)] private float bubbleIdleDelay = 2f;
    [Tooltip("Y displacement allowed while waiting; horizontal motion does not reset the timer.")]
    [SerializeField, Min(0.001f)] private float bubbleVerticalTolerance = 0.05f;
    [SerializeField] private string secondCheckpointName = "BouncyBall_Upper";
    private TMP_Text bubbleIdleHint;
    private PossessableBody trackedBubble;
    private float bubbleIdleOriginY;
    private float bubbleIdleTime;

    private sealed class Marker
    {
        public Transform Target;
        public string Id;
        public TMP_Text Label;
        public Vector3 Position;
        public bool IsBall;
    }

    private readonly List<Marker> markers = new List<Marker>();
    private PlayerPossessionController player;
    private GameObject root;
    private RectTransform canvasRect;
    private TMP_Text toast;
    private float savedUntil;
    private Camera worldCamera;

    public void Initialize(PlayerPossessionController controller)
    {
        player = controller;
        if (root != null) return;

        root = new GameObject("Checkpoint HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        // Keep the HUD in the player's scene and clean it up with the controller.
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = root.GetComponent<RectTransform>();

        toast = CreateLabel("Saved notification", root.transform, 20f, -45f, 360f);
        toast.color = new Color(0.35f, 1f, 0.7f);
        bubbleIdleHint = CreateLabel("Bubble pop reminder", root.transform, 18f, 0f, 260f);
        bubbleIdleHint.rectTransform.anchorMin = bubbleIdleHint.rectTransform.anchorMax = Vector2.one * 0.5f;
        bubbleIdleHint.text = "Press E to pop bubble";
        bubbleIdleHint.enabled = false;
    }

    public void ShowSaved()
    {
        savedUntil = Time.unscaledTime + 2.5f;
    }

    public void RegisterMarker(Transform target, string id, bool isBall = false)
    {
        TMP_Text label = CreateLabel("Checkpoint " + id, root.transform, 16f, 0f, 180f);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.one * 0.5f;
        markers.Add(new Marker
        {
            Target = target, Id = id, Label = label,
            Position = target.position, IsBall = isBall
        });
    }

    private static TMP_Text CreateLabel(string name, Transform parent, float size, float y, float width)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.fontSize = size;
        label.fontStyle = FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineColor = new Color32(0, 0, 0, 255);
        label.outlineWidth = 0.15f;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = size;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, 36f);
        label.text = "";
        return label;
    }

    private void LateUpdate()
    {
        if (player == null || root == null) return;

        toast.text = Time.unscaledTime < savedUntil
            ? "CHECKPOINT SAVED"
            : "";

        if (worldCamera == null) worldCamera = Camera.main;
        UpdateBubbleIdleHint();
        if (worldCamera == null) return;
        foreach (Marker marker in markers)
        {
            if (marker.Target == null)
            {
                marker.Label.enabled = false;
                continue;
            }

            // Ball recovery uses its original spawn, even after the ball moves.
            Vector3 screen = worldCamera.WorldToScreenPoint(
                marker.IsBall ? marker.Position : marker.Target.position);
            bool visible = screen.z > 0f && worldCamera.pixelRect.Contains((Vector2)screen);
            marker.Label.enabled = visible;
            if (!visible) continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screen, null, out Vector2 local);
            marker.Label.rectTransform.anchoredPosition = local + Vector2.up * 45f;
            bool active = marker.IsBall
                ? player.IsCurrentBouncyBallCheckpoint(marker.Id)
                : player.IsCurrentCheckpoint(marker.Id);
            marker.Label.text = active ? "SAVED\nv"
                : marker.IsBall ? "POSSESS TO SAVE\nv" : "CHECKPOINT\nv";
            marker.Label.color = active
                ? new Color(0.35f, 1f, 0.7f)
                : new Color(1f, 0.85f, 0.35f);
        }
    }

    private void UpdateBubbleIdleHint()
    {
        bubbleIdleHint.enabled = false;
        PossessableBody body = player.CurrentBody;
        BouncyBallCheckpointSession session = BouncyBallCheckpointSession.Instance;
        bool reachedSecondCheckpoint = session != null &&
            session.HasReachedCheckpoint(gameObject.scene.path, secondCheckpointName);

        if (gameObject.scene.name != "Level01" || !player.isActiveAndEnabled || player.IsDead ||
            Time.timeScale <= 0f || reachedSecondCheckpoint || body == null ||
            !body.isActiveAndEnabled || body.Form != BodyForm.Bubble || body.Body == null)
        {
            ResetBubbleIdleHint();
            return;
        }

        float positionY = body.Body.position.y;
        if (trackedBubble != body)
        {
            trackedBubble = body;
            bubbleIdleOriginY = positionY;
            bubbleIdleTime = 0f;
        }
        // A bubble can still slide sideways while blocked vertically.
        // Only meaningful Y displacement resets the timer; tolerate small physics jitter.
        if (Mathf.Abs(positionY - bubbleIdleOriginY) > bubbleVerticalTolerance)
        {
            bubbleIdleOriginY = positionY;
            bubbleIdleTime = 0f;
            return;
        }

        bubbleIdleTime += Time.deltaTime;
        if (bubbleIdleTime < bubbleIdleDelay || worldCamera == null) return;

        Vector3 screen = worldCamera.WorldToScreenPoint(body.transform.position);
        if (screen.z <= 0f || !worldCamera.pixelRect.Contains((Vector2)screen)) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        Vector2 hintPosition = local + Vector2.up * 55f;
        // Keep the reminder readable when a bubble is stuck near a screen edge.
        float marginX = Mathf.Min(140f, canvasRect.rect.width * 0.5f);
        hintPosition.x = Mathf.Clamp(hintPosition.x, canvasRect.rect.xMin + marginX, canvasRect.rect.xMax - marginX);
        hintPosition.y = Mathf.Clamp(hintPosition.y, canvasRect.rect.yMin + 30f, canvasRect.rect.yMax - 30f);
        bubbleIdleHint.rectTransform.anchoredPosition = hintPosition;
        bubbleIdleHint.enabled = true;
    }

    private void ResetBubbleIdleHint()
    {
        trackedBubble = null;
        bubbleIdleTime = 0f;
        if (bubbleIdleHint != null) bubbleIdleHint.enabled = false;
    }

    private void OnEnable()
    {
        if (root != null) root.SetActive(true);
    }

    private void OnDisable()
    {
        ResetBubbleIdleHint();
        if (root != null) root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root);
    }
}
