using TMPro;
using UnityEngine;

// Scene-local restart lesson above the first water tank in Level01_Advanced.
[RequireComponent(typeof(TextMeshPro))]
public class BubbleCeilingRestartHint : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private Collider2D ceiling;
    [SerializeField] private Rect teachingArea = new Rect(7.4f, 5f, 8.9f, 5.6f);
    [SerializeField, Min(.1f)] private float stuckDelay = 1.5f;

    private TMP_Text label;
    private PossessableBody trackedBubble;
    private float contactSeconds;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        label.text = "Feel stuck?\nPress <b>R</b> to restart";
        ResetHint();
        if (playerController == null || ceiling == null)
        {
            Debug.LogError("Assign the player controller and the water tank ceiling.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        PossessableBody body = playerController != null ? playerController.CurrentBody : null;
        if (playerController == null || !playerController.isActiveAndEnabled ||
            playerController.IsDead || Time.timeScale <= 0f || body == null ||
            !body.isActiveAndEnabled || !body.IsReady || !body.IsControlled ||
            body.Form != BodyForm.Bubble || !body.Body.simulated ||
            !body.BodyCollider.enabled || !teachingArea.Contains(body.Body.position) ||
            !TouchesCeiling(body))
        {
            ResetHint();
            return;
        }

        if (trackedBubble != body)
        {
            ResetHint();
            trackedBubble = body;
        }
        // Sideways movement is allowed: the lesson is about being blocked from rising.
        contactSeconds += Time.deltaTime;
        label.enabled = contactSeconds >= stuckDelay;
    }

    private bool TouchesCeiling(PossessableBody body)
    {
        int count = body.BodyCollider.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = contacts[i];
            if (contact.normal.y < -.65f &&
                (contact.collider == ceiling || contact.otherCollider == ceiling))
                return true;
        }
        return false;
    }

    private void ResetHint()
    {
        trackedBubble = null;
        contactSeconds = 0f;
        if (label != null) label.enabled = false;
    }

    private void OnDisable() => ResetHint();
}
