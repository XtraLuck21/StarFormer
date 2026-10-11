using UnityEngine;

// 一扇会滑开的门。由 SpikeButton 之类的开关调用 SetOpen()。
public class TriggerDoor : MonoBehaviour
{
    [Header("Movement")]
    // 打开时相对关闭位置的位移。
    // (0, 3) = 向上升 3 米；(-2, 0) = 向左退 2 米。
    [SerializeField] private Vector2 openOffset = new Vector2(0f, 3f);

    [SerializeField, Min(0.01f)] private float moveSpeed = 4f;

    [Header("State")]
    [SerializeField] private bool startOpen = false;

    // 勾上：门一开就关掉碰撞体，适合「原地消失」式的门。
    // 滑动式的门不需要勾，门体自己让开了。
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
            // 由脚本控制位置，不受重力也不被撞飞。
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;

        isOpen = startOpen;

        // 开局直接摆到位，不播放动画。
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

    // 在 Scene 窗口里画出门打开后的位置。
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