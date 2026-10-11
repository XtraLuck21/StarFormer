using UnityEngine;

[DefaultExecutionOrder(50)]
[RequireComponent(
    typeof(Rigidbody2D),
    typeof(Collider2D),
    typeof(PossessableBody))]
public class BouncyBallAbility : MonoBehaviour
{
    [Header("Bounce Rhythm")]
    [SerializeField, Min(0.25f)] private float defaultBounceHeight = 20.25f;
    [SerializeField, Min(0.05f)] private float defaultSecondsToApex = 2.4f;
    [SerializeField, Min(1.01f)] private float compressionTempoExponent = 1.1f;
    [SerializeField, Min(0.05f)] private float minimumSecondsToApex = 0.18f;
    [SerializeField, Min(0f)] private float ceilingStrikeDistance = 0.15f;

    [SerializeField, Min(0f)] private float waterLinearDamping = 3f;

    private const float MinimumBounceHeight = 0.25f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private PossessableBody possessable;

    private float activeBounceHeight;
    private float lastBounceOriginY;

    private float landGravityScale;
    private float landLinearDamping;
    private bool wasControlled;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        possessable = GetComponent<PossessableBody>();

        landGravityScale = body.gravityScale;
        landLinearDamping = body.linearDamping;
        activeBounceHeight = defaultBounceHeight;

        SetWaitingState();
    }

    private void FixedUpdate()
    {
        if (body == null ||
            bodyCollider == null ||
            possessable == null)
        {
            return;
        }

        if (!possessable.IsControlled)
        {
            if (wasControlled || body.bodyType != RigidbodyType2D.Kinematic)
            {
                SetWaitingState();
            }

            wasControlled = false;
            return;
        }

        if (!wasControlled)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.WakeUp();
        }

        wasControlled = true;

        if (WaterZone.TryGetSurfaceY(bodyCollider.bounds, out float surfaceY))
        {
            FloatAtSurface(surfaceY);
            return;
        }

        ApplyBounceGravity(activeBounceHeight);
        body.linearDamping = landLinearDamping;

        if (possessable.IsGrounded() && body.linearVelocity.y <= 0.1f)
        {
            activeBounceHeight = CalculateAvailableBounceHeight();
            lastBounceOriginY = body.position.y;
            ApplyBounceGravity(activeBounceHeight);

            Vector2 velocity = body.linearVelocity;
            velocity.y = CalculateLaunchSpeed(activeBounceHeight);
            body.linearVelocity = velocity;
        }
    }

    private float CalculateAvailableBounceHeight()
    {
        float castRadius = Mathf.Min(
            bodyCollider.bounds.extents.x,
            bodyCollider.bounds.extents.y
        );

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            body.position,
            castRadius,
            Vector2.up,
            defaultBounceHeight,
            Physics2D.GetLayerCollisionMask(gameObject.layer)
        );

        float availableHeight = defaultBounceHeight;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null ||
                hit.collider == bodyCollider ||
                hit.collider.isTrigger ||
                hit.normal.y > -0.5f)
            {
                continue;
            }

            availableHeight = Mathf.Min(
                availableHeight,
                hit.distance + ceilingStrikeDistance
            );
        }

        return Mathf.Max(MinimumBounceHeight, availableHeight);
    }

    private float CalculateSecondsToApex(float height)
    {
        float heightRatio = height / defaultBounceHeight;
        float secondsToApex = defaultSecondsToApex *
            Mathf.Pow(heightRatio, compressionTempoExponent);

        return Mathf.Max(minimumSecondsToApex, secondsToApex);
    }

    private float CalculateLaunchSpeed(float height)
    {
        float secondsToApex = CalculateSecondsToApex(height);
        return 2f * height / secondsToApex;
    }

    private void ApplyBounceGravity(float height)
    {
        float secondsToApex = CalculateSecondsToApex(height);
        float gravityAcceleration =
            2f * height / (secondsToApex * secondsToApex);
        float worldGravity = Mathf.Abs(Physics2D.gravity.y);

        body.gravityScale = worldGravity > 0.001f
            ? gravityAcceleration / worldGravity
            : landGravityScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (possessable == null ||
            !possessable.IsControlled ||
            possessable.IsInWater)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y >= -0.65f)
            {
                continue;
            }

            float compressedHeight = Mathf.Clamp(
                body.position.y - lastBounceOriginY,
                MinimumBounceHeight,
                defaultBounceHeight
            );

            activeBounceHeight = compressedHeight;
            ApplyBounceGravity(activeBounceHeight);

            Vector2 velocity = body.linearVelocity;
            velocity.y = -CalculateLaunchSpeed(activeBounceHeight);
            body.linearVelocity = velocity;
            break;
        }
    }

    private void FloatAtSurface(float surfaceY)
    {
        body.gravityScale = 0f;
        body.linearDamping = waterLinearDamping;

        Vector2 position = body.position;
        position.y = surfaceY + bodyCollider.bounds.extents.y;
        body.position = position;

        Vector2 velocity = body.linearVelocity;
        velocity.y = 0f;
        body.linearVelocity = velocity;
    }

    private void SetWaitingState()
    {
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        activeBounceHeight = defaultBounceHeight;
        body.gravityScale = landGravityScale;
        body.linearDamping = landLinearDamping;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.Sleep();
    }

    private void OnDisable()
    {
        if (body == null)
        {
            return;
        }

        body.gravityScale = landGravityScale;
        body.linearDamping = landLinearDamping;
    }
}
