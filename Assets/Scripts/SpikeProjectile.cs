using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpikeProjectile : MonoBehaviour
{
    // Seconds before an unhit spike removes itself.
    [SerializeField] private float lifetime = 3f;

    private Rigidbody2D body;
    private Rigidbody2D shooter;
    private bool hasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        // Fly in a straight line without falling or spinning.
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Detect hits without pushing objects around.
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 velocity, Rigidbody2D shooterBody)
    {
        shooter = shooterBody;
        body.linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore sensors (like pressure plates) and the wall that fired it.
        if (hasHit ||
            other.isTrigger ||
            other.attachedRigidbody == shooter)
        {
            return;
        }

        hasHit = true;
        Debug.Log($"Spike hit {other.name}.", this);
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.1f, lifetime);
    }
}
