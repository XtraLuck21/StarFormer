using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpikeWallEnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPossessionController player;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;

    [Header("Death")]
    [SerializeField] private bool restartImmediately = true;

    private Rigidbody2D body;
    private bool movementStarted;
    private bool deathRequested;
    private float pauseTimer;
    private float fixedY;

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
        if (deathRequested || !movementStarted)
        {
            return;
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

    // 镜头分镜结束后，通过事件调用。
    public void StartChase()
    {
        if (deathRequested)
        {
            return;
        }

        movementStarted = true;
        Debug.Log("刺墙开始追逐！", this);
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
        if (deathRequested || player == null)
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