using System.Collections;
using UnityEngine;

// Invisible trigger zone that activates objects in sequence when the controlled body enters.
[RequireComponent(typeof(Collider2D))]
public class HiddenTriggerZone : MonoBehaviour
{
    [Header("Targets")]
    // Objects to activate in list order; assign the four red spikes here.
    [SerializeField] private GameObject[] targets = new GameObject[4];

    // Enabled: hide all listed objects at startup instead of disabling them manually.
    [SerializeField] private bool hideTargetsAtStart = true;

    [Header("Timing")]
    // Delay after entry before activation starts.
    [SerializeField, Min(0f)] private float firstDelay = 0f;

    // Delay between objects; zero activates them simultaneously.
    [SerializeField, Min(0f)] private float delayBetween = 0.4f;

    [Header("Conditions")]
    // Enabled: only the player's currently controlled body can activate the trigger.
    [SerializeField] private bool requireControlledBody = true;

    [SerializeField] private bool triggerOnce = true;

    [Header("Editor")]
    // Enabled: hide this object's sprite in Play Mode but keep it visible while editing.
    [SerializeField] private bool hideSpriteOnPlay = true;

    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0f, 0.35f);

    private Collider2D zone;
    private bool hasFired;

    private void Awake()
    {
        zone = GetComponent<Collider2D>();

        // Detect entry without blocking movement.
        zone.isTrigger = true;

        if (hideSpriteOnPlay &&
            TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.enabled = false;
        }

        if (!hideTargetsAtStart)
        {
            return;
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasFired && triggerOnce)
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null ||
            !otherBody.TryGetComponent(out PossessableBody possessableBody))
        {
            return;
        }

        if (requireControlledBody && !possessableBody.IsControlled)
        {
            return;
        }

        hasFired = true;
        StartCoroutine(ActivateTargets());

        Debug.Log($"{name} triggered by {otherBody.name}.", this);
    }

    private IEnumerator ActivateTargets()
    {
        if (firstDelay > 0f)
        {
            yield return new WaitForSeconds(firstDelay);
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                target.SetActive(true);
            }

            if (delayBetween > 0f)
            {
                yield return new WaitForSeconds(delayBetween);
            }
        }
    }

    // Draw the trigger zone in the Scene view for easier placement.
    private void OnDrawGizmos()
    {
        Collider2D preview = GetComponent<Collider2D>();

        if (preview == null)
        {
            return;
        }

        Bounds bounds = preview.bounds;

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}