using System.Collections;
using TMPro;
using UnityEngine;

// Optional, latched Box D switch for the upper return in Level01_Advanced.
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class AdvancedRouteSwitch : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody targetBox;
    [SerializeField] private GameObject routeA;
    [SerializeField] private GameObject routeB;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Transform buttonFace;
    [SerializeField] private AdvancedLevelCamera2D levelCamera;
    [SerializeField] private Color idleColor = new Color32(255, 211, 77, 255);
    [SerializeField] private Color activatedColor = new Color32(102, 228, 218, 255);

    [Header("Route Presentation")]
    [SerializeField, Min(.1f)] private float cameraMoveSeconds = .85f;
    [SerializeField, Min(0f)] private float previewHoldSeconds = 1f;
    [SerializeField, Min(.1f)] private float spikeSinkSeconds = 1.6f;
    [SerializeField, Min(.7f)] private float spikeSinkDistance = 1f;
    [SerializeField, Min(0f)] private float buttonPressDistance = .12f;
    [SerializeField, Min(.1f)] private float previewHalfWidth = 1.4f;
    [SerializeField] private Vector2 previewCenter = new Vector2(8f, 13.7f);
    [SerializeField] private Vector2 overviewCenter = new Vector2(14f, 14f);
    [SerializeField, Min(1f)] private float previewSize = 3.4f;
    [SerializeField, Min(1f)] private float overviewSize = 6.4f;

    private BoxCollider2D sensor;
    private SpriteRenderer buttonRenderer;
    private Vector3 faceStart;
    private Vector3 spikesStart;
    private Camera viewCamera;
    private Vector3 returnPosition;
    private float returnSize;
    private float previousTimeScale;
    private bool controllerWasEnabled;
    private bool ownsPresentation;
    private bool previewShown;
    public bool IsActivated { get; private set; }
    public bool IsPresenting => ownsPresentation;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        buttonRenderer = buttonFace != null
            ? buttonFace.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
        sensor.isTrigger = true;
        if (playerController == null || targetBox == null || routeA == null ||
            routeB == null || routeA == routeB || buttonRenderer == null ||
            transform.IsChildOf(routeA.transform) || transform.IsChildOf(routeB.transform))
        {
            Debug.LogError("Assign the player, Box D, and separate A/B route roots.", this);
            enabled = false;
            return;
        }
        if (levelCamera == null) levelCamera = FindFirstObjectByType<AdvancedLevelCamera2D>();
        if (levelCamera != null) viewCamera = levelCamera.GetComponent<Camera>();
        spikesStart = routeA.transform.localPosition;
        if (buttonFace != null) faceStart = buttonFace.localPosition;
        routeA.SetActive(true);
        routeB.SetActive(false);
        buttonRenderer.color = idleColor;
        if (label != null) label.text = "SAVE MY LIFE";
    }

    private void Update()
    {
        if (IsActivated || ownsPresentation || Time.timeScale <= 0f ||
            !playerController.isActiveAndEnabled || playerController.IsDead ||
            !targetBox.isActiveAndEnabled || !targetBox.IsReady ||
            targetBox.Form != BodyForm.Box || !targetBox.IsControlled ||
            playerController.CurrentBody != targetBox)
            return;

        Rigidbody2D body = targetBox.Body;
        Collider2D boxCollider = targetBox.BodyCollider;
        if (!body.simulated || body.bodyType != RigidbodyType2D.Dynamic ||
            !boxCollider.enabled || !targetBox.IsGrounded())
            return;

        Bounds box = boxCollider.bounds;
        Bounds button = sensor.bounds;
        bool underButton = box.center.y < button.min.y &&
            Mathf.Abs(box.center.x - button.center.x) <= previewHalfWidth;
        if (!previewShown && underButton)
        {
            previewShown = true;
            if (BeginPresentation())
            {
                StartCoroutine(PreviewRoute());
                return;
            }
        }

        if (!sensor.IsTouching(boxCollider) || box.center.y >= button.min.y ||
            box.max.y < button.min.y || box.max.x <= button.min.x || box.min.x >= button.max.x)
            return;

        IsActivated = true;
        buttonRenderer.color = activatedColor;
        if (label != null) label.text = "LIFE SAVED";
        // Hazards stop hurting immediately, but remain visible until fully below the floor.
        foreach (var collider in routeA.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        foreach (var renderer in routeA.GetComponentsInChildren<SpriteRenderer>()) renderer.sortingOrder = -1;
        if (BeginPresentation()) StartCoroutine(RevealSafeRoute());
        else FinishRouteChange();
    }

    private bool BeginPresentation()
    {
        if (levelCamera == null || viewCamera == null || !levelCamera.TryBeginCinematic()) return false;
        returnPosition = viewCamera.transform.position;
        returnSize = viewCamera.orthographicSize;
        previousTimeScale = Time.timeScale;
        controllerWasEnabled = playerController.enabled;
        ownsPresentation = true;
        playerController.ClearInputForRestartMenu();
        targetBox.Body.linearVelocity = Vector2.zero;
        playerController.enabled = false;
        Time.timeScale = 0f;
        return true;
    }

    private IEnumerator PreviewRoute()
    {
        yield return MoveCamera(previewCenter, FramedSize(previewSize));
        yield return new WaitForSecondsRealtime(previewHoldSeconds);
        yield return MoveCamera(returnPosition, returnSize);
        RestorePresentation();
    }

    private IEnumerator RevealSafeRoute()
    {
        // Press into the housing while the camera pulls back to include button and route.
        if (buttonFace != null) buttonFace.localPosition = faceStart + Vector3.up * buttonPressDistance;
        yield return MoveCamera(overviewCenter, FramedSize(overviewSize));
        yield return new WaitForSecondsRealtime(.25f);
        float elapsed = 0f;
        while (elapsed < spikeSinkSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / spikeSinkSeconds));
            routeA.transform.localPosition = spikesStart + Vector3.down * (spikeSinkDistance * progress);
            yield return null;
        }
        FinishRouteChange();
        yield return new WaitForSecondsRealtime(.6f);
        yield return MoveCamera(returnPosition, returnSize);
        RestorePresentation();
    }

    private float FramedSize(float size) => size * Mathf.Max(1f, (16f / 9f) / Mathf.Max(.1f, viewCamera.aspect));

    private IEnumerator MoveCamera(Vector2 center, float size)
    {
        Vector3 start = viewCamera.transform.position;
        float startSize = viewCamera.orthographicSize;
        Vector3 end = new Vector3(center.x, center.y, start.z);
        float elapsed = 0f;
        while (elapsed < cameraMoveSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / cameraMoveSeconds));
            levelCamera.SetCinematicView(Vector3.Lerp(start, end, progress), Mathf.Lerp(startSize, size, progress));
            yield return null;
        }
        levelCamera.SetCinematicView(end, size);
    }

    private void FinishRouteChange()
    {
        routeA.transform.localPosition = spikesStart + Vector3.down * spikeSinkDistance;
        routeA.SetActive(false);
        routeB.SetActive(true);
        if (buttonFace != null) buttonFace.localPosition = faceStart + Vector3.up * buttonPressDistance;
    }

    private void RestorePresentation()
    {
        if (!ownsPresentation) return;
        ownsPresentation = false;
        if (levelCamera != null)
        {
            levelCamera.SetCinematicView(returnPosition, returnSize);
            levelCamera.EndCinematic();
        }
        if (playerController != null) playerController.enabled = controllerWasEnabled;
        Time.timeScale = previousTimeScale;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        // Disabling or unloading during an animation must never leave gameplay paused.
        if (ownsPresentation && IsActivated) FinishRouteChange();
        RestorePresentation();
    }
}
