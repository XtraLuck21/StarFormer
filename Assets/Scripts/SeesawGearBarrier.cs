using UnityEngine;

/// <summary>Permanent player barrier protecting the decorative gear column.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawGearBarrier : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawRoomStage stage;
    [SerializeField] private SpriteRenderer visual;
    private BoxCollider2D barrier;

    private void Awake()
    {
        // Spider's authored surface mask contains Default only. This layer keeps
        // physical body collisions while excluding the wall from all climb probes.
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        barrier = GetComponent<BoxCollider2D>();
        barrier.isTrigger = false;
        foreach (Collider2D other in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (other == barrier || other.gameObject.scene != gameObject.scene) continue;
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent(out PossessableBody _))
                Physics2D.IgnoreCollision(barrier, other);
        }
        var feedback = gameObject.AddComponent<SeesawGateFeedback>();
        feedback.Initialize(stage, player, visual, true, hintText: null,
            collisionReveal: true, energyColor: new Color(.2f, .65f, 1f, .85f));
    }

    public void AllowBall(Collider2D ball)
    {
        if (barrier == null) barrier = GetComponent<BoxCollider2D>();
        if (ball != null) Physics2D.IgnoreCollision(barrier, ball);
    }

}
