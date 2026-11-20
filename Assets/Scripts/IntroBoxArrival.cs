using UnityEngine;

// Scene-local arrival: match the outgoing box and floor framing, then reveal
// the Bubble route without stopping input or physics. The snapshot is one-shot.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public sealed class IntroBoxArrival : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody entryBox;
    [SerializeField] private Collider2D entryFloor;
    [SerializeField] private float spawnLeftEdge = -9.6f;
    [SerializeField] private float viewLeftEdge = -10.25f;
    [SerializeField] private float directEntryWidth = 0.45f;
    [SerializeField] private float directEntryCameraSize = 5.3f;
    [SerializeField] private float directEntryFloorViewport = 0.3566f;
    [SerializeField] private float revealSeconds = 1.5f;

    private static string pendingScene;
    private static float pendingWidth, pendingCameraSize, pendingFloorViewport;
    private Camera view;
    private Vector3 overviewPosition, arrivalPosition;
    private float overviewSize, arrivalSize, elapsed;
    private bool arriving, revealing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetArrival() => pendingScene = null;

    public static void RecordDeparture(PossessableBody box, string destination)
    {
        pendingScene = null;
        Camera source = Camera.main;
        if (box == null || box.Form != BodyForm.Box || source == null || !source.orthographic)
            return;
        Bounds bounds = box.BodyCollider.bounds;
        pendingScene = destination;
        pendingWidth = bounds.size.x;
        pendingCameraSize = source.orthographicSize;
        pendingFloorViewport = source.WorldToViewportPoint(
            new Vector3(bounds.center.x, bounds.min.y, 0f)).y;
    }

    private void Awake()
    {
        view = GetComponent<Camera>();
        overviewPosition = transform.position;
        overviewSize = view.orthographicSize;
    }

    private void Start()
    {
        bool inherited = pendingScene == gameObject.scene.name;
        float width = inherited ? pendingWidth : directEntryWidth;
        arrivalSize = inherited ? pendingCameraSize : directEntryCameraSize;
        float floorViewport = inherited ? pendingFloorViewport : directEntryFloorViewport;
        pendingScene = null;
        // Return travel from IntroBouncyBall starts near the flag in the full view.
        if (player == null || entryBox == null || entryFloor == null ||
            player.CurrentBody != entryBox || !entryBox.TryGetComponent(out BoxResizeAbility resize))
            return;

        resize.RestoreSceneEntryWidth(width);
        Bounds bounds = entryBox.BodyCollider.bounds;
        Vector2 shift = new Vector2(spawnLeftEdge - bounds.min.x,
            entryFloor.bounds.max.y - bounds.min.y);
        Vector2 position = entryBox.Body.position + shift;
        entryBox.transform.position = new Vector3(position.x, position.y, entryBox.transform.position.z);
        entryBox.Body.position = position;
        entryBox.Body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        arrivalPosition = new Vector3(viewLeftEdge + arrivalSize * view.aspect,
            entryFloor.bounds.max.y - (floorViewport - 0.5f) * 2f * arrivalSize,
            overviewPosition.z);
        transform.position = arrivalPosition;
        view.orthographicSize = arrivalSize;
        arriving = true;
    }

    private void LateUpdate()
    {
        if (!arriving || player == null) return;
        if (player.CurrentBody != entryBox) revealing = true;
        if (!revealing) return;
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.1f, revealSeconds)));
        transform.position = Vector3.Lerp(arrivalPosition, overviewPosition, t);
        view.orthographicSize = Mathf.Lerp(arrivalSize, overviewSize, t);
        if (t >= 1f) arriving = false;
    }
}
