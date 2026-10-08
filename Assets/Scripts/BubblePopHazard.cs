using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BubblePopHazard : MonoBehaviour
{
    [SerializeField]
    private PlayerPossessionController playerController;

    private void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;

        if (playerController == null)
        {
            Debug.LogError(
                "Assign the scene's PlayerPossessionController.",
                this
            );

            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckContact(other);
    }

    private void CheckContact(Collider2D other)
    {
        if (!isActiveAndEnabled || playerController == null)
        {
            return;
        }

        Rigidbody2D body = other.attachedRigidbody;

        if (body != null &&
            body.TryGetComponent(out PossessableBody hitBody) &&
            hitBody.IsControlled &&
            hitBody.Form == BodyForm.Bubble)
        {
            playerController.RequestBubblePop(hitBody);
        }
    }
}