using UnityEngine;

/// <summary>Floor spikes collect ordinary balls and break Fans belonging to this room.</summary>
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class SeesawBallSpike : MonoBehaviour
{
    [SerializeField] private SeesawRoomSupplies supplies;
    private void Awake() => GetComponent<PolygonCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);

    private void Check(Collider2D other)
    {
        if (!isActiveAndEnabled || supplies == null) return;
        Rigidbody2D body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out WindReleasedBall ball))
            ball.TryDespawnOnSpikes(supplies);
        else if (body != null && body.TryGetComponent(out PossessableBody fan))
            supplies.TryBreakFanOnSpikes(fan);
    }
}
