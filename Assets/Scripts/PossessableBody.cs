using UnityEngine;

public enum BodyForm
{
    Star = 0,
    Box = 1,
    Bubble = 2,
    SpikeWall = 3,
    Spider = 4,
    Fan = 5,
    gravityBall = 6,
    BouncyBall = 7
}

[RequireComponent(typeof(Rigidbody2D))]
public class PossessableBody : MonoBehaviour
{
    [SerializeField] private BodyForm form;
    [SerializeField] private GameObject controlVisual;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 10f;

    public BodyForm Form => form;
    public float MoveSpeed => moveSpeed;
    public float JumpSpeed => jumpSpeed;
    public bool IsControlled => owner != null;

    /// <summary>
    /// The player controller owning this body, or null when it is not possessed.
    /// Ability scripts, such as box resizing, use it to access shared input values.
    /// </summary>
    public PlayerPossessionController Owner => owner;

    public bool IsInWater
    {
        get
        {
            if (!isActiveAndEnabled || Body == null)
            {
                return false;
            }

            if (form == BodyForm.BouncyBall && BodyCollider != null)
            {
                return WaterZone.TryGetSurfaceY(
                    BodyCollider.bounds,
                    out _
                );
            }

            return WaterZone.Contains(Body.position);
        }
    }

    public Rigidbody2D Body { get; private set; }
    public Collider2D BodyCollider { get; private set; }

    public bool IsReady =>
        Body != null &&
        BodyCollider != null &&
        controlVisual != null &&
        controlVisual != gameObject;

    private PlayerPossessionController owner;

    private readonly ContactPoint2D[] contacts =
        new ContactPoint2D[16];

    private bool wasInWater;
    private bool bubbleActivated;
    private RigidbodyConstraints2D bubbleActiveConstraints;

    private void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        BodyCollider = GetComponent<Collider2D>();

        if (!IsReady)
        {
            Debug.LogError(
                "PossessableBody requires a collider and a separate control visual.",
                this
            );

            enabled = false;
            return;
        }

        controlVisual.SetActive(false);

        if (form == BodyForm.Bubble)
        {
            // Keep an untouched bubble at its authored position while preserving
            // its dynamic collider for contact possession and trigger interactions.
            bubbleActiveConstraints = Body.constraints;
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
            Body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    public void SetOwner(PlayerPossessionController newOwner)
    {
        owner = newOwner;

        if (owner != null && form == BodyForm.Bubble && !bubbleActivated)
        {
            // Activation lasts for this bubble's lifetime, including after release.
            // A scene restart or a newly spawned bubble starts dormant again.
            bubbleActivated = true;
            Body.constraints = bubbleActiveConstraints;
            Body.WakeUp();
        }

        controlVisual.SetActive(owner != null);

        if (TryGetComponent(out BodyWaterPhysics waterPhysics))
        {
            waterPhysics.RefreshWaterPhysics();
        }
    }

    public bool IsGrounded()
    {
        if (Body == null)
        {
            return false;
        }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RequestPossessionFromBody(collision.rigidbody);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        RequestPossessionFromBody(collision.rigidbody);
    }

    // Called by Spider surface detection.
    public void RequestSurfacePossession(Collider2D surface)
    {
        if (surface == null)
        {
            return;
        }

        RequestPossessionFromBody(surface.attachedRigidbody);
    }

    private void RequestPossessionFromBody(Rigidbody2D targetBody)
    {
        if (!isActiveAndEnabled ||
            !IsReady ||
            owner == null ||
            targetBody == null ||
            targetBody == Body)
        {
            return;
        }

        // Ground and obstacles without PossessableBody cannot trigger possession.
        if (!targetBody.TryGetComponent<PossessableBody>(
                out var target) ||
            target == this ||
            !target.isActiveAndEnabled ||
            !target.IsReady)
        {
            return;
        }

        // The controller still manages transfers and contact locks.
        owner.RequestPossession(this, target);
    }

    private void FixedUpdate()
    {
        if (!IsReady)
        {
            return;
        }

        bool inWater = IsInWater;

        if (inWater == wasInWater)
        {
            return;
        }

        wasInWater = inWater;
        LogWaterState(inWater);
    }

    private void LogWaterState(bool inWater)
    {
        string state = inWater ? "entered water" : "left water";
        Debug.Log($"{name} ({form}) {state}.", this);
    }

    private void OnDisable()
    {
        wasInWater = false;
    }
}