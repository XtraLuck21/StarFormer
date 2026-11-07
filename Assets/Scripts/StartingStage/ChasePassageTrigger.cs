using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ChasePassageTrigger : MonoBehaviour
{
    [SerializeField] private level2Camera levelCamera;
    [SerializeField] private SpriteRenderer doorwayVisual;
    private bool triggered;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => CheckPassage(other);
    private void OnTriggerStay2D(Collider2D other) => CheckPassage(other);

    private void CheckPassage(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (triggered || levelCamera == null || body == null ||
            !body.TryGetComponent<PossessableBody>(out var star) ||
            star.Form != BodyForm.Star || !star.IsControlled ||
            star.Owner.IsDead || star.Owner.CurrentBody != star ||
            body.position.x < transform.position.x)
        {
            return;
        }

        triggered = true;
        if (doorwayVisual != null)
        {
            doorwayVisual.color = new Color(1f, 0.2f, 0.1f, 0.25f);
        }
        levelCamera.BeginDoorChase();
    }
}
