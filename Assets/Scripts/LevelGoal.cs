using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private GameObject requiredGate;
    [SerializeField] private TMP_Text statusText;

    private bool isCompleted;
    private float previousTimeScale;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (playerController == null ||
            requiredGate == null ||
            statusText == null)
        {
            Debug.LogError(
                "Assign the player controller, gate, and status text.",
                this
            );

            enabled = false;
            return;
        }

        statusText.text = "";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckGoal(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckGoal(other);
    }

    private void CheckGoal(Collider2D other)
    {
        if (!isActiveAndEnabled ||
            isCompleted ||
            playerController.IsDead ||
            Time.timeScale == 0f ||
            requiredGate.activeSelf)
        {
            return;
        }

        Rigidbody2D body = other.attachedRigidbody;

        if (body == null ||
            !body.TryGetComponent(out PossessableBody playerBody) ||
            !playerBody.IsControlled)
        {
            return;
        }

        isCompleted = true;

        // Show the actual contact position before pausing.
        Vector2 position = body.position;
        float rotation = body.rotation;

        body.interpolation = RigidbodyInterpolation2D.None;

        body.transform.SetPositionAndRotation(
            new Vector3(
                position.x,
                position.y,
                body.transform.position.z
            ),
            Quaternion.Euler(0f, 0f, rotation)
        );

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;

        statusText.text =
            "Level 1 complete!\nPress R to restart.";

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        if (isCompleted)
        {
            Time.timeScale = previousTimeScale;
        }
    }
}