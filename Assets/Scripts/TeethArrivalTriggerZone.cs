using UnityEngine;
using UnityEngine.Events;

/// <summary>Draggable world-space trigger box: fires when the linked tooth rises into it.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TeethArrivalTriggerZone : MonoBehaviour
{
    [Tooltip("The MovableTeeth object (has RatchetingCeiling) that must enter this box.")]
    [SerializeField] private RatchetingCeiling targetTooth;

    [Tooltip("Fire only the first time the tooth arrives.")]
    [SerializeField] private bool once = true;

    [SerializeField] private UnityEvent onToothArrived = new UnityEvent();
    [SerializeField] private UnityEvent onToothLeft = new UnityEvent();

    private bool hasArrived;
    private BoxCollider2D zone;

    public bool HasArrived => hasArrived;

    public void RestoreArrived()
    {
        if (targetTooth == null || hasArrived) return;
        targetTooth.LiftForBox(targetTooth.CurrentBottom + targetTooth.MaximumRise);
        hasArrived = true;
        onToothArrived.Invoke();
    }

    private void Awake()
    {
        zone = GetComponent<BoxCollider2D>();
        zone.isTrigger = true;

        // Trigger callbacks need a Rigidbody2D on at least one side.
        if (GetComponent<Rigidbody2D>() == null)
        {
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.simulated = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTooth(other))
        {
            return;
        }

        if (hasArrived && once)
        {
            return;
        }

        hasArrived = true;
        onToothArrived.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsTooth(other) || once || !hasArrived)
        {
            return;
        }

        hasArrived = false;
        onToothLeft.Invoke();
    }

    private bool IsTooth(Collider2D other)
    {
        if (targetTooth == null || other == null)
        {
            return false;
        }

        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.TryGetComponent(out RatchetingCeiling fromBody) &&
            fromBody == targetTooth)
        {
            return true;
        }

        return other.transform == targetTooth.transform ||
               other.transform.IsChildOf(targetTooth.transform);
    }
}
