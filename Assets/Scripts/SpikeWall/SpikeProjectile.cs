using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpikeProjectile : MonoBehaviour
{
    // Maximum seconds of flight. Stuck spikes stay until reload.
    [SerializeField] private float lifetime = 3f;

    // Fraction of normal speed while inside a WaterZone.
    [SerializeField, Range(0.05f, 1f)]
    private float waterSpeedMultiplier = 0.4f;

    // When true, a stuck spike becomes solid so bodies can stand on it.
    [SerializeField] private bool solidWhenStuck = false;

    // 勾上：打中移动尖刺时把它和自己一起销毁。
    [SerializeField] private bool canBreakMovingSpikes = true;

    private Rigidbody2D body;
    private Collider2D spikeCollider;
    private Rigidbody2D shooter;

    private Vector2 direction;
    private float baseSpeed;
    private float flightTime;

    private bool launched;
    private bool isStuck;
    private bool isFinished;

    public bool IsStuck => isStuck;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spikeCollider = GetComponent<Collider2D>();

        // Fly in a straight line without falling or spinning.
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Detect hits without pushing objects around.
        spikeCollider.isTrigger = true;
    }

    public void Launch(Vector2 velocity, Rigidbody2D shooterBody)
    {
        shooter = shooterBody;
        direction = velocity.normalized;
        baseSpeed = velocity.magnitude;
        flightTime = 0f;
        launched = true;

        UpdateSpeed();
    }

    private void FixedUpdate()
    {
        if (!launched || isFinished)
        {
            return;
        }

        // Only flying spikes time out.
        flightTime += Time.fixedDeltaTime;

        if (flightTime >= lifetime)
        {
            isFinished = true;
            Destroy(gameObject);
            return;
        }

        UpdateSpeed();
    }

    // Slow down inside water and recover full speed after leaving it.
    private void UpdateSpeed()
    {
        float speed = WaterZone.Contains(body.position)
            ? baseSpeed * waterSpeedMultiplier
            : baseSpeed;

        body.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore the wall that fired it.
        if (!launched || isFinished || other.attachedRigidbody == shooter)
        {
            return;
        }

        // 移动尖刺自己也是 trigger，所以这一步必须放在下面的
        // isTrigger 过滤之前，否则会被当成水域直接跳过。
        if (canBreakMovingSpikes && TryFindMovingSpike(other, out MovingSpikeHazard hazard))
        {
            isFinished = true;

            hazard.Shatter();
            Destroy(gameObject);

            Debug.Log($"Spike destroyed {hazard.name}.", this);
            return;
        }

        // 按钮：通知之后不 return，让刺继续按正常规则钉进去或飞过。
        if (TryFindButton(other, out SpikeButton button))
        {
            button.OnSpikeHit(this);
        }

        // Ignore sensors (water, pressure plates).
        if (other.isTrigger)
        {
            return;
        }

        isFinished = true;
        Rigidbody2D otherBody = other.attachedRigidbody;

        // Moving physics bodies (boxes, bubbles) do not hold spikes.
        if (otherBody != null &&
            otherBody.bodyType == RigidbodyType2D.Dynamic)
        {
            Debug.Log($"Spike hit {other.name} and broke.", this);
            Destroy(gameObject);
            return;
        }

        StickInto(other);
    }

    private static bool TryFindButton(Collider2D other, out SpikeButton button)
    {
        if (other.TryGetComponent(out button))
        {
            return true;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody != null && otherBody.TryGetComponent(out button))
        {
            return true;
        }

        button = null;
        return false;
    }

    // 碰撞体可能挂在子物体上，所以本体和刚体都找一遍。
    private static bool TryFindMovingSpike(
        Collider2D other,
        out MovingSpikeHazard hazard)
    {
        if (other.TryGetComponent(out hazard))
        {
            return true;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody != null && otherBody.TryGetComponent(out hazard))
        {
            return true;
        }

        hazard = null;
        return false;
    }

    // Freeze in place where the spike entered the surface.
    private void StickInto(Collider2D surface)
    {
        isStuck = true;

        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;

        if (solidWhenStuck)
        {
            spikeCollider.isTrigger = false;
        }

        Debug.Log($"Spike stuck in {surface.name}.", this);
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.1f, lifetime);
    }
}