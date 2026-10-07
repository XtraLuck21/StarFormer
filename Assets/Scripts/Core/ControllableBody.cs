using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class ControllableBody : MonoBehaviour
{
    [SerializeField] private bool canBePossessed = true;
    [SerializeField] private bool hideAfterControlLost;
    [SerializeField] private GameObject controlMarker;

    private PlayerControlManager owner;

    public bool IsControlled => owner != null;
    public bool CanBePossessed => canBePossessed;
    public bool HideAfterControlLost => hideAfterControlLost;
    public Rigidbody2D Body { get; private set; }
    public Collider2D BodyCollider { get; private set; }

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        BodyCollider = GetComponent<Collider2D>();

        if (controlMarker != null)
        {
            controlMarker.SetActive(false);
        }
    }

    public void ConfigureBody(
        bool isPossessable,
        bool hideWhenControlEnds,
        GameObject marker)
    {
        canBePossessed = isPossessable;
        hideAfterControlLost = hideWhenControlEnds;
        controlMarker = marker;
    }

    internal void BeginControl(PlayerControlManager newOwner)
    {
        owner = newOwner;

        if (controlMarker != null)
        {
            controlMarker.SetActive(true);
        }

        OnControlGained();
    }

    internal void EndControl()
    {
        OnControlLost();
        owner = null;

        if (controlMarker != null)
        {
            controlMarker.SetActive(false);
        }
    }

    internal abstract void ReceiveInput(float horizontal, bool actionPressed);
    internal abstract void ControlFixedUpdate();

    protected virtual void OnControlGained()
    {
    }

    protected virtual void OnControlLost()
    {
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsControlled || owner == null)
        {
            return;
        }

        ControllableBody target =
            collision.collider.GetComponentInParent<ControllableBody>();

        if (target != null &&
            target != this &&
            target.CanBePossessed &&
            target.isActiveAndEnabled)
        {
            owner.RequestPossession(this, target);
        }
    }

    public void KillIfControlled()
    {
        if (IsControlled && owner != null)
        {
            owner.KillControlledBody(this);
        }
    }
}
