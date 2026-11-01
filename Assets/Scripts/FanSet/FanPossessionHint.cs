using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Possession hint for one fan: while this fan is possessed, show a
/// control prompt above it for a fixed time. Attach next to FanBlower.
/// </summary>
public class FanPossessionHint : MonoBehaviour
{
    [SerializeField] private string hintText =
        "Press F to activate air flow，Press Q to quit form";
    [SerializeField, Min(0.1f)] private float displayDuration = 5f;
    [Tooltip("World-space offset from this fan to the label anchor.")]
    [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Min(1f)] private float labelFontSize = 20f;

    private PossessableBody body;
    private bool wasControlled;
    private GameObject labelRoot;
    private TMP_Text labelText;
    private RectTransform labelRect;
    private Coroutine activeShow;

    private void Awake()
    {
        body = GetComponent<PossessableBody>();

        if (body == null)
        {
            Debug.LogError("FanPossessionHint requires a PossessableBody on the same object.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        wasControlled = body.IsControlled;
    }

    private void Update()
    {
        bool controlled = body.IsControlled;
        bool justPossessed = controlled && !wasControlled;
        wasControlled = controlled;

        if (justPossessed)
        {
            Show();
        }
    }

    private void LateUpdate()
    {
        if (labelRect == null || labelRoot == null || !labelRoot.activeSelf)
        {
            return;
        }

        PositionLabel();
    }

    private void Show()
    {
        EnsureLabel();
        labelText.text = hintText;
        labelRoot.SetActive(true);
        PositionLabel();

        if (activeShow != null)
        {
            StopCoroutine(activeShow);
        }

        activeShow = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        if (labelRoot != null)
        {
            labelRoot.SetActive(false);
        }

        activeShow = null;
    }

    private void EnsureLabel()
    {
        if (labelRoot != null)
        {
            return;
        }

        labelRoot = new GameObject(
            "Fan Possession Hint",
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

    private void PositionLabel()
    {
        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            return;
        }

        Vector3 world = transform.position + labelWorldOffset;
        Vector3 screen = worldCamera.WorldToScreenPoint(world);

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

    private void OnDisable()
    {
        if (activeShow != null)
        {
            StopCoroutine(activeShow);
            activeShow = null;
        }

        if (labelRoot != null)
        {
            labelRoot.SetActive(false);
        }
    }
}
