using UnityEngine;

/// <summary>Expands the fixed room view when an Obstacle enters the authored triangle trigger.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class FanSeesawCameraReveal : MonoBehaviour
{
    [SerializeField] private CameraFocusIntro intro;
    [SerializeField] private Transform seesawRegion;
    [SerializeField] private Vector2 viewOffset = new Vector2(3.8f, -2.8f);
    [SerializeField, Min(0.1f)] private float revealedOrthographicSize = 6.5f;
    [SerializeField, Min(0.05f)] private float transitionDuration = 1.2f;

    public bool HasRevealed { get; private set; }

    private Camera attachedCamera;
    private Vector3 roomPosition;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float startSize;
    private float targetSize;
    private float elapsed;
    private bool transitioning;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
        // Capture before the intro's Start temporarily borrows the camera.
        roomPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (intro != null && intro.enabled) return;
        if (!transitioning) return;

        elapsed += Time.deltaTime;
        float duration = Mathf.Max(0.05f, transitionDuration);
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
        transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        attachedCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
        if (elapsed >= duration) transitioning = false;
    }

    public void RevealFromTrigger()
    {
        // Ignore intro-time contacts instead of queuing a pan for the moment the camera returns.
        if (!isActiveAndEnabled || HasRevealed || (intro != null && intro.enabled)) return;
        BeginReveal();
    }

    private void BeginReveal()
    {
        HasRevealed = true;
        transitioning = true;
        elapsed = 0f;
        startPosition = transform.position;
        startSize = attachedCamera.orthographicSize;
        targetPosition = roomPosition + new Vector3(viewOffset.x, viewOffset.y, 0f);
        targetSize = Mathf.Max(startSize, revealedOrthographicSize);
        if (seesawRegion == null) return;

        float aspect = Mathf.Max(0.1f, attachedCamera.aspect);
        foreach (SpriteRenderer renderer in seesawRegion.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Bounds bounds = renderer.bounds;
            float width = Mathf.Max(Mathf.Abs(bounds.min.x - targetPosition.x), Mathf.Abs(bounds.max.x - targetPosition.x));
            float height = Mathf.Max(Mathf.Abs(bounds.min.y - targetPosition.y), Mathf.Abs(bounds.max.y - targetPosition.y));
            targetSize = Mathf.Max(targetSize, (width + 0.5f) / aspect, height + 0.5f);
        }
    }
}
