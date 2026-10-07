using UnityEngine;

public class StarController : ControllableBody
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 10f;

    private float horizontalInput;
    private bool jumpRequested;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    internal override void ReceiveInput(float horizontal, bool actionPressed)
    {
        horizontalInput = horizontal;
        jumpRequested |= actionPressed;
    }

    internal override void ControlFixedUpdate()
    {
        Vector2 velocity = Body.linearVelocity;
        velocity.x = horizontalInput * moveSpeed;

        if (jumpRequested && velocity.y <= 0.1f && IsGrounded())
        {
            velocity.y = jumpSpeed;
        }

        Body.linearVelocity = velocity;
        jumpRequested = false;
    }

    private bool IsGrounded()
    {
        int count = Body.GetContacts(contacts);

        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.65f)
            {
                return true;
            }
        }

        return false;
    }
}
