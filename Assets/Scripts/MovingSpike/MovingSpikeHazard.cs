using UnityEngine;

public enum SpikeMoveMode
{
    Patrol,
    Straight
}

[RequireComponent(typeof(Rigidbody2D))]
public class MovingSpikeHazard : MonoBehaviour
{
    [Header("References")]
    // 留空也可以，Start 时会自动在场景里找。
    [SerializeField] private PlayerPossessionController playerController;

    [Header("Movement")]
    // Patrol = 在两点之间来回；Straight = 朝一个方向一直走，不折返。
    [SerializeField] private SpikeMoveMode moveMode = SpikeMoveMode.Patrol;

    // 两种模式共用的速度，单位是米/秒。
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    [Header("Patrol Mode")]
    // 相对起点的位移。(4, 0) = 向右来回 4 米；(0, 3) = 上下来回 3 米。
    [SerializeField] private Vector2 patrolOffset = new Vector2(4f, 0f);

    // 到达两端后停顿几秒，0 表示立刻折返。
    [SerializeField, Min(0f)] private float waitTime = 0.5f;

    [SerializeField] private bool startMovingToEnd = true;

    [Header("Straight Mode")]
    // 勾上 = 一直往右走，取消勾选 = 一直往左走。
    [SerializeField] private bool straightMovesRight = true;

    // 勾上后，折返时左右翻转贴图。
    [SerializeField] private bool flipSpriteOnTurn = false;

    [Header("Lifetime")]
    // 勾上后，尖刺存活到时间会自动消失；不勾则永久存在。
    [SerializeField] private bool useLifetime = false;

    // 存活秒数，从关卡开始计时。
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    // 勾上：到时间播碎片特效再消失；不勾：直接消失。
    [SerializeField] private bool shatterOnExpire = true;

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

    // 已经存活的秒数，以及「已经进入销毁流程」的保险。
    private float age;
    private bool isExpired;

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

        age = 0f;

        isConfigured = true;
        ApplyFacing();
    }

    private void FixedUpdate()
    {
        if (!isConfigured || isExpired)
        {
            return;
        }

        // 存活计时：到点就停止移动并销毁。
        if (useLifetime)
        {
            age += Time.fixedDeltaTime;

            if (age >= lifetime)
            {
                Expire();
                return;
            }
        }

        if (moveMode == SpikeMoveMode.Straight)
        {
            Vector2 step = (straightMovesRight ? Vector2.right : Vector2.left)
                * (moveSpeed * Time.fixedDeltaTime);

            body.MovePosition(body.position + step);
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

        spriteRenderer.flipX = moveMode == SpikeMoveMode.Straight
            ? !straightMovesRight
            : !movingToEnd;
    }

    // 存活时间到了：按设置决定是否播碎片。
    private void Expire()
    {
        if (isExpired)
        {
            return;
        }

        if (shatterOnExpire)
        {
            Shatter();
            return;
        }

        isExpired = true;
        Destroy(gameObject);
    }

    // 被射出的刺打中时调用：先掉碎片，再销毁自己。
    public void Shatter()
    {
        // Destroy 要等这一帧结束才真正生效，期间可能被再次调用。
        if (isExpired)
        {
            return;
        }

        isExpired = true;

        if (TryGetComponent(out ShatterEffect effect))
        {
            effect.Play();
        }

        Destroy(gameObject);
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
        if (!isActiveAndEnabled || isExpired || playerController == null)
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
            // Record the death before reloading; zero delay preserves the
            // immediate-restart behavior without bypassing Analytics.
            playerController.KillControlledBody(
                possessableBody,
                AnalyticsDeathCause.MovingSpike,
                0f
            );
        }
        else
        {
            // 定格两秒后自动重开。
            playerController.KillControlledBody(
                possessableBody,
                AnalyticsDeathCause.MovingSpike
            );
        }
    }

    // 在 Scene 窗口里画出巡逻路线，方便摆位置。
    private void OnDrawGizmosSelected()
    {
        Vector2 from = Application.isPlaying
            ? startPoint
            : (Vector2)transform.position;

        // Straight 模式只画一条表示方向的短线。
        Vector2 to = moveMode == SpikeMoveMode.Straight
            ? from + (straightMovesRight ? Vector2.right : Vector2.left) * 2f
            : from + patrolOffset;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(from, 0.15f);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}
