using UnityEngine;

/// <summary>A Box-lifted panel that holds its position between impacts.</summary>
// Move after the ball launches, so lowering its support does not swallow a bounce.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
public class RatchetingCeiling : MonoBehaviour
{
    [SerializeField, Min(0f)] private float maximumRise = 3.5f;
    [SerializeField, Min(0.01f)] private float ballLiftStep = 0.6f;
    [SerializeField, Min(0.01f)] private float liftSpeed = 3f;
    [Tooltip("Box lifts the door; controlled Ball landings on its top lower it instead of lifting it from below.")]
    [SerializeField] private bool ballPushesDown;

    [Tooltip("Only Box growth moves this panel. Ball impacts never raise or lower it.")]
    [SerializeField] private bool boxOnlyLift;

    public float CurrentBottom => ceiling.bounds.min.y;

    public float MaximumRise => maximumRise;

    public float CurrentRise =>
        ceiling != null ? ceiling.bounds.min.y - minimumBottom : 0f;

    public bool BallRaisesCeiling => !boxOnlyLift && !ballPushesDown;

    private BoxCollider2D ceiling;
    private float minimumBottom;
    private float maximumBottom;
    private float targetBottom;
    private float nextBallHitTime;
    private readonly Collider2D[] blockers = new Collider2D[32];

    private void Awake()
    {
        ceiling = GetComponent<BoxCollider2D>();
        minimumBottom = ceiling.bounds.min.y;
        targetBottom = minimumBottom;
        maximumBottom = minimumBottom + maximumRise;
    }

    // This is a pure query: BoxResizeAbility probes several candidate sizes.
    public bool CanLiftForBox(Collider2D box, float requiredBottom)
    {
        return isActiveAndEnabled && ceiling != null &&
            box.bounds.max.y <= ceiling.bounds.min.y + 0.03f &&
            requiredBottom <= maximumBottom;
    }

    public void LiftForBox(float requiredBottom)
    {
        targetBottom = Mathf.Max(targetBottom, Mathf.Min(requiredBottom, maximumBottom));
        // Clear the proposed box bounds before the resize is applied.
        SetBottom(Mathf.Max(ceiling.bounds.min.y, Mathf.Min(requiredBottom, maximumBottom)));
    }

    private void FixedUpdate()
    {
        float bottom = Mathf.MoveTowards(ceiling.bounds.min.y, targetBottom,
            liftSpeed * Time.fixedDeltaTime);
        if (bottom < ceiling.bounds.min.y)
            bottom = SafeLowerBottom(bottom);
        SetBottom(bottom);
    }

    private float SafeLowerBottom(float requestedBottom)
    {
        Bounds bounds = ceiling.bounds;
        float distance = bounds.min.y - requestedBottom;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = Physics2D.OverlapBox(
            new Vector2(bounds.center.x, (bounds.min.y + requestedBottom) * 0.5f),
            new Vector2(Mathf.Max(0.01f, bounds.size.x - 0.02f), distance + 0.02f),
            0f, filter, blockers);
        if (count == blockers.Length) return bounds.min.y;
        for (int i = 0; i < count; i++)
        {
            Collider2D other = blockers[i];
            if (other == ceiling || Physics2D.GetIgnoreCollision(ceiling, other)) continue;
            // Anything under the door (including the abandoned Box) stops its descent.
            requestedBottom = Mathf.Max(requestedBottom, other.bounds.max.y + 0.01f);
        }
        return Mathf.Min(bounds.min.y, requestedBottom);
    }

    private void SetBottom(float bottom)
    {
        float movement = Mathf.Clamp(bottom, minimumBottom, maximumBottom) - ceiling.bounds.min.y;
        if (Mathf.Abs(movement) < 0.00001f) return;
        transform.position += Vector3.up * movement;
        Physics2D.SyncTransforms();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (boxOnlyLift || Time.time < nextBallHitTime || collision.rigidbody == null ||
            !collision.rigidbody.TryGetComponent(out PossessableBody ball) ||
            !ball.IsControlled || ball.Form != BodyForm.BouncyBall || ball.IsInWater)
            return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // On this panel, top impacts have downward normals; underside impacts upward normals.
            if (ballPushesDown ? contact.normal.y > -0.65f : contact.normal.y < 0.65f)
                continue;
            targetBottom = ballPushesDown
                ? Mathf.Max(minimumBottom, targetBottom - ballLiftStep)
                : Mathf.Min(maximumBottom, targetBottom + ballLiftStep);
            nextBallHitTime = Time.time + 0.15f;
            break;
        }
    }
}
