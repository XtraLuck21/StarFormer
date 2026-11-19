using UnityEngine;

/// <summary>The invisible triangle reveals the seesaw view when a blocking ball enters it.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class FanSeesawRevealTrigger : MonoBehaviour
{
    [SerializeField] private FanSeesawCameraReveal cameraReveal;

    private void Awake() => GetComponent<PolygonCollider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled || cameraReveal == null) return;
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic ||
            !body.TryGetComponent<SeesawObstacle>(out _)) return;

        cameraReveal.RevealFromTrigger();
    }
}
