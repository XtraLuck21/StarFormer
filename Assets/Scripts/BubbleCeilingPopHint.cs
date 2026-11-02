using TMPro;
using UnityEngine;

// One-time pop lesson above the water exit's rise-and-drop passage in Level01_Advanced.
[RequireComponent(typeof(TextMeshPro))]
public class BubbleCeilingPopHint : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private Collider2D ceiling;
    [SerializeField] private Rect teachingArea = new Rect(16.8f, 9.2f, 3.5f, 1.3f);
    [SerializeField, Min(.1f)] private float stuckDelay = 3f;

    private TMP_Text label;
    private PossessableBody trackedBubble;
    private float contactSeconds;
    private bool hasShown;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        label.text = "Press <b>Q</b>\nto pop";
        ResetContact();
        if (playerController == null || ceiling == null)
        {
            Debug.LogError("Assign the player controller and the water-exit ceiling.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Pausing neither advances the lesson timer nor consumes the hint.
        if (Time.timeScale <= 0f) return;
        PossessableBody body = playerController != null ? playerController.CurrentBody : null;
        if (playerController == null || !playerController.isActiveAndEnabled ||
            playerController.IsDead ||
            body == null || !body.isActiveAndEnabled || !body.IsReady || !body.IsControlled ||
            body.Form != BodyForm.Bubble || !body.Body.simulated ||
            !body.BodyCollider.enabled || !teachingArea.Contains(body.Body.position) ||
            !TouchesCeiling(body))
        {
            ResetContact();
            return;
        }

        if (trackedBubble != body)
        {
            ResetContact();
            trackedBubble = body;
        }
        // Keep the first display visible while contact continues; never show it again
        // after leaving, popping, or switching bodies. Reloading the scene starts fresh.
        if (hasShown) return;
        contactSeconds += Time.deltaTime;
        if (contactSeconds >= stuckDelay)
        {
            hasShown = true;
            label.enabled = true;
        }
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

    private void ResetContact()
    {
        trackedBubble = null;
        contactSeconds = 0f;
        if (label != null) label.enabled = false;
    }

    private void OnDisable() => ResetContact();
}
