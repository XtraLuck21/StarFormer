using UnityEngine;

/// <summary>A single kinematic pivot drives the board; dynamic riders retain real collision support.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SeesawTilt2D : MonoBehaviour
{
    public enum Side { None, Left, Right }
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private BoxCollider2D boardSurface;
    [SerializeField] private TriangleFulcrumVisual fulcrum;
    [SerializeField] private float initialAngle;
    [SerializeField, Range(1f, 15f)] private float leftTiltDegrees = 9f;
    [SerializeField, Range(1f, 15f)] private float rightTiltDegrees = 9f;
    [SerializeField, Min(0.05f)] private float animationDuration = 1.2f;

    public Side LastSide { get; private set; }
    public int TriggerCount { get; private set; }
    public bool IsAnimating { get; private set; }
    public bool IsSolved { get; private set; }
    public float TargetAngle { get; private set; }
    public float CurrentAngle => pivot != null ? pivot.rotation : transform.eulerAngles.z;
    public bool ReferencesReady => supplies != null && boardSurface != null && fulcrum != null;
    private Rigidbody2D pivot;
    private float fromAngle;
    private float elapsed;
    private bool overloadHeld;

    private void Awake()
    {
        pivot = GetComponent<Rigidbody2D>();
        foreach (Joint2D joint in GetComponents<Joint2D>()) joint.enabled = false;
        pivot.bodyType = RigidbodyType2D.Kinematic;
        pivot.gravityScale = 0f;
        pivot.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        pivot.interpolation = RigidbodyInterpolation2D.Interpolate;
        pivot.useFullKinematicContacts = true;
        pivot.rotation = initialAngle;
        TargetAngle = initialAngle;
    }

    public bool TryTilt(Side side, WindReleasedBall ball)
    {
        if (!isActiveAndEnabled || IsSolved || overloadHeld || !ReferencesReady || side == Side.None ||
            ball == null || !ball.TryUseSeesawTrigger(supplies)) return false;
        LastSide = side;
        TriggerCount++;
        fromAngle = pivot.rotation;
        // Positive rotation lowers the left end; negative lowers the right.
        TargetAngle = initialAngle + (side == Side.Left ? leftTiltDegrees : -rightTiltDegrees);
        elapsed = 0f;
        IsAnimating = Mathf.Abs(Mathf.DeltaAngle(fromAngle, TargetAngle)) > 0.01f;
        return true;
    }

    public void CompletePuzzle()
    {
        if (IsSolved) return;
        IsSolved = true;
        fromAngle = pivot.rotation;
        TargetAngle = initialAngle;
        elapsed = 0f;
        IsAnimating = Mathf.Abs(Mathf.DeltaAngle(fromAngle, TargetAngle)) > .01f;
    }

    public void HoldLeftForOverload(bool strained = false)
    {
        overloadHeld = true;
        LastSide = Side.Left;
        fromAngle = pivot.rotation;
        TargetAngle = initialAngle + leftTiltDegrees + (strained ? 2f : 0f);
        elapsed = 0f;
        IsAnimating = true;
    }

    public void HoldNeutral()
    {
        overloadHeld = true;
        LastSide = Side.None;
        AnimateTo(initialAngle);
    }

    public void LiftLeftFromBall(float rightDownDegrees)
    {
        overloadHeld = true;
        LastSide = Side.Right;
        AnimateTo(initialAngle - rightDownDegrees);
    }

    private void AnimateTo(float angle)
    {
        fromAngle = pivot.rotation;
        TargetAngle = angle;
        elapsed = 0f;
        IsAnimating = true;
    }

    private void FixedUpdate()
    {
        if (!IsAnimating || Time.timeScale <= 0f) return;
        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, animationDuration));
        pivot.MoveRotation(Mathf.LerpAngle(fromAngle, TargetAngle, Mathf.SmoothStep(0f, 1f, t)));
        if (t >= 1f) IsAnimating = false;
    }

    public bool HasBallExited(Vector2 point, float inset, float below, float above)
    {
        // Check in board coordinates so a tilted endpoint is still the same exit.
        Vector2 p = boardSurface.transform.InverseTransformPoint(point);
        p -= boardSurface.offset;
        Vector3 scale = boardSurface.transform.lossyScale;
        float sx = Mathf.Max(0.001f, Mathf.Abs(scale.x));
        float sy = Mathf.Max(0.001f, Mathf.Abs(scale.y));
        float halfWidth = boardSurface.size.x * 0.5f;
        float halfHeight = boardSurface.size.y * 0.5f;
        float edge = halfWidth - Mathf.Min(inset / sx, halfWidth * 0.8f);
        return Mathf.Abs(p.x) > edge || p.y < -halfHeight - below / sy || p.y > halfHeight + above / sy;
    }
}
