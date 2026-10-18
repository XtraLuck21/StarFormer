using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class HintZone : MonoBehaviour
{
    [Header("References")]
    // Text to fade in and out.
    [SerializeField] private TMP_Text hintText;

    [Header("Behaviour")]
    // Enabled: fade out after leaving the zone.
    // Disabled: keep the text visible once revealed.
    [SerializeField] private bool hideOnExit = true;

    // Enabled: trigger once only; subsequent entries do not show the text again.
    [SerializeField] private bool showOnlyOnce = false;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInTime = 0.4f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.3f;

    private Collider2D zoneCollider;
    private bool isInside;
    private bool hasShown;
    private float alpha;

    private void Start()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;

        if (hintText == null)
        {
            Debug.LogError(
                "Assign the hint text to the Hint Text field in the Inspector.",
                this
            );

            enabled = false;
            return;
        }

        // Hide the text at startup.
        alpha = 0f;
        hintText.alpha = 0f;
    }

    private void Update()
    {
        bool shouldShow = isInside || (!hideOnExit && hasShown);

        float target = shouldShow ? 1f : 0f;
        float duration = shouldShow ? fadeInTime : fadeOutTime;

        if (Mathf.Approximately(alpha, target))
        {
            return;
        }

        // Use unscaledDeltaTime so the text can fade during a death freeze.
        float step = duration > 0f
            ? Time.unscaledDeltaTime / duration
            : 1f;

        alpha = Mathf.MoveTowards(alpha, target, step);
        hintText.alpha = alpha;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (showOnlyOnce && hasShown)
        {
            return;
        }

        isInside = true;
        hasShown = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            isInside = false;
        }
    }

    // Only accept the body currently controlled by the player.
    private bool IsPlayer(Collider2D other)
    {
        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null)
        {
            return false;
        }

        return otherBody.TryGetComponent(
                   out PossessableBody possessableBody) &&
               possessableBody.IsControlled;
    }
}
