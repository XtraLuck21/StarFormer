using UnityEngine;

// Attach to Main Camera to position and zoom it based on the player's room.
[RequireComponent(typeof(Camera))]
public class RoomCameraController : MonoBehaviour
{
    [Header("Transition")]
    // Enabled: switch instantly with a hard camera cut.
    // Disabled: transition smoothly.
    [SerializeField] private bool snapInstantly = false;

    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.1f)] private float zoomSpeed = 6f;

    [Header("Start")]
    // Starting room; leave empty to use the first room entered.
    [SerializeField] private CameraRoom startRoom;

    [Header("References")]
    // Automatically find the reference in the scene when unassigned.
    [SerializeField] private PlayerPossessionController player;

    private Camera cam;
    private CameraRoom currentRoom;
    private Transform target;

    private float targetSize;
    private Vector2 targetPosition;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }

        if (!cam.orthographic)
        {
            Debug.LogWarning(
                "The camera is not orthographic. Set Camera Projection to Orthographic for this 2D game.",
                this
            );
        }
    }

    private void Start()
    {
        if (startRoom != null)
        {
            EnterRoom(startRoom);

            // Position the camera immediately at startup without a transition.
            ApplyImmediately();
        }
    }

    public void EnterRoom(CameraRoom room)
    {
        if (room == null || room == currentRoom)
        {
            return;
        }

        currentRoom = room;
    }

    private void LateUpdate()
    {
        if (currentRoom == null)
        {
            return;
        }

        RefreshTarget();
        Recalculate();

        if (snapInstantly)
        {
            ApplyImmediately();
            return;
        }

        // Frame-rate-independent exponential smoothing slows as the target approaches.
        float moveT = 1f - Mathf.Exp(-moveSpeed * Time.deltaTime);
        float zoomT = 1f - Mathf.Exp(-zoomSpeed * Time.deltaTime);

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            zoomT
        );

        Vector2 next = Vector2.Lerp(
            transform.position,
            targetPosition,
            moveT
        );

        SetPosition(next);
    }

    // Calculate this frame's target camera position and size.
    private void Recalculate()
    {
        Bounds area = currentRoom.Area;

        if (currentRoom.Mode == RoomCameraMode.FitRoom)
        {
            // Use the larger size required to fit both the room's width and height.
            float sizeByHeight = area.extents.y;
            float sizeByWidth = area.extents.x / cam.aspect;

            targetSize = Mathf.Max(sizeByHeight, sizeByWidth) + currentRoom.Padding;
            targetPosition = area.center;

            return;
        }

        // FollowInsideRoom: follow at a fixed size, clamped to the room bounds.
        targetSize = currentRoom.FollowViewSize;

        Vector2 desired = target != null
            ? (Vector2)target.position
            : (Vector2)area.center;

        float halfHeight = targetSize;
        float halfWidth = targetSize * cam.aspect;

        targetPosition = new Vector2(
            ClampAxis(desired.x, area.min.x, area.max.x, halfWidth),
            ClampAxis(desired.y, area.min.y, area.max.y, halfHeight)
        );
    }

    // Clamp the camera center to the room; center it if the view exceeds the room.
    private static float ClampAxis(float value, float min, float max, float halfView)
    {
        float low = min + halfView;
        float high = max - halfView;

        if (low > high)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(value, low, high);
    }

    private void ApplyImmediately()
    {
        Recalculate();

        cam.orthographicSize = targetSize;
        SetPosition(targetPosition);
    }

    // Update only X and Y; preserve Z (typically -10 for a 2D camera).
    private void SetPosition(Vector2 position)
    {
        Vector3 next = transform.position;

        next.x = position.x;
        next.y = position.y;

        transform.position = next;
    }

    // Query the controller each frame because possession can change the target body.
    private void RefreshTarget()
    {
        PossessableBody body = player != null ? player.CurrentBody : null;

        target = body != null ? body.transform : null;
    }
}
