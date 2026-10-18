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
    // Enabled: start every child SpikeLifetime countdown when the chase begins.
    [SerializeField] private bool startSpikeLifetimeOnChase = false;

    // Zero uses each SpikeLifetime component's configured duration.
    // A positive value overrides all spike durations.
    [SerializeField, Min(0f)] private float spikeLifetimeOverride = 0f;

    [Header("Chase End")]
    // Enabled: stop the wall and notify listeners once all spikes are gone.
    [SerializeField] private bool endChaseWhenSpikesGone = true;

    // Additional delay after the last spike disappears before ending the chase.
    [SerializeField, Min(0f)] private float endChaseDelay = 0.3f;

    // Connect level2Camera.BeginRemainingFollow or FocusOnTarget here.
    [SerializeField] private UnityEvent onChaseEnded = new UnityEvent();

    [Header("Death")]
    [SerializeField] private bool restartImmediately = true;

    private Rigidbody2D body;
    private bool movementStarted;
    private bool deathRequested;
    private float pauseTimer;
    private float fixedY;

    // Track these spikes at chase start to determine when the chase should end.
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

        // Child spike colliders use the parent object's Rigidbody2D.
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

    // Once all spikes are gone, begin the final delay.
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
                // Unity treats destroyed object references as null.
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

    // Called by an event after the camera cutscene ends.
    public void StartChase()
    {
        if (deathRequested || chaseEnded)
        {
            return;
        }

        movementStarted = true;

        BeginSpikeLifetime();

        Debug.Log("Spike wall chase started!", this);
    }

    // Start the countdown on every child spike now.
    private void BeginSpikeLifetime()
    {
        // Always track the spikes, even when automatic lifetime start is disabled.
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

        Debug.Log($"Started lifetime countdowns for {trackedSpikes.Length} spikes.", this);
    }

    // End the chase: stop the wall and notify listeners to restore the camera.
    public void EndChase()
    {
        if (chaseEnded)
        {
            return;
        }

        chaseEnded = true;

        StopChase();
        onChaseEnded.Invoke();

        Debug.Log("Chase ended; camera control released.", this);
    }

    public void StopChase()
    {
        movementStarted = false;
        body.linearVelocity = Vector2.zero;
    }

    // Later collapsing blocks can call this to pause the spike wall.
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

        // Handle a player hit only once even if multiple spikes touch simultaneously.
        deathRequested = true;

        if (restartImmediately)
        {
            player.KillControlledBody(
                possessableBody,
                AnalyticsDeathCause.SpikeWall,
                0f
            );
        }
        else
        {
            player.KillControlledBody(
                possessableBody,
                AnalyticsDeathCause.SpikeWall
            );
        }
    }
}
