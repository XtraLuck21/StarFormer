using UnityEngine;

public enum BodyForm
{
    Star,
    Box,
    Bubble,
    Spider
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
    public bool IsInWater =>
        isActiveAndEnabled &&
        Body != null &&
        WaterZone.Contains(Body.position);

    public Rigidbody2D Body { get; private set; }
    public Collider2D BodyCollider { get; private set; }

    public bool IsReady =>
        Body != null &&
        BodyCollider != null &&
        controlVisual != null &&
        controlVisual != gameObject;

    private PlayerPossessionController owner;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private bool wasInWater;

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
    }

    public void SetOwner(PlayerPossessionController newOwner)
    {
        owner = newOwner;
        controlVisual.SetActive(owner != null);

        if (TryGetComponent(out BodyWaterPhysics waterPhysics))
        {
            waterPhysics.RefreshWaterPhysics();
        }
    }

    public bool IsGrounded()
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActiveAndEnabled || owner == null)
        {
            return;
        }

        Rigidbody2D incomingBody = collision.rigidbody;

        if (incomingBody != null &&
            incomingBody.TryGetComponent(out PossessableBody target))
        {
            owner.RequestPossession(this, target);
        }
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