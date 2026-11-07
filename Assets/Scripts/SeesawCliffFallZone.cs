using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawCliffFallZone : MonoBehaviour
{
    [SerializeField] private SeesawRoomSupplies supplies;
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);

    private void Check(Collider2D other)
    {
        if (supplies == null || other.attachedRigidbody == null) return;
        Rigidbody2D body = other.attachedRigidbody;
        if (body.TryGetComponent(out WindReleasedBall ball)) ball.TryDespawnOnFall(supplies);
        else if (body.TryGetComponent(out PossessableBody form))
        {
            if (form.IsControlled && form.Owner != null)
                form.Owner.KillControlledBody(form, AnalyticsDeathCause.OutOfBoundsLow);
            else if (form.Form == BodyForm.Fan) supplies.TryRemoveFallenFan(form);
        }
    }
}
