using UnityEngine;

public enum RoomCameraMode
{
    // Frame the entire room with a stationary camera.
    FitRoom,

    // Follow the player without moving beyond the room boundaries.
    FollowInsideRoom
}

// Define a room's bounds using the size of its Collider2D.
[RequireComponent(typeof(Collider2D))]
public class CameraRoom : MonoBehaviour
{
    [SerializeField] private RoomCameraMode mode = RoomCameraMode.FitRoom;

    // FitRoom: add padding beyond the size needed to frame the room.
    [SerializeField, Min(0f)] private float padding = 0.5f;

    // FollowInsideRoom: fixed orthographic size (half the view height in world units).
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

        // Detect entry without blocking movement.
        area.isTrigger = true;

        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.enabled = false;
        }

        controller = FindAnyObjectByType<RoomCameraController>();

        if (controller == null)
        {
            Debug.LogError(
                "No RoomCameraController found in the scene. Add it to Main Camera.",
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