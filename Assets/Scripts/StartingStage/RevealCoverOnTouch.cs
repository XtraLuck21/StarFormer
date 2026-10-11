using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RevealCoverOnTouch : MonoBehaviour
{
    private SpriteRenderer[] coverRenderers;
    private Collider2D triggerArea;
    private bool revealed;

    private void Awake()
    {
        coverRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        triggerArea = GetComponent<Collider2D>();
        triggerArea.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void CheckPlayer(Collider2D other)
    {
        if (revealed)
        {
            return;
        }

        Rigidbody2D playerBody = other.attachedRigidbody;

        if (playerBody == null ||
            !playerBody.TryGetComponent<PossessableBody>(out var player) ||
            !player.IsControlled)
        {
            return;
        }

        revealed = true;

        foreach (SpriteRenderer coverRenderer in coverRenderers)
        {
            coverRenderer.enabled = false;
        }

        triggerArea.enabled = false;
    }
}