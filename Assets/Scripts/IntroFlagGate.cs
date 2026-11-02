using UnityEngine;

// Reaching the flag opens a route; scene transitions belong beyond the door.
[RequireComponent(typeof(BoxCollider2D))]
public class IntroFlagGate : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody requiredBody;
    [SerializeField] private GameObject exitDoor;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (playerController == null || requiredBody == null || exitDoor == null)
        {
            Debug.LogError("IntroFlagGate requires a player controller, required body, and exit door.", this);
            enabled = false;
            return;
        }

        if (transform.IsChildOf(exitDoor.transform))
        {
            Debug.LogError("Keep the flag trigger separate from the door it opens.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryOpen(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryOpen(other);
    }

    private void TryOpen(Collider2D other)
    {
        if (!isActiveAndEnabled || IsOpen || Time.timeScale <= 0f ||
            playerController == null || playerController.IsDead ||
            requiredBody == null || exitDoor == null ||
            playerController.CurrentBody != requiredBody)
        {
            return;
        }

        Rigidbody2D touchingBody = other.attachedRigidbody;
        if (touchingBody == null || touchingBody != requiredBody.Body)
        {
            return;
        }

        Open();
    }

    public void Open()
    {
        if (exitDoor == null) return;
        IsOpen = true;
        exitDoor.SetActive(false);
    }
}
