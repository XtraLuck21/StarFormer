using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Deliberately independent of gameplay possession, checkpoints and analytics.
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class NavigationStarController : MonoBehaviour
{
    [SerializeField] private Renderer starVisual;
    [SerializeField] private CanvasGroup enteringOverlay;
    [SerializeField] private TMP_Text enteringLabel;
    [SerializeField] private float moveSpeed = 7.2f;
    [SerializeField] private float jumpSpeed = 13.2f;
    [SerializeField] private float gravityScale = 3.4f;

    private Rigidbody2D body;
    private CapsuleCollider2D shape;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private ContactFilter2D groundFilter;
    private MaterialPropertyBlock tint;
    private Color starColor;
    private Vector2 lastSafePosition;
    private float horizontal;
    private float jumpUntil = -1f;
    private float groundedUntil = -1f;
    private Vector2 approachVelocity;

    public bool IsPreviewing { get; private set; }
    public bool IsGrounded { get; private set; }
    public Rigidbody2D Body => body;

    private void Awake()
    {
        tint = new MaterialPropertyBlock();
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<CapsuleCollider2D>();
        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        groundFilter = new ContactFilter2D { useTriggers = false };
        lastSafePosition = body.position;
        starColor = starVisual.sharedMaterial.color;
        enteringOverlay.alpha = 0f;
        enteringOverlay.blocksRaycasts = false;
        enteringOverlay.interactable = false;
    }

    private void Update()
    {
        if (IsPreviewing) return;
        var keyboard = Keyboard.current;
        horizontal = keyboard == null ? 0f :
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            jumpUntil = Time.time + .12f;
    }

    private void FixedUpdate()
    {
        IsGrounded = false;
        int count = shape.Cast(Vector2.down, groundFilter, groundHits, .07f);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > .65f) IsGrounded = true;

        if (IsPreviewing) return;
        if (body.position.y < 4.3f || body.position.x < 1f || body.position.x > 31f)
        {
            ReturnToRoute(lastSafePosition);
            return;
        }
        if (IsGrounded && body.linearVelocity.y <= .1f)
        {
            groundedUntil = Time.time + .08f;
            float x = body.position.x;
            bool safelyOnRoute = (x > 2.3f && x < 16.8f) || (x > 19.2f && x < 21.8f) ||
                (x > 24.2f && x < 29.7f);
            if (safelyOnRoute && Mathf.Abs(shape.bounds.min.y - 10f) < .15f)
                lastSafePosition = body.position + Vector2.up * .08f;
        }
        var velocity = body.linearVelocity;
        velocity.x = horizontal * moveSpeed;
        if (jumpUntil >= Time.time && groundedUntil >= Time.time)
        {
            velocity.y = jumpSpeed;
            jumpUntil = groundedUntil = -1f;
        }
        body.linearVelocity = velocity;
        approachVelocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsPreviewing) return;
        var entrance = collision.collider.GetComponent<NavigationEntrance>();
        if (entrance == null) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (entrance.TryActivate(this, approachVelocity.y, contact.normal.y, contact.point))
                break;
        }
    }

    public void BeginPreview(bool hitFromBelow)
    {
        IsPreviewing = true;
        horizontal = 0f;
        jumpUntil = groundedUntil = -1f;
        body.linearVelocity = new Vector2(0f, hitFromBelow ? -4.5f : 6.4f);
    }

    public void SetFlash(float amount)
    {
        tint.SetColor("_Color", Color.Lerp(starColor, Color.white, amount));
        starVisual.SetPropertyBlock(tint);
    }

    public void ShowEntering(string destination, float alpha)
    {
        // Preview only: intentionally no SceneManager or persistent progression calls.
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        enteringLabel.text = "Entering " + destination + "...";
        enteringOverlay.alpha = alpha;
    }

    public void ReturnToRoute(Vector2 position)
    {
        enteringOverlay.alpha = 0f;
        body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        body.linearVelocity = Vector2.zero;
        body.simulated = true;
        horizontal = 0f;
        jumpUntil = groundedUntil = -1f;
        SetFlash(0f);
        IsPreviewing = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        horizontal = 0f;
        jumpUntil = -1f;
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void OnDisable()
    {
        if (enteringOverlay != null) enteringOverlay.alpha = 0f;
        if (starVisual != null) starVisual.SetPropertyBlock(null);
        if (body != null) { body.simulated = true; body.linearVelocity = Vector2.zero; }
        IsPreviewing = false;
    }
}
