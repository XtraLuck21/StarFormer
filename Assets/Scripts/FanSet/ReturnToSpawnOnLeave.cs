using UnityEngine;

/// <summary>
/// When the player leaves this body, it snaps back to its spawn pose.
/// Intended for loaned forms such as Bubble and Spider (e.g. SpiderFanRoom).
/// </summary>
[RequireComponent(typeof(PossessableBody))]
public class ReturnToSpawnOnLeave : MonoBehaviour
{
    [SerializeField] private bool resetVelocity = true;
    [SerializeField] private bool consumeOnLeave;

    private PossessableBody possessable;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool wasControlled;

    private void Awake()
    {
        possessable = GetComponent<PossessableBody>();
        if (consumeOnLeave)
        {
            // Later-room forms were authored beneath this body; consuming it must not remove them.
            foreach (PossessableBody other in GetComponentsInChildren<PossessableBody>(true))
                if (other != possessable && other.transform.IsChildOf(transform))
                    other.transform.SetParent(transform.parent, true);
        }
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Update()
    {
        bool controlled = possessable.IsControlled;

        if (wasControlled && !controlled)
        {
            ReturnToSpawn();
        }

        wasControlled = controlled;
    }

    /// <summary>
    /// Snap this body back to its spawn pose and keep it active in the world.
    /// </summary>
    public void ReturnToSpawn()
    {
        if (consumeOnLeave)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        Rigidbody2D body = possessable.Body;

        wasControlled = false;

        if (body == null || !resetVelocity)
        {
            return;
        }

        body.position = spawnPosition;
        body.rotation = 0f;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }
}
