using UnityEngine;

/// <summary>Tilts the complete beam clockwise when an Obstacle lands on its right half.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class ObstacleSeesawTilt2D : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boardSurface;
    [SerializeField, Range(0f, 60f)] private float rightTiltDegrees = 30f;
    [Tooltip("Seconds to reach the final tilt. Increase for a heavier, slower lift.")]
    [SerializeField, Min(0.05f)] private float tiltDuration = 2.2f;

    public bool HasTriggered { get; private set; }
    public bool IsTilting { get; private set; }
    public float TiltProgress => HasTriggered ? Mathf.Clamp01(elapsed / Mathf.Max(0.05f, tiltDuration)) : 0f;

    private Rigidbody2D pivot;
    private float startAngle;
    private float targetAngle;
    private float elapsed;

    private void Awake()
    {
        pivot = GetComponent<Rigidbody2D>();
        pivot.bodyType = RigidbodyType2D.Kinematic;
        pivot.gravityScale = 0f;
        pivot.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        pivot.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (boardSurface == null)
        {
            Debug.LogError("Assign the Beam collider to the seesaw's Board Surface.", this);
            enabled = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => CheckLanding(collision);
    private void OnCollisionStay2D(Collision2D collision) => CheckLanding(collision);

    private void CheckLanding(Collision2D collision)
    {
        if (!isActiveAndEnabled || HasTriggered || boardSurface == null) return;
        Rigidbody2D incoming = collision.rigidbody;
        if (incoming == null || incoming == pivot || incoming.bodyType != RigidbodyType2D.Dynamic ||
            !incoming.TryGetComponent<SeesawObstacle>(out _)) return;

        // Handles share this rigidbody, but only the top of the actual board triggers a tilt.
        if (collision.collider != boardSurface && collision.otherCollider != boardSurface) return;
        float surfaceTop = boardSurface.offset.y + boardSurface.size.y * 0.5f;
        float surfaceTolerance = 0.035f / Mathf.Max(0.001f, Mathf.Abs(boardSurface.transform.lossyScale.y));
        Vector2 center = boardSurface.transform.InverseTransformPoint(incoming.worldCenterOfMass);
        if (center.y <= surfaceTop) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 point = collision.GetContact(i).point;
            Vector2 boardPoint = boardSurface.transform.InverseTransformPoint(point);
            Vector2 pivotPoint = transform.InverseTransformPoint(point);
            if (pivotPoint.x <= 0.05f || boardPoint.y < surfaceTop - surfaceTolerance) continue;

            HasTriggered = true;
            IsTilting = true;
            startAngle = pivot.rotation;
            // Unity's negative Z rotation lowers the right end. The target is 30 degrees below horizontal.
            float parentAngle = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
            targetAngle = parentAngle - rightTiltDegrees;
            elapsed = 0f;
            return;
        }
    }

    private void FixedUpdate()
    {
        if (!IsTilting) return;
        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, tiltDuration));
        pivot.MoveRotation(Mathf.LerpAngle(startAngle, targetAngle, Mathf.SmoothStep(0f, 1f, t)));
        if (t >= 1f) IsTilting = false;
    }
}
