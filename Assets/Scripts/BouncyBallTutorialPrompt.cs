using System.Collections;
using TMPro;
using UnityEngine;

public class BouncyBallTutorialPrompt : MonoBehaviour
{
    [SerializeField] private PossessableBody tutorialBall;
    [SerializeField] private TMP_Text promptText;
    [SerializeField, Min(0.1f)] private float displayDuration = 4f;
    [SerializeField, Min(0f)] private float verticalWorldOffset = 1.4f;

    private bool wasControlled;
    private bool tutorialShown;
    private RectTransform promptRect;
    private RectTransform canvasRect;
    private Camera mainCamera;

    private void Start()
    {
        if (tutorialBall == null || promptText == null)
        {
            Debug.LogError(
                "Assign the lower bouncing ball and tutorial text.",
                this
            );

            enabled = false;
            return;
        }

        promptRect = promptText.rectTransform;
        Canvas canvas = promptText.canvas;
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        mainCamera = Camera.main;

        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);

        promptText.text = "";
        wasControlled = tutorialBall.IsControlled;
    }

    private void Update()
    {
        bool isControlled = tutorialBall.IsControlled;

        if (!tutorialShown && !wasControlled && isControlled)
        {
            tutorialShown = true;
            StartCoroutine(ShowTutorial());
        }

        wasControlled = isControlled;
    }

    private void LateUpdate()
    {
        if (string.IsNullOrEmpty(promptText.text))
        {
            return;
        }

        PositionPromptAboveBall();
    }

    private IEnumerator ShowTutorial()
    {
        promptText.text = "A — Move Left     D — Move Right";
        PositionPromptAboveBall();
        yield return new WaitForSecondsRealtime(displayDuration);
        promptText.text = "";
    }

    private void PositionPromptAboveBall()
    {
        if (promptRect == null || canvasRect == null || tutorialBall == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 worldPosition = tutorialBall.transform.position
            + Vector3.up * verticalWorldOffset;
        Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint))
        {
            promptRect.anchoredPosition = localPoint;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (promptText != null)
        {
            promptText.text = "";
        }
    }
}
