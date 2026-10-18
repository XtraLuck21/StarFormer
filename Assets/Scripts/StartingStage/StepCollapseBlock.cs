using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class StepCollapseBlock : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float fallDelay = 0.3f;

    private Rigidbody2D body;
    private Collider2D[] blockColliders;

    private bool triggered;
    private bool falling;
    private float timer;
    private float fallSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        blockColliders = GetComponentsInChildren<Collider2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.useFullKinematicContacts = true;

        GetComponent<BoxCollider2D>().isTrigger = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckStep(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckStep(collision);
    }

    private void CheckStep(Collision2D collision)
    {
        if (triggered)
        {
            return;
        }

        Rigidbody2D playerBody = collision.rigidbody;

        if (playerBody == null ||
            !playerBody.TryGetComponent<PossessableBody>(
                out var possessable) ||
            !possessable.IsControlled)
        {
            return;
        }

        // In collisions received by the block, a landing on its top has a downward normal.
        // Side impacts and hits from below do not trigger collapse.
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.normal.y < -0.5f &&
                playerBody.worldCenterOfMass.y > body.worldCenterOfMass.y)
            {
                triggered = true;
                timer = 0f;
                return;
            }
        }
    }

private void FixedUpdate()
{
    if (!triggered || falling)
    {
        return;
    }

    timer += Time.fixedDeltaTime;

    if (timer < fallDelay)
    {
        return;
    }

    falling = true;

    // Remove support immediately when the block collapses.
    foreach (Collider2D blockCollider in blockColliders)
    {
        blockCollider.enabled = false;
    }

    // Play the shatter effect.
    if (TryGetComponent<ShatterEffect>(out var effect))
    {
        effect.Play();
    }

    // Remove the original intact block.
    Destroy(gameObject);
}

}