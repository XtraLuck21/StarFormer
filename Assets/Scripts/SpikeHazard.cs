using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class SpikeHazard : MonoBehaviour
{
    [SerializeField]
    private PlayerPossessionController playerController;

    private void Awake()
    {
        GetComponent<PolygonCollider2D>().isTrigger = true;

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

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null)
        {
            return;
        }

        if (otherBody.TryGetComponent(
                out PossessableBody possessableBody) &&
            possessableBody.IsControlled)
        {
            playerController.KillControlledBody(possessableBody);
        }
    }
}