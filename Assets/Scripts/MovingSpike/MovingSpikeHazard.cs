using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingSpikeHazard : MonoBehaviour
{
    [Header("References")]
    // 留空也可以，Start 时会自动在场景里找。
    [SerializeField] private PlayerPossessionController playerController;

    [Header("Patrol")]
    // 相对起点的位移。(4, 0) = 向右来回 4 米；(0, 3) = 上下来回 3 米。
    [SerializeField] private Vector2 patrolOffset = new Vector2(4f, 0f);

    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    // 到达两端后停顿几秒，0 表示立刻折返。
    [SerializeField, Min(0f)] private float waitTime = 0.5f;

    [SerializeField] private bool startMovingToEnd = true;

    // 勾上后，折返时左右翻转贴图。
    [SerializeField] private bool flipSpriteOnTurn = false;

    [Header("Death")]
    // 勾上：碰到立刻重开关卡，等同于按 R。
    // 不勾：画面定格 2 秒后再重开，与普通尖刺一致。
    [SerializeField] private bool restartImmediately = true;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private bool movingToEnd;
    private float waitTimer;

    private bool isConfigured;

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyCollider == null)
        {
            Debug.LogError(
                "缺少 Collider2D。请先添加 Box Collider 2D 或 Polygon Collider 2D。",
                this
            );

            return;
        }

        // 位置完全由脚本控制：不受重力，也不会被撞飞。
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.simulated = true;

        // 清掉可能被手动勾上的位置锁定，只保留旋转锁定。
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 让没有刚体的物体也能触发事件。
        body.useFullKinematicContacts = true;

        // 只检测接触，不阻挡任何东西。
        bodyCollider.isTrigger = true;

        // 自动查找场景里的玩家控制器。
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerPossessionController>();
        }

        if (playerController == null)
        {
            Debug.LogError(
                "场景里找不到 PlayerPossessionController，" +
                "请手动拖进 Inspector 的 Player Controller 栏。",
                this
            );
        }

        startPoint = body.position;
        endPoint = startPoint + patrolOffset;
        movingToEnd = startMovingToEnd;

        if (patrolOffset == Vector2.zero)
        {
            Debug.LogWarning(
                "Patrol Offset 是 (0,0)，这个刺不会移动。" +
                "请在 Inspector 里填一个位移，例如 X = 4。",
                this
            );
        }

        isConfigured = true;
        ApplyFacing();
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
            return;
        }

        Vector2 target = movingToEnd ? endPoint : startPoint;

        Vector2 nextPosition = Vector2.MoveTowards(
            body.position,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        body.MovePosition(nextPosition);

        // 到达端点就换方向。
        if (Vector2.Distance(nextPosition, target) < 0.001f)
        {
            movingToEnd = !movingToEnd;
            waitTimer = waitTime;

            ApplyFacing();
        }
    }

    private void ApplyFacing()
    {
        if (!flipSpriteOnTurn || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = !movingToEnd;
    }

    // ── 杀伤逻辑 ──

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckContact(other);
    }

    private void CheckContact(Collider2D other)
    {
        if (!isActiveAndEnabled || playerController == null)
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null)
        {
            return;
        }

        if (!otherBody.TryGetComponent(
                out PossessableBody possessableBody) ||
            !possessableBody.IsControlled)
        {
            return;
        }

        if (restartImmediately)
        {
            // 等同于玩家按下 R。
            playerController.RequestRestart();
        }
        else
        {
            // 定格两秒后自动重开。
            playerController.KillControlledBody(possessableBody);
        }
    }

    // 在 Scene 窗口里画出巡逻路线，方便摆位置。
    private void OnDrawGizmosSelected()
    {
        Vector2 from = Application.isPlaying
            ? startPoint
            : (Vector2)transform.position;

        Vector2 to = from + patrolOffset;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(from, 0.15f);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}