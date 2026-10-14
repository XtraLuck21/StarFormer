using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class SpikeWallEnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPossessionController player;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;

    [Header("Spike Lifetime")]
    // 勾上：追逐开始的那一刻，墙上所有 SpikeLifetime 开始倒计时。
    [SerializeField] private bool startSpikeLifetimeOnChase = false;

    // 留 0 = 用每个 SpikeLifetime 自己填的秒数。
    // 大于 0 = 统一覆盖成这个秒数。
    [SerializeField, Min(0f)] private float spikeLifetimeOverride = 0f;

    [Header("Chase End")]
    // 勾上：所有刺都消失后，墙停下并通知外部（镜头在这里转回去）。
    [SerializeField] private bool endChaseWhenSpikesGone = true;

    // 刺全没了之后，再过几秒才真正结束，留一点缓冲。
    [SerializeField, Min(0f)] private float endChaseDelay = 0.3f;

    // 把 level2Camera 的 BeginRemainingFollow 或 FocusOnTarget 连到这里。
    [SerializeField] private UnityEvent onChaseEnded = new UnityEvent();

    [Header("Death")]
    [SerializeField] private bool restartImmediately = true;

    private Rigidbody2D body;
    private bool movementStarted;
    private bool deathRequested;
    private float pauseTimer;
    private float fixedY;

    // 追逐开始时记下这些刺，之后靠它们判断何时收尾。
    private SpikeLifetime[] trackedSpikes;
    private bool chaseEnded;
    private float endTimer;
    private bool spikesGone;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
        body.constraints =
            RigidbodyConstraints2D.FreezeRotation |
            RigidbodyConstraints2D.FreezePositionY;

        fixedY = body.position.y;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }

        // 子尖刺的 Collider 使用父对象的 Rigidbody2D。
        foreach (Collider2D spikeCollider
                 in GetComponentsInChildren<Collider2D>())
        {
            spikeCollider.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        if (deathRequested || !movementStarted || chaseEnded)
        {
            return;
        }

        if (endChaseWhenSpikesGone)
        {
            CheckSpikesGone();

            if (chaseEnded)
            {
                return;
            }
        }

        if (pauseTimer > 0f)
        {
            pauseTimer = Mathf.Max(
                0f,
                pauseTimer - Time.fixedDeltaTime
            );
            return;
        }

        Vector2 nextPosition = new Vector2(
            body.position.x + moveSpeed * Time.fixedDeltaTime,
            fixedY
        );

        body.MovePosition(nextPosition);
    }

    // 刺是不是都没了；没了就开始数缓冲时间。
    private void CheckSpikesGone()
    {
        if (trackedSpikes == null || trackedSpikes.Length == 0)
        {
            return;
        }

        if (!spikesGone)
        {
            foreach (SpikeLifetime spike in trackedSpikes)
            {
                // 物体被销毁后，这里的引用会变成 null。
                if (spike != null && !spike.IsExpired)
                {
                    return;
                }
            }

            spikesGone = true;
            endTimer = 0f;
        }

        endTimer += Time.fixedDeltaTime;

        if (endTimer >= endChaseDelay)
        {
            EndChase();
        }
    }

    // 镜头分镜结束后，通过事件调用。
    public void StartChase()
    {
        if (deathRequested || chaseEnded)
        {
            return;
        }

        movementStarted = true;

        BeginSpikeLifetime();

        Debug.Log("刺墙开始追逐！", this);
    }

    // 让墙上所有子尖刺从这一刻开始倒计时。
    private void BeginSpikeLifetime()
    {
        // 不管勾没勾，都先记下有哪些刺，收尾判断要用。
        trackedSpikes = GetComponentsInChildren<SpikeLifetime>();

        if (!startSpikeLifetimeOnChase)
        {
            return;
        }

        foreach (SpikeLifetime timer in trackedSpikes)
        {
            if (spikeLifetimeOverride > 0f)
            {
                timer.Begin(spikeLifetimeOverride);
            }
            else
            {
                timer.Begin();
            }
        }

        Debug.Log($"{trackedSpikes.Length} 根尖刺开始倒计时。", this);
    }

    // 追逐收尾：墙停下，通知外部把镜头转回去。
    public void EndChase()
    {
        if (chaseEnded)
        {
            return;
        }

        chaseEnded = true;

        StopChase();
        onChaseEnded.Invoke();

        Debug.Log("追逐结束，镜头交还。", this);
    }

    public void StopChase()
    {
        movementStarted = false;
        body.linearVelocity = Vector2.zero;
    }

    // 后续方块被碾碎时，可以调用此方法暂停刺墙。
    public void PauseMovement(float duration)
    {
        pauseTimer = Mathf.Max(pauseTimer, duration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayerContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckPlayerContact(other);
    }

    private void CheckPlayerContact(Collider2D other)
    {
        if (deathRequested || chaseEnded || player == null)
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

        // 多个尖刺同时碰到玩家，也只处理一次。
        deathRequested = true;

        if (restartImmediately)
        {
            player.RequestRestart();
        }
        else
        {
            player.KillControlledBody(possessableBody);
        }
    }
}