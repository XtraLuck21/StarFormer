using UnityEngine;

// 挂在 Main Camera 上。根据玩家所在的房间移动和缩放镜头。
[RequireComponent(typeof(Camera))]
public class RoomCameraController : MonoBehaviour
{
    [Header("Transition")]
    // 勾上：瞬间切换，像塞尔达那种硬切分镜。
    // 不勾：平滑推过去。
    [SerializeField] private bool snapInstantly = false;

    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.1f)] private float zoomSpeed = 6f;

    [Header("Start")]
    // 开局所在的房间，留空就用第一次触发到的房间。
    [SerializeField] private CameraRoom startRoom;

    [Header("References")]
    // 留空会自动在场景里找。
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
                "镜头不是 Orthographic（正交）模式，2D 游戏请在 Camera 上改成 Orthographic。",
                this
            );
        }
    }

    private void Start()
    {
        if (startRoom != null)
        {
            EnterRoom(startRoom);

            // 开局直接到位，不播放推镜。
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

        // 指数平滑：越接近目标越慢，和帧率无关。
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

    // 计算这一帧镜头应该在哪、多大。
    private void Recalculate()
    {
        Bounds area = currentRoom.Area;

        if (currentRoom.Mode == RoomCameraMode.FitRoom)
        {
            // 要同时装下房间的宽和高，取更大的那个需求。
            float sizeByHeight = area.extents.y;
            float sizeByWidth = area.extents.x / cam.aspect;

            targetSize = Mathf.Max(sizeByHeight, sizeByWidth) + currentRoom.Padding;
            targetPosition = area.center;

            return;
        }

        // FollowInsideRoom：固定大小，跟着玩家，但夹在房间里。
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

    // 把镜头中心夹在房间内；房间比画面还小时直接居中。
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

    // 只改 X 和 Y，Z 保持不变（2D 相机通常是 -10）。
    private void SetPosition(Vector2 position)
    {
        Vector3 next = transform.position;

        next.x = position.x;
        next.y = position.y;

        transform.position = next;
    }

    // 玩家随时可能附身到别的身体上，所以每帧问一次控制器。
    private void RefreshTarget()
    {
        PossessableBody body = player != null ? player.CurrentBody : null;

        target = body != null ? body.transform : null;
    }
}
