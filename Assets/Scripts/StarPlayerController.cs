using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class StarPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float respawnHeight = -10f;

    private Rigidbody2D body;
    private Vector2 spawnPosition;

    private float horizontalInput;
    private bool jumpRequested;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spawnPosition = body.position;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        horizontalInput = 0f;

        if (keyboard == null)
        {
            jumpRequested = false;
            return;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (body.position.y < respawnHeight)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            jumpRequested = false;
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.x = horizontalInput * moveSpeed;

        if (jumpRequested && velocity.y <= 0.1f && IsGrounded())
        {
            velocity.y = jumpSpeed;
        }

        body.linearVelocity = velocity;
        jumpRequested = false;
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
        jumpRequested = false;
    }
}