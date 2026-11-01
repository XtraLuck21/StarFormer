using UnityEngine;

/// <summary>A ceiling that ratchets upward and never falls back during a run.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class RatchetingCeiling : MonoBehaviour
{
    [SerializeField, Min(0f)] private float maximumRise = 3.5f;
    [SerializeField, Min(0.01f)] private float ballLiftStep = 0.6f;
    [SerializeField, Min(0.01f)] private float liftSpeed = 3f;

    private BoxCollider2D ceiling;
    private float maximumBottom;
    private float targetBottom;
    private float nextBallHitTime;

    private void Awake()
    {
        ceiling = GetComponent<BoxCollider2D>();
        targetBottom = ceiling.bounds.min.y;
        maximumBottom = targetBottom + maximumRise;
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
        SetBottom(Mathf.Min(requiredBottom, maximumBottom));
    }

    private void FixedUpdate()
    {
        SetBottom(Mathf.MoveTowards(ceiling.bounds.min.y, targetBottom,
            liftSpeed * Time.fixedDeltaTime));
    }

    private void SetBottom(float bottom)
    {
        float rise = bottom - ceiling.bounds.min.y;
        if (rise <= 0f) return;

        transform.position += Vector3.up * rise;
        Physics2D.SyncTransforms();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time < nextBallHitTime || collision.rigidbody == null ||
            !collision.rigidbody.TryGetComponent(out PossessableBody ball) ||
            !ball.IsControlled || ball.Form != BodyForm.BouncyBall || ball.IsInWater)
            return;

        // On the ceiling, an upward normal identifies a strike on its underside.
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < 0.65f) continue;

            targetBottom = Mathf.Min(maximumBottom, targetBottom + ballLiftStep);
            nextBallHitTime = Time.time + 0.15f;
            break;
        }
    }
}
