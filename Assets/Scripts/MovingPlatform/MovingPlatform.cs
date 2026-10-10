using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Patrol")]
    // 相对起点的位移。(4, 0) = 左右来回 4 米；(0, 3) = 上下来回 3 米。
    [SerializeField] private Vector2 patrolOffset = new Vector2(4f, 0f);

    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    // 到达两端后停顿几秒，0 表示立刻折返。
    [SerializeField, Min(0f)] private float waitTime = 0.5f;

    [SerializeField] private bool startMovingToEnd = true;

    [Header("Riders")]
    // 站在平台上的物体会跟着一起移动。
    [SerializeField] private bool carryRiders = true;

    // 接触面法线至少要多向上，才算"站在平台上"。
    // 0.5 约等于 60 度，侧面贴着的物体不会被带走。
    [SerializeField, Range(0f, 1f)] private float riderNormalThreshold = 0.5f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private bool movingToEnd;
    private float waitTimer;

    private Vector2 lastPosition;
    private bool isConfigured;

    // 本帧站在平台上的物体。
    private readonly HashSet<Rigidbody2D> riders =
        new HashSet<Rigidbody2D>();

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider == null)
        {
            Debug.LogError(
                "缺少 Collider2D。请先添加 Box Collider 2D。",
                this
            );

            return;
        }

        // 位置由脚本控制，不受重力，也不会被踩下去。
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 实心碰撞体才能站上去。
        bodyCollider.isTrigger = false;

        startPoint = body.position;
        endPoint = startPoint + patrolOffset;
        movingToEnd = startMovingToEnd;
        lastPosition = startPoint;

        if (patrolOffset == Vector2.zero)
        {
            Debug.LogWarning(
                "Patrol Offset 是 (0,0)，这个平台不会移动。" +
                "请在 Inspector 里填一个位移，例如 X = 4。",
                this
            );
        }

        isConfigured = true;
    }

    private void FixedUpdate()
    {
        if (!isConfigured)
        {
            return;
        }

        // 在端点停顿。
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            riders.Clear();
            lastPosition = body.position;
            return;
        }

        Vector2 target = movingToEnd ? endPoint : startPoint;

        Vector2 nextPosition = Vector2.MoveTowards(
            body.position,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        Vector2 delta = nextPosition - lastPosition;

        body.MovePosition(nextPosition);

        // 把这一帧的位移同样加到乘客身上。
        if (carryRiders && delta != Vector2.zero)
        {
            foreach (Rigidbody2D rider in riders)
            {
                if (rider != null)
                {
                    rider.position += delta;
                }
            }
        }

        // 乘客名单每帧重建，离开平台的会自动移除。
        riders.Clear();
        lastPosition = nextPosition;

        // 到达端点就换方向。
        if (Vector2.Distance(nextPosition, target) < 0.001f)
        {
            movingToEnd = !movingToEnd;
            waitTimer = waitTime;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!carryRiders)
        {
            return;
        }

        Rigidbody2D otherBody = collision.rigidbody;

        if (otherBody == null ||
            otherBody.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        int count = collision.GetContacts(contacts);

        for (int i = 0; i < count; i++)
        {
            // 法线从对方指向平台。对方在上面时，法线朝下。
            if (contacts[i].normal.y < -riderNormalThreshold)
            {
                riders.Add(otherBody);
                return;
            }
        }
    }

    // 在 Scene 窗口里画出巡逻路线。
    private void OnDrawGizmosSelected()
    {
        Vector2 from = Application.isPlaying
            ? startPoint
            : (Vector2)transform.position;

        Vector2 to = from + patrolOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(from, 0.15f);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}
