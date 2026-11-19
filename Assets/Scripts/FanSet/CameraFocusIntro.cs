using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-start camera intro: lock the player and hold on a focus target
/// for a fixed time with a close orthographic size and a hint label.
/// Optionally show the seesaw next, then return to the authored room view.
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

    [Header("Seesaw Preview")]
    [SerializeField] private Transform seesawFocusTarget;
    [SerializeField, Min(0f)] private float seesawHoldDuration = 3f;
    [SerializeField, Min(0.05f)] private float seesawTravelDuration = 0.8f;
    [SerializeField] private Vector3 seesawOffset = new Vector3(0f, 1f, -10f);
    [SerializeField, Min(0.1f)] private float seesawOrthographicSize = 4.2f;
    [SerializeField] private string seesawHintText = "Find a way to raise the flag.";

    private Camera attachedCamera;
    private Vector3 roomPosition;
    private float roomOrthographicSize;
    private bool previousControllerEnabled;
    private bool introStarted;
    private bool transitioning;
    private Transform activeFocusTarget;
    private Vector3 activeOffset;
    private bool worldSpaceLabel = true;
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
        activeFocusTarget = focusTarget;
        activeOffset = offset;
        introStarted = true;
        UpdateLabelPosition();
        StartCoroutine(PlayIntro());
    }

    private void LateUpdate()
    {
        if (transitioning || activeFocusTarget == null)
        {
            return;
        }

        Vector3 desired = activeFocusTarget.position + activeOffset;

        if (smoothSpeed <= 0f)
        {
            transform.position = desired;
        }
        else
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }

        if (worldSpaceLabel) UpdateLabelPosition();
    }

    private IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(duration);

        transitioning = true;
        if (labelRoot != null) labelRoot.SetActive(false);

        if (seesawFocusTarget != null && seesawHoldDuration > 0f)
        {
            Vector3 position = seesawFocusTarget.position + seesawOffset;
            yield return MoveCamera(position, GetSeesawShotSize(position), seesawTravelDuration);
            activeFocusTarget = seesawFocusTarget;
            activeOffset = seesawOffset;
            transitioning = false;
            ShowSeesawHint();
            // The full hold starts after arrival, so travel never consumes the three-second preview.
            yield return new WaitForSeconds(seesawHoldDuration);
            transitioning = true;
            if (labelRoot != null) labelRoot.SetActive(false);
        }

        yield return MoveCamera(roomPosition, roomOrthographicSize, returnDuration);

        if (playerController != null)
        {
            playerController.enabled = previousControllerEnabled;
        }
        DestroyLabel();
        introStarted = false;
        enabled = false;
    }

    private float GetSeesawShotSize(Vector3 cameraPosition)
    {
        float size = seesawOrthographicSize;
        float aspect = Mathf.Max(0.1f, attachedCamera.aspect);
        foreach (SpriteRenderer renderer in seesawFocusTarget.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Bounds bounds = renderer.bounds;
            float width = Mathf.Max(Mathf.Abs(bounds.min.x - cameraPosition.x), Mathf.Abs(bounds.max.x - cameraPosition.x));
            float height = Mathf.Max(Mathf.Abs(bounds.min.y - cameraPosition.y), Mathf.Abs(bounds.max.y - cameraPosition.y));
            size = Mathf.Max(size, (width + 0.5f) / aspect, height + 0.5f);
        }
        return size;
    }

    private void ShowSeesawHint()
    {
        if (labelRoot == null) return;
        worldSpaceLabel = false;
        labelText.text = seesawHintText;
        labelText.enabled = true;
        labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -80f);
        labelRoot.SetActive(true);
    }

    private IEnumerator MoveCamera(Vector3 destination, float orthographicSize, float seconds)
    {
        Vector3 startPosition = transform.position;
        float startSize = attachedCamera.orthographicSize;
        float elapsed = 0f;
        float travelDuration = Mathf.Max(0.05f, seconds);

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            // Smoothstep for a soft arrival at the room view.
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, destination, t);
            attachedCamera.orthographicSize = Mathf.Lerp(startSize, orthographicSize, t);
            yield return null;
        }

        transform.position = destination;
        attachedCamera.orthographicSize = orthographicSize;
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
        StopAllCoroutines();

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
