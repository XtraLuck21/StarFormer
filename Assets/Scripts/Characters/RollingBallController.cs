using UnityEngine;

public class RollingBallController : ControllableBody
{
    [Header("Steering")]
    [SerializeField] private float groundAcceleration = 38f;
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float maximumHorizontalSpeed = 8.5f;
    [SerializeField] private float rollingTorque = 7f;

    [Header("Autonomous rolling")]
    [SerializeField] private float autonomousAcceleration = 3f;
    [SerializeField] private float autonomousMaximumSpeed = 3.2f;
    [SerializeField] private float autonomousTorque = 3f;

    [Header("Automatic bounce")]
    [SerializeField] private float bounceSpeed = 15.5f;
    [SerializeField] private float wallBounceSpeed = 6f;

    [Header("Visual feedback")]
    [SerializeField] private SpriteRenderer ballRenderer;
    [SerializeField] private Color idleColor = new Color(0.62f, 0.50f, 0.86f, 1f);
    [SerializeField] private Color activeColor = new Color(0.72f, 0.61f, 0.96f, 1f);

    private float horizontalInput;
    private bool bounceActive;
    private bool hasStartedBouncing;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    public bool BounceActive => bounceActive;

    public void ConfigureRenderer(SpriteRenderer renderer)
    {
        ballRenderer = renderer;
    }

    internal override void ReceiveInput(float horizontal, bool actionPressed)
    {
        horizontalInput = horizontal;
    }

    internal override void ControlFixedUpdate()
    {
        bool grounded = IsGrounded();
        float acceleration = grounded
            ? groundAcceleration
            : airAcceleration;

        Body.AddTorque(
            -horizontalInput * rollingTorque,
            ForceMode2D.Force
        );

        Vector2 velocity = Body.linearVelocity;

        // Possession itself never launches the ball. The player's first
        // steering input starts the reusable automatic-bounce cycle.
        if (!hasStartedBouncing &&
            grounded &&
            Mathf.Abs(horizontalInput) > 0.05f)
        {
            velocity.y = Mathf.Max(velocity.y, bounceSpeed);
            hasStartedBouncing = true;
        }

        float targetHorizontalSpeed =
            horizontalInput * maximumHorizontalSpeed;

        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetHorizontalSpeed,
            acceleration * Time.fixedDeltaTime
        );
        Body.linearVelocity = velocity;
    }

    protected override void OnControlGained()
    {
        bounceActive = true;
        hasStartedBouncing = false;
        SetVisualState();
        Body.WakeUp();
    }

    protected override void OnControlLost()
    {
        bounceActive = false;
        hasStartedBouncing = false;
        horizontalInput = 0f;
        SetVisualState();
    }

    private void FixedUpdate()
    {
        if (IsControlled || Body == null)
        {
            return;
        }

        Body.AddForce(
            Vector2.right * autonomousAcceleration,
            ForceMode2D.Force
        );
        Body.AddTorque(-autonomousTorque, ForceMode2D.Force);

        Vector2 velocity = Body.linearVelocity;
        velocity.x = Mathf.Clamp(
            velocity.x,
            -autonomousMaximumSpeed,
            autonomousMaximumSpeed
        );
        Body.linearVelocity = velocity;
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (!bounceActive ||
            !hasStartedBouncing ||
            !IsControlled ||
            collision.contactCount == 0)
        {
            return;
        }

        Vector2 normal = collision.GetContact(0).normal;

        for (int i = 1; i < collision.contactCount; i++)
        {
            Vector2 candidate = collision.GetContact(i).normal;

            if (candidate.y > normal.y)
            {
                normal = candidate;
            }
        }

        Vector2 velocity = Body.linearVelocity;
        float targetNormalSpeed = Mathf.Abs(normal.y) > 0.45f
            ? bounceSpeed
            : wallBounceSpeed;

        float currentNormalSpeed = Vector2.Dot(velocity, normal);

        if (currentNormalSpeed < targetNormalSpeed)
        {
            velocity += normal * (targetNormalSpeed - currentNormalSpeed);
            Body.linearVelocity = velocity;
        }
    }

    private bool IsGrounded()
    {
        int count = Body.GetContacts(contacts);

        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.55f)
            {
                return true;
            }
        }

        return false;
    }

    private void SetVisualState()
    {
        if (ballRenderer != null)
        {
            ballRenderer.color = bounceActive ? activeColor : idleColor;
        }
    }

    private void OnValidate()
    {
        groundAcceleration = Mathf.Max(0f, groundAcceleration);
        airAcceleration = Mathf.Max(0f, airAcceleration);
        maximumHorizontalSpeed = Mathf.Max(0.1f, maximumHorizontalSpeed);
        autonomousAcceleration = Mathf.Max(0f, autonomousAcceleration);
        autonomousMaximumSpeed = Mathf.Max(0.1f, autonomousMaximumSpeed);
        autonomousTorque = Mathf.Max(0f, autonomousTorque);
        bounceSpeed = Mathf.Max(0.1f, bounceSpeed);
        wallBounceSpeed = Mathf.Max(0.1f, wallBounceSpeed);
    }
}
