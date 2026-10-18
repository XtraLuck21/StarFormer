using UnityEngine;

// Sliding door controlled by switches such as SpikeButton via SetOpen().
public class TriggerDoor : MonoBehaviour
{
    [Header("Movement")]
    // Open-position offset relative to the closed position.
    // (0, 3) raises the door three units; (-2, 0) moves it two units left.
    [SerializeField] private Vector2 openOffset = new Vector2(0f, 3f);

    [SerializeField, Min(0.01f)] private float moveSpeed = 4f;

    [Header("State")]
    [SerializeField] private bool startOpen = false;

    // Enabled: disable the collider as soon as the door opens, for disappearing doors.
    // Sliding doors can leave this disabled because the door moves out of the way.
    [SerializeField] private bool disableColliderWhenOpen = false;

    private Rigidbody2D body;
    private Collider2D doorCollider;

    private Vector2 closedPosition;
    private Vector2 openPosition;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        doorCollider = GetComponent<Collider2D>();

        if (body != null)
        {
            // Script-controlled position; ignore gravity and collision impulses.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;

        isOpen = startOpen;

        // Apply the initial position immediately without animation.
        transform.position = isOpen ? openPosition : closedPosition;

        ApplyCollider();
    }

    public void SetOpen(bool open)
    {
        if (open == isOpen)
        {
            return;
        }

        isOpen = open;
        ApplyCollider();
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    private void FixedUpdate()
    {
        Vector2 target = isOpen ? openPosition : closedPosition;
        Vector2 current = body != null ? body.position : (Vector2)transform.position;

        if (current == target)
        {
            return;
        }

        Vector2 next = Vector2.MoveTowards(
            current,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        if (body != null)
        {
            body.MovePosition(next);
        }
        else
        {
            transform.position = next;
        }
    }

    private void ApplyCollider()
    {
        if (disableColliderWhenOpen && doorCollider != null)
        {
            doorCollider.enabled = !isOpen;
        }
    }

    // Draw the open door position in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Vector2 from = Application.isPlaying
            ? closedPosition
            : (Vector2)transform.position;

        Vector2 to = from + openOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireCube(to, transform.localScale);
    }
}