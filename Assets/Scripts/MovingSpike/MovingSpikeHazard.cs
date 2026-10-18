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
    // Optional; automatically find the reference in Start when unassigned.
    [SerializeField] private PlayerPossessionController playerController;

    [Header("Movement")]
    // Patrol moves between two points; Straight moves continuously in one direction.
    [SerializeField] private SpikeMoveMode moveMode = SpikeMoveMode.Patrol;

    // Speed shared by both modes, in world units per second.
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    [Header("Patrol Mode")]
    // Offset from start: (4, 0) patrols four units right; (0, 3) patrols three units up.
    [SerializeField] private Vector2 patrolOffset = new Vector2(4f, 0f);

    // Pause at each endpoint in seconds; zero reverses immediately.
    [SerializeField, Min(0f)] private float waitTime = 0.5f;

    [SerializeField] private bool startMovingToEnd = true;

    [Header("Straight Mode")]
    // Enabled: move right continuously; disabled: move left.
    [SerializeField] private bool straightMovesRight = true;

    // Enabled: flip the sprite horizontally when reversing direction.
    [SerializeField] private bool flipSpriteOnTurn = false;

    [Header("Lifetime")]
    // Enabled: remove the spike when its lifetime expires; disabled: keep it indefinitely.
    [SerializeField] private bool useLifetime = false;

    // Lifetime in seconds, measured from level start.
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    // Enabled: play a shatter effect before removal; disabled: remove directly.
    [SerializeField] private bool shatterOnExpire = true;

    [Header("Death")]
    // Enabled: reload immediately after a lethal hit, retaining the active checkpoint.
    // Disabled: use a two-second death freeze before reloading, like ordinary spikes.
    [SerializeField] private bool restartImmediately = true;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private bool movingToEnd;
    private float waitTimer;

    private bool isConfigured;

    // Elapsed lifetime and a guard against entering destruction more than once.
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
                "Missing Collider2D. Add a Box Collider 2D or Polygon Collider 2D first.",
                this
            );

            return;
        }

        // Script-controlled position: ignore gravity and collision impulses.
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.simulated = true;

        // Clear any position constraints while keeping rotation locked.
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Allow trigger events with objects that have no rigidbody.
        body.useFullKinematicContacts = true;

        // Detect contact without blocking movement.
        bodyCollider.isTrigger = true;

        // Automatically find the player controller in the scene.
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerPossessionController>();
        }

        if (playerController == null)
        {
            Debug.LogError(
                "No PlayerPossessionController found in the scene. " +
                "Assign the Player Controller field in the Inspector.",
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

        // Lifetime expired: stop moving and destroy the spike.
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

        // Pause at an endpoint.
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

        // Reverse direction on reaching an endpoint.
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

    // On lifetime expiry, optionally play the shatter effect before removal.
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

    // Called when hit by a fired spike: play fragments, then destroy this object.
    public void Shatter()
    {
        // Destroy takes effect at frame end, so this method may be called again before removal.
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

    // -- Damage handling --

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
            // Reload automatically after a two-second freeze.
            playerController.KillControlledBody(
                possessableBody,
                AnalyticsDeathCause.MovingSpike
            );
        }
    }

    // Draw the patrol path in the Scene view for easier placement.
    private void OnDrawGizmosSelected()
    {
        Vector2 from = Application.isPlaying
            ? startPoint
            : (Vector2)transform.position;

        // In Straight mode, draw only a short line indicating direction.
        Vector2 to = moveMode == SpikeMoveMode.Straight
            ? from + (straightMovesRight ? Vector2.right : Vector2.left) * 2f
            : from + patrolOffset;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(from, 0.15f);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}
