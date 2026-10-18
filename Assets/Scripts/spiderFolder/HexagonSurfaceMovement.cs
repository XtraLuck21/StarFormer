using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PossessableBody))]
[RequireComponent(typeof(CircleCollider2D))]
public class HexagonSurfaceMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visual;
    [SerializeField] private LayerMask surfaceMask;

    [Header("Attachment")]
    [SerializeField, Min(0.01f)] private float searchDistance = 0.15f;
    [SerializeField, Min(0f)] private float surfaceGap = 0.02f;
    [SerializeField, Min(0f)] private float correctionSpeed = 10f;

    private Rigidbody2D body;
    private PossessableBody possessable;
    private CircleCollider2D circle;

    private float airGravity;
    private Vector2 surfaceNormal = Vector2.up;

    [Header("Rolling Animation")]
    [SerializeField, Min(1f)]
    private float settleDegreesPerSecond = 240f;

    // Flat-top hexagon: local distance from the center to the left/right vertex.
    // Use 0.5 for a sprite with width 1.
    [SerializeField, Min(0.01f)]
    private float visualRadius = 0.5f;

    [SerializeField, Min(0f)]
    private float visualSurfaceGap = 0.005f;

    private float rollAngle;
    private Vector3 originalVisualLocalPosition;
    private Quaternion originalVisualLocalRotation;
    public bool IsAttached { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        possessable = GetComponent<PossessableBody>();
        circle = GetComponent<CircleCollider2D>();

        airGravity = body.gravityScale;
        if (visual != null)
        {
            originalVisualLocalPosition = visual.localPosition;
            originalVisualLocalRotation = visual.localRotation;
        }
    }

    // Called by PlayerPossessionController once per physics step.
    public void Tick(float input)
    {
        float radius = circle.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y)
        );

        Vector2 center = transform.TransformPoint(circle.offset);

        if (!FindSurface(center, radius, input, out RaycastHit2D hit))
        {
            IsAttached = false;
            surfaceNormal = Vector2.up;
            body.gravityScale = airGravity;

            Vector2 velocity = body.linearVelocity;
            velocity.x = input * possessable.MoveSpeed;
            body.linearVelocity = velocity;

            ResetRollingVisual();
            return;
        }
        possessable.RequestSurfacePossession(hit.collider);

        surfaceNormal = hit.normal.normalized;

        Vector2 tangent = new Vector2(
            surfaceNormal.y,
            -surfaceNormal.x
        );
        IsAttached = true;
        body.gravityScale = 0f;

        float dt = Time.fixedDeltaTime;

        float worldVisualRadius = visual != null
            ? visualRadius * Mathf.Abs(visual.lossyScale.x)
            : radius;

        worldVisualRadius = Mathf.Max(worldVisualRadius, 0.01f);

        float previousAngle = rollAngle;

        if (Mathf.Abs(input) > 0.01f)
        {
            rollAngle +=
                input * possessable.MoveSpeed *
                dt / worldVisualRadius * 60f;
        }
        else
        {
            float targetAngle =
                Mathf.Round(rollAngle / 60f) * 60f;

            rollAngle = Mathf.MoveTowards(
                rollAngle,
                targetAngle,
                settleDegreesPerSecond * dt
            );
        }

        float moveVelocity =
            (rollAngle - previousAngle) / 60f *
            worldVisualRadius / dt;

        float desiredDistance = radius + surfaceGap;

        float actualDistance = Vector2.Dot(
            center - hit.point,
            surfaceNormal
        );

        float normalVelocity = Mathf.Clamp(
            (desiredDistance - actualDistance) / dt,
            -correctionSpeed,
            correctionSpeed
        );

        body.linearVelocity =
            tangent * moveVelocity +
            surfaceNormal * normalVelocity;

        UpdateRollingVisual(
            center,
            tangent,
            actualDistance,
            worldVisualRadius
        );
    }

    private bool FindSurface(
        Vector2 center,
        float radius,
        float input,
        out RaycastHit2D bestHit)
    {
        float probeRadius = radius * 0.9f;

        float probeDistance =
            radius - probeRadius + surfaceGap + searchDistance;

        Vector2 tangent = new Vector2(
            surfaceNormal.y,
            -surfaceNormal.x
        );

        // With movement input, check the surface ahead in the travel direction.
        if (Mathf.Abs(input) > 0.01f)
        {
            Vector2 moveDirection = tangent * Mathf.Sign(input);

            float forwardDistance =
                Mathf.Abs(input) *
                possessable.MoveSpeed *
                Time.fixedDeltaTime +
                radius - probeRadius +
                surfaceGap +
            searchDistance; ;

            RaycastHit2D forwardHit = Physics2D.CircleCast(
                center,
                probeRadius,
                moveDirection,
                forwardDistance,
                surfaceMask
            );

            if (forwardHit.collider != null &&
                forwardHit.distance > 0f)
            {
                Vector2 candidateNormal = forwardHit.normal.normalized;

                float normalDot = Vector2.Dot(
                    candidateNormal,
                    surfaceNormal
                );

                // Negative: moving toward this surface.
                // Positive: moving away from this surface; do not switch to it.
                float approachDot = Vector2.Dot(
                    moveDirection,
                    candidateNormal
                );

                if (normalDot < 0.5f && approachDot < -0.05f)
                {
                    bestHit = forwardHit;
                    return true;
                }
            }
        }
        // Detect the current attached surface with or without movement input.
        bestHit = Physics2D.CircleCast(
            center,
            probeRadius,
            -surfaceNormal,
            probeDistance,
            surfaceMask
        );

        if (bestHit.collider != null && bestHit.distance > 0f)
        {
            return true;
        }

        // Search nearby surfaces when the current surface is lost.
        bestHit = default;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;

            Vector2 direction = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );

            RaycastHit2D hit = Physics2D.CircleCast(
                center,
                probeRadius,
                direction,
                probeDistance,
                surfaceMask
            );

            if (hit.collider == null || hit.distance <= 0f)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                bestHit = hit;
            }
        }

        return bestHit.collider != null;
    }

    private void UpdateRollingVisual(
    Vector2 center,
    Vector2 tangent,
    float actualDistance,
    float radius)
    {
        if (visual == null)
            return;

        // Progress through the current 60-degree roll, from 0 to 1.
        float phase = Mathf.Repeat(rollAngle, 60f) / 60f;

        // Rotate from -30 to +30 degrees.
        float pivotAngle = (phase - 0.5f) * Mathf.PI / 3f;

        // The center is lower on an edge and higher when pivoting over a vertex.
        float supportHeight = radius * Mathf.Cos(pivotAngle);

        // Horizontal path while rolling around the contact vertex,
        // compensated visually relative to the body's constant-speed movement.
        float arcTravel = radius * (
            0.5f + Mathf.Sin(pivotAngle)
        );

        float linearTravel = phase * radius;
        float tangentOffset = arcTravel - linearTravel;

        Vector2 visualCenter =
            center +
            surfaceNormal * (
                supportHeight + visualSurfaceGap - actualDistance
            ) +
            tangent * tangentOffset;

        visual.position = new Vector3(
            visualCenter.x,
            visualCenter.y,
            visual.position.z
        );

        // A flat-top hexagon on the ground has a horizontal bottom edge at zero degrees.
        float surfaceAngle = Mathf.Atan2(
            tangent.y,
            tangent.x
        ) * Mathf.Rad2Deg;

        visual.rotation = Quaternion.Euler(
            0f,
            0f,
            surfaceAngle - rollAngle
        );
    }

    private void ResetRollingVisual()
    {
        rollAngle = 0f;

        if (visual != null)
        {
            visual.localPosition = originalVisualLocalPosition;
            visual.localRotation = originalVisualLocalRotation;
        }
    }

    public void Release()
    {
        IsAttached = false;
        surfaceNormal = Vector2.up;

        ResetRollingVisual();

        if (body != null)
        {
            body.gravityScale = airGravity;
        }
    }

    private void OnDisable()
    {
        Release();
    }
}