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

        // 在方块收到的碰撞中，顶部受踩的法线朝下。
        // 侧面碰撞、从下面顶到都不会触发。
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

    // 踩断后立即失去支撑。
    foreach (Collider2D blockCollider in blockColliders)
    {
        blockCollider.enabled = false;
    }

    // 播放碎片效果。
    if (TryGetComponent<ShatterEffect>(out var effect))
    {
        effect.Play();
    }

    // 移除原本完整的方块。
    Destroy(gameObject);
}

}