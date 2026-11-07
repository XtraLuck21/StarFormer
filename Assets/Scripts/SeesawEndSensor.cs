using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SeesawEndSensor : MonoBehaviour
{
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private SeesawTilt2D.Side side;
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);
    private void Check(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (seesaw != null && body != null && body.TryGetComponent(out WindReleasedBall ball))
            seesaw.TryTilt(side, ball);
    }
}
