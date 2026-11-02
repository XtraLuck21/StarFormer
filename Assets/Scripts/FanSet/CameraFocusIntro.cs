using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-start camera intro: lock the player and hold on a focus target
/// for a fixed time with a close orthographic size and a hint label.
/// Then smooth back to the camera's authored room view and stay there.
/// Attach to the Main Camera.
/// </summary>
public class CameraFocusIntro : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player controller locked while the intro plays. Auto-found if empty.")]
    [SerializeField] private PlayerPossessionController playerController;
    [Tooltip("Object the camera watches first (e.g. Obstacle).")]
    [SerializeField] private Transform focusTarget;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float duration = 3f;
    [Tooltip("Seconds to ease from the focus view back to the room view.")]
    [SerializeField, Min(0.05f)] private float returnDuration = 0.8f;

    [Header("Intro Framing")]
    [Tooltip("Camera position relative to the focus target.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [Tooltip("Higher = snappier camera. 0 = instant.")]
    [SerializeField, Min(0f)] private float smoothSpeed = 8f;
    [Tooltip("Orthographic size while focused. Room view size is taken from the camera.")]
    [SerializeField, Min(0.1f)] private float introOrthographicSize = 3f;

    [Header("Hint Label")]
    [SerializeField] private string hintText =
        "these obstacle are safe to touch, but very heavy";
    [Tooltip("World-space offset from the focus target to the label anchor.")]
    [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField, Min(1f)] private float labelFontSize = 22f;

    private Camera attachedCamera;
    private Vector3 roomPosition;
    private float roomOrthographicSize;
    private bool previousControllerEnabled;
    private bool introStarted;
    private bool returning;
    private GameObject labelRoot;
    private TMP_Text labelText;
    private RectTransform labelRect;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerPossessionController>();
        }
    }

    private void Start()
    {
        if (focusTarget == null || attachedCamera == null || duration <= 0f)
        {
            // Nothing to stage; leave the authored room camera untouched.
            enabled = false;
            return;
        }

        // The camera's authored transform IS the room view: capture it
        // before borrowing it for the focus shot.
        roomPosition = transform.position;
        roomOrthographicSize = attachedCamera.orthographicSize;

        if (playerController != null)
        {
            previousControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        // Open directly on the focus so the intro does not slide over from
        // the room view.
        transform.position = focusTarget.position + offset;
        attachedCamera.orthographicSize = introOrthographicSize;

        CreateLabel();
        introStarted = true;
        StartCoroutine(ReturnToRoomView());
    }

    private void LateUpdate()
    {
        if (returning || focusTarget == null)
        {
            return;
        }

        Vector3 desired = focusTarget.position + offset;

        if (smoothSpeed <= 0f)
        {
            transform.position = desired;
        }
        else
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }

        UpdateLabelPosition();
    }

    private IEnumerator ReturnToRoomView()
    {
        yield return new WaitForSeconds(duration);

        returning = true;

        if (labelRoot != null)
        {
            labelRoot.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.enabled = previousControllerEnabled;
        }

        Vector3 startPosition = transform.position;
        float startSize = attachedCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            // Smoothstep for a soft arrival at the room view.
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, roomPosition, t);
            attachedCamera.orthographicSize = Mathf.Lerp(startSize, roomOrthographicSize, t);
            yield return null;
        }

        transform.position = roomPosition;
        attachedCamera.orthographicSize = roomOrthographicSize;

        DestroyLabel();
        enabled = false;
    }

    private void CreateLabel()
    {
        labelRoot = new GameObject(
            "Intro Hint",
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

        var labelObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        labelObject.transform.SetParent(labelRoot.transform, false);

        labelText = labelObject.GetComponent<TMP_Text>();
        labelText.text = hintText;
        labelText.fontSize = labelFontSize;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.outlineColor = new Color32(0, 0, 0, 255);
        labelText.outlineWidth = 0.2f;
        labelText.raycastTarget = false;
        // Fixed size: auto-sizing would clamp the text back into the rect
        // and ignore the inspector value.
        labelText.enableAutoSizing = false;

        labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot =
            new Vector2(0.5f, 0.5f);
        // Tall enough for the largest allowed line; alignment keeps it centered.
        labelRect.sizeDelta = new Vector2(1400f, labelFontSize * 2.5f);
    }

    private void UpdateLabelPosition()
    {
        if (labelRect == null || attachedCamera == null || focusTarget == null)
        {
            return;
        }

        Vector3 world = focusTarget.position + labelWorldOffset;
        Vector3 screen = attachedCamera.WorldToScreenPoint(world);

        bool visible = screen.z > 0f;
        labelText.enabled = visible;
        if (!visible)
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
    }

    private void DestroyLabel()
    {
        if (labelRoot != null)
        {
            Destroy(labelRoot);
            labelRoot = null;
            labelText = null;
            labelRect = null;
        }
    }

    private void OnDisable()
    {
        if (!introStarted)
        {
            return;
        }

        introStarted = false;

        // If the scene unloads mid-intro, do not leave the player locked
        // or the camera stuck on the focus shot.
        if (playerController != null)
        {
            playerController.enabled = previousControllerEnabled;
        }

        if (attachedCamera != null)
        {
            transform.position = roomPosition;
            attachedCamera.orthographicSize = roomOrthographicSize;
        }

        DestroyLabel();
    }
}
