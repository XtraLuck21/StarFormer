using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PossessableBox : MonoBehaviour
{
    [SerializeField] private GameObject starMark;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float respawnHeight = -10f;

    private Rigidbody2D body;
    private Vector2 spawnPosition;
    private StarPlayerController pendingPlayer;

    private bool isControlled;
    private float horizontalInput;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spawnPosition = body.position;

        if (starMark == null || starMark == gameObject)
        {
            Debug.LogError(
                "Assign the StarMark child object to PossessableBox.",
                this
            );

            enabled = false;
            return;
        }

        starMark.SetActive(false);
    }

    private void Update()
    {
        horizontalInput = 0f;

        if (!isControlled || Keyboard.current == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }
    }

    private void FixedUpdate()
    {
        if (pendingPlayer != null)
        {
            if (pendingPlayer.isActiveAndEnabled)
            {
                TakeControl(pendingPlayer);
            }

            pendingPlayer = null;
        }

        if (!isControlled)
        {
            return;
        }

        // Keep the current form when resetting this test body.
        if (body.position.y < respawnHeight)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            return;
        }

        // The box can move along supporting surfaces but cannot steer in air.
        if (IsGrounded())
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = horizontalInput * moveSpeed;
            body.linearVelocity = velocity;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enabled || isControlled || pendingPlayer != null)
        {
            return;
        }

        Rigidbody2D incomingBody = collision.rigidbody;

        if (incomingBody == null)
        {
            return;
        }

        if (incomingBody.TryGetComponent(
                out StarPlayerController player)
            && player.isActiveAndEnabled)
        {
            // Process the transfer outside the collision callback.
            pendingPlayer = player;
        }
    }

    private void TakeControl(StarPlayerController player)
    {
        player.gameObject.SetActive(false);
        isControlled = true;
        starMark.SetActive(true);

        Debug.Log("Possessed box.", this);
    }

    private bool IsGrounded()
    {
        int contactCount = body.GetContacts(contacts);

        for (int i = 0; i < contactCount; i++)
        {
            if (contacts[i].normal.y > 0.65f)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        horizontalInput = 0f;
        pendingPlayer = null;
    }
}