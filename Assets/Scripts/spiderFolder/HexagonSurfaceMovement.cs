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

    // Flat Top 六边形：中心到左右尖角的本地距离。
    // 如果图片宽度为 1，则这里填 0.5。
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

        // 有输入时，检查行进方向前方的表面。
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

                // 小于 0：正在朝这个表面撞过去。
                // 大于 0：正在离开这个表面，不应切换过去。
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
        // 无论有没有输入，都检测当前吸附表面。
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

        // 当前表面丢失后，搜索附近表面。
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

        // 本次 60° 翻滚的进度：0 到 1。
        float phase = Mathf.Repeat(rollAngle, 60f) / 60f;

        // 从 -30° 到 +30°。
        float pivotAngle = (phase - 0.5f) * Mathf.PI / 3f;

        // 边贴墙时中心较低，转到顶点时中心较高。
        float supportHeight = radius * Mathf.Cos(pivotAngle);

        // 绕接触顶点翻滚的水平路径，
        // 相对于身体匀速前进的位置做视觉补偿。
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

        // Flat Top 六边形在地面上，0° 就有一条水平底边。
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