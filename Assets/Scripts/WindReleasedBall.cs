using UnityEngine;

/// <summary>An ordinary, non-possessable ball. Wind releases it once for its entire lifetime.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class WindReleasedBall : MonoBehaviour
{
    private SeesawRoomSupplies supply;
    private Rigidbody2D body;
    private CircleCollider2D ballCollider;
    private float fallingGravity;
    private RigidbodyConstraints2D fallingConstraints;
    private ulong armedAfterActivation;
    private bool hasTriggeredSeesaw;
    private Transform capturedSocket;
    private Vector2 capturedPosition;
    public bool IsCaptured { get; private set; }
    [SerializeField, Min(.1f)] private float releaseShakeSeconds = .6f;
    [SerializeField, Min(0f)] private float releaseShakeDistance = .16f;
    private float shakeElapsed;
    private Vector2 shakeOrigin;
    public bool IsShaking { get; private set; }
    public bool HasBeenReleased { get; private set; }
    public bool IsOwnedBy(SeesawRoomSupplies owner) => supply != null && supply == owner;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        fallingGravity = body.gravityScale;
        fallingConstraints = body.constraints;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.gravityScale = 0f;
    }

    public void Bind(SeesawRoomSupplies owner)
    {
        supply = owner;
        if (supply != null) supply.RegisterBallCollider(GetComponent<CircleCollider2D>());
        armedAfterActivation = FanBlower.LatestBlowActivation;
    }

    public void CaptureInSocket(Transform socket,float depth)
    {
        IsCaptured=true;
        HasBeenReleased=true;
        IsShaking=false;
        float radius=ballCollider.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y));
        capturedSocket=socket;
        capturedPosition=new Vector2(0f,-depth+radius+.06f);
        body.linearVelocity=Vector2.zero;
        body.angularVelocity=0f;
        body.gravityScale=0f;
        body.bodyType=RigidbodyType2D.Kinematic;
        body.constraints=RigidbodyConstraints2D.FreezeAll;
        ballCollider.isTrigger=false;
        transform.SetParent(socket,true);
        body.position=socket.TransformPoint(capturedPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision) => ReportLoad(collision);
    private void OnCollisionStay2D(Collision2D collision) => ReportLoad(collision);
    private void ReportLoad(Collision2D collision)
    {
        if (IsCaptured) return;
        if (HasBeenReleased && !ballCollider.isTrigger && collision.collider.TryGetComponent<SeesawGearBarrier>(out _))
        {
            float outside = collision.collider.bounds.max.x + ballCollider.bounds.extents.x;
            body.position = new Vector2(Mathf.Max(body.position.x, outside), body.position.y);
            BeginCliffDrop();
        }
    }

    public bool TryUseSeesawTrigger(SeesawRoomSupplies owner)
    {
        if (!HasBeenReleased || hasTriggeredSeesaw || supply == null || supply != owner) return false;
        hasTriggeredSeesaw = true;
        return true;
    }

    public bool TryDespawnOnSpikes(SeesawRoomSupplies owner)
        => TryDespawnOnFall(owner);

    public bool TryDespawnOnFall(SeesawRoomSupplies owner)
    {
        if (IsCaptured || !gameObject.activeInHierarchy || !HasBeenReleased || supply == null || supply != owner) return false;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    private void FixedUpdate()
    {
        if (IsCaptured)
        {
            if(capturedSocket!=null)body.position=capturedSocket.TransformPoint(capturedPosition);
            return;
        }
        if (supply == null || !supply.CanRunEffects) return;
        if (!HasBeenReleased && !IsShaking && !supply.FanSupplyStopped && supply.HasFanBlowCommandAfter(armedAfterActivation))
        {
            BeginReleaseShake();
        }
        if (IsShaking)
        {
            shakeElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(shakeElapsed / Mathf.Max(.1f, releaseShakeSeconds));
            // Two complete side-to-side shakes, ending at the original release point.
            body.position = shakeOrigin + Vector2.right *
                (Mathf.Sin(t * Mathf.PI * 4f) * Mathf.Sin(t * Mathf.PI) * releaseShakeDistance);
            if (t >= 1f)
            {
                body.position = shakeOrigin;
                IsShaking = false;
                HasBeenReleased = true;
                body.constraints = fallingConstraints;
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = fallingGravity;
                body.WakeUp();
            }
        }
        // Beyond the board the pit must collect ordinary balls even if a wall or
        // bridge lip would otherwise catch them above the collection trigger.
        if (HasBeenReleased && !ballCollider.isTrigger && supply.IsOverCliff(ballCollider.bounds))
            BeginCliffDrop();
        if (HasBeenReleased && supply.HasBallLeftBoard(body.position)) Destroy(gameObject);
    }

    private void BeginCliffDrop()
    {
        ballCollider.isTrigger = true;
        body.linearVelocity = Vector2.down * Mathf.Max(1f, -body.linearVelocity.y);
        body.constraints |= RigidbodyConstraints2D.FreezePositionX;
        body.WakeUp();
    }

    public bool BeginReleaseShake()
    {
        if (supply == null || !supply.CanRunEffects || supply.FanSupplyStopped || HasBeenReleased || IsShaking)
            return false;
        IsShaking = true;
        shakeElapsed = 0f;
        shakeOrigin = body.position;
        return true;
    }
}
