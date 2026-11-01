using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-start camera intro: hold on a focus target for a fixed time,
/// zoom in, lock player control, and show a hint above the focus.
/// Then hand everything back to the previous setup. Attach next to
/// CameraFollow on the Main Camera.
/// </summary>
public class CameraFocusIntro : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CameraFollow on this same camera. Disabled during the intro.")]
    [SerializeField] private CameraFollow follow;
    [Tooltip("Player controller locked while the intro plays. Auto-found if empty.")]
    [SerializeField] private PlayerPossessionController playerController;
    [Tooltip("Object the camera watches first (e.g. Obstacle).")]
    [SerializeField] private Transform focusTarget;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float duration = 3f;

    [Header("Intro Framing")]
    [Tooltip("Camera position relative to the focus target.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [Tooltip("Higher = snappier camera. 0 = instant.")]
    [SerializeField, Min(0f)] private float smoothSpeed = 8f;
    [Tooltip("Orthographic size while focused; restored after the intro.")]
    [SerializeField, Min(0.1f)] private float introOrthographicSize = 3f;

    [Header("Hint Label")]
    [SerializeField] private string hintText =
        "these obstacle are safe to touch, but very heavy";
    [Tooltip("World-space offset from the focus target to the label anchor.")]
    [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField, Min(1f)] private float labelFontSize = 22f;

    private Camera attachedCamera;
    private float previousOrthographicSize;
    private bool previousControllerEnabled;
    private bool introStarted;
    private GameObject labelRoot;
    private TMP_Text labelText;
    private RectTransform labelRect;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();

        if (follow == null)
        {
            follow = GetComponent<CameraFollow>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerPossessionController>();
        }
    }

    private void Start()
    {
        if (focusTarget == null || attachedCamera == null || duration <= 0f)
        {
            // Nothing to stage; leave the normal camera setup untouched.
            enabled = false;
            return;
        }

        previousOrthographicSize = attachedCamera.orthographicSize;

        if (playerController != null)
        {
            previousControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (follow != null)
        {
            follow.enabled = false;
        }

        // Open directly on the focus so the intro does not slide over from
        // the player spawn.
        transform.position = focusTarget.position + offset;
        attachedCamera.orthographicSize = introOrthographicSize;

        CreateLabel();
        introStarted = true;
        StartCoroutine(ReturnToFollow());
    }

    private void LateUpdate()
    {
        if (focusTarget == null)
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

    private IEnumerator ReturnToFollow()
    {
        yield return new WaitForSeconds(duration);

        attachedCamera.orthographicSize = previousOrthographicSize;

        if (playerController != null)
        {
            playerController.enabled = previousControllerEnabled;
        }

        // CameraFollow smooths from the current position back to the player.
        if (follow != null)
        {
            follow.enabled = true;
        }

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
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 12f;
        labelText.fontSizeMax = labelFontSize;

        labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot =
            new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(900f, 48f);
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

        // If the scene unloads mid-intro, do not leave the player locked.
        if (playerController != null)
        {
            playerController.enabled = previousControllerEnabled;
        }

        if (follow != null)
        {
            follow.enabled = true;
        }

        if (attachedCamera != null)
        {
            attachedCamera.orthographicSize = previousOrthographicSize;
        }

        DestroyLabel();
    }
}
