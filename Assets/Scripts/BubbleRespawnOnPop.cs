using UnityEngine;

/// <summary>
/// Marks a bubble that immediately creates a fresh replacement at its
/// authored position whenever the controlled instance pops.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PossessableBody))]
public sealed class BubbleRespawnOnPop : MonoBehaviour
{
    [SerializeField] private GameObject bubblePrefab;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;
    private Transform respawnParent;

    private void Awake()
    {
        respawnPosition = transform.position;
        respawnRotation = transform.rotation;
        respawnParent = transform.parent;
    }

    public bool TrySpawnReplacement()
    {
        if (bubblePrefab == null)
        {
            Debug.LogError(
                "Assign a bubble prefab before enabling pop respawning.",
                this
            );

            return false;
        }

        GameObject replacement = Instantiate(
            bubblePrefab,
            respawnPosition,
            respawnRotation,
            respawnParent
        );

        replacement.name = gameObject.name;
        replacement.transform.localScale = transform.localScale;

        if (!replacement.TryGetComponent(out PossessableBody body) ||
            body.Form != BodyForm.Bubble)
        {
            Debug.LogError(
                "The assigned respawn prefab must be a possessable bubble.",
                this
            );

            Destroy(replacement);
            return false;
        }

        BubbleRespawnOnPop nextRespawner =
            replacement.AddComponent<BubbleRespawnOnPop>();
        nextRespawner.bubblePrefab = bubblePrefab;

        Rigidbody2D replacementBody = body.Body;
        replacementBody.position = (Vector2)respawnPosition;
        replacementBody.linearVelocity = Vector2.zero;
        replacementBody.angularVelocity = 0f;

        Physics2D.SyncTransforms();
        return true;
    }
}
