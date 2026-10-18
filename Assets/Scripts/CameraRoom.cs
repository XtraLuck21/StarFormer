using UnityEngine;

public enum RoomCameraMode
{
    // 把整个房间框进画面，镜头固定不动。
    FitRoom,

    // 镜头跟着玩家走，但不会越过房间边界。
    FollowInsideRoom
}

// 一个房间的范围。用 Collider 2D 的大小来定义房间有多大。
[RequireComponent(typeof(Collider2D))]
public class CameraRoom : MonoBehaviour
{
    [SerializeField] private RoomCameraMode mode = RoomCameraMode.FitRoom;

    // FitRoom 模式：在刚好框住房间的基础上再往外拉一点，留白。
    [SerializeField, Min(0f)] private float padding = 0.5f;

    // FollowInsideRoom 模式：镜头的固定大小（画面高度的一半，单位米）。
    [SerializeField, Min(1f)] private float followViewSize = 5f;

    [SerializeField] private Color gizmoColor = new Color(0.3f, 0.8f, 1f, 0.15f);

    private Collider2D area;
    private RoomCameraController controller;

    public RoomCameraMode Mode => mode;
    public float Padding => padding;
    public float FollowViewSize => followViewSize;
    public Bounds Area => area != null ? area.bounds : new Bounds(transform.position, Vector3.one);

    private void Awake()
    {
        area = GetComponent<Collider2D>();

        // 只感应，不挡路。
        area.isTrigger = true;

        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.enabled = false;
        }

        controller = FindAnyObjectByType<RoomCameraController>();

        if (controller == null)
        {
            Debug.LogError(
                "场景里找不到 RoomCameraController，请把它挂到 Main Camera 上。",
                this
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller == null)
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null ||
            !otherBody.TryGetComponent(out PossessableBody possessableBody) ||
            !possessableBody.IsControlled)
        {
            return;
        }

        controller.EnterRoom(this);
    }

    private void OnDrawGizmos()
    {
        Collider2D preview = GetComponent<Collider2D>();

        if (preview == null)
        {
            return;
        }

        Bounds bounds = preview.bounds;

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}