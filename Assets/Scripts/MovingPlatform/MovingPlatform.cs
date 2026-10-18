using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Patrol")]
    // Offset from the start: (4, 0) travels four units horizontally; (0, 3) three vertically.
    [SerializeField] private Vector2 patrolOffset = new Vector2(4f, 0f);

    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    // Pause at each endpoint in seconds; zero reverses immediately.
    [SerializeField, Min(0f)] private float waitTime = 0.5f;

    [SerializeField] private bool startMovingToEnd = true;

    [Header("Riders")]
    // Carry bodies standing on the platform.
    [SerializeField] private bool carryRiders = true;

    // Minimum upward contact-normal alignment required to count as standing on top.
    [SerializeField, Range(0f, 1f)] private float riderNormalThreshold = 0.5f;

    // Cancel passengers' downward velocity while carrying them to reduce vertical jitter.
    [SerializeField] private bool dampRiderFalling = true;

    private Rigidbody2D body;
    private Collider2D bodyCollider;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private bool movingToEnd;
    private float waitTimer;

    private bool isConfigured;

    // Bodies standing on the platform this frame.
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
                "Missing Collider2D. Add a Box Collider 2D first.",
                this
            );

            return;
        }

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Match the player's interpolation settings for smooth motion.
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Use a solid collider so bodies can stand on the platform.
        bodyCollider.isTrigger = false;

        startPoint = body.position;
        endPoint = startPoint + patrolOffset;
        movingToEnd = startMovingToEnd;

        if (patrolOffset == Vector2.zero)
        {
            Debug.LogWarning(
                "Patrol Offset is (0, 0), so this platform will not move. " +
                "Set an offset in the Inspector, such as X = 4.",
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

        // Pause at an endpoint.
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            riders.Clear();
            return;
        }

        Vector2 current = body.position;
        Vector2 target = movingToEnd ? endPoint : startPoint;

        Vector2 nextPosition = Vector2.MoveTowards(
            current,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        Vector2 delta = nextPosition - current;

        // Assign position directly instead of using MovePosition,
        // so the platform and its passengers move together without a one-frame offset.
        body.position = nextPosition;

        if (carryRiders && delta != Vector2.zero)
        {
            foreach (Rigidbody2D rider in riders)
            {
                if (rider == null)
                {
                    continue;
                }

                rider.position += delta;

                // Cancel downward velocity accumulated while standing on the platform.
                if (dampRiderFalling)
                {
                    Vector2 riderVelocity = rider.linearVelocity;

                    if (riderVelocity.y < 0f)
                    {
                        riderVelocity.y = 0f;
                        rider.linearVelocity = riderVelocity;
                    }
                }
            }
        }

        // Rebuild the passenger list each frame so bodies that leave are removed.
        riders.Clear();

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
            // The normal points from the other collider toward the platform; from above it points down.
            if (contacts[i].normal.y < -riderNormalThreshold)
            {
                riders.Add(otherBody);
                return;
            }
        }
    }

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
