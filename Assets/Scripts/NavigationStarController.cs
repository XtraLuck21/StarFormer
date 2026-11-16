using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
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

    [SerializeField] private GameObject keyboardHints;
    [SerializeField] private Transform touchHintAnchor;
    private MobileFormControls mobileControls;
    private GameObject touchHints;
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

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    public bool ControlsBlocked { get; private set; }
    public bool IsPreviewing { get; private set; }

    public void SetControlsBlocked(bool blocked)
    {
        ControlsBlocked = blocked;
        horizontal = 0f;
        jumpUntil = groundedUntil = -1f;
    }

    private bool PointerIsOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
        return uiHits.Count > 0;
    }
    public bool IsGrounded { get; private set; }
    public Rigidbody2D Body => body;

    private void Awake()
    {
        mobileControls = GetComponent<MobileFormControls>();
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
        bool touchMode = mobileControls != null && mobileControls.MobileMode;
        UpdateMovementHints(touchMode);
        if (IsPreviewing || ControlsBlocked) return;
        var keyboard = Keyboard.current;
        horizontal = keyboard == null ? 0f :
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        if (touchMode)
            horizontal = Mathf.Clamp(horizontal + mobileControls.Movement.x, -1f, 1f);
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) RequestJump();

        var mouse = Mouse.current;
        if (!touchMode && mouse != null && mouse.leftButton.wasPressedThisFrame && !PointerIsOverUI(mouse.position.ReadValue()))
            ActivateEntranceAt(mouse.position.ReadValue());
    }

    internal void RequestJump()
    {
        if (!IsPreviewing && !ControlsBlocked && isActiveAndEnabled)
            jumpUntil = Time.time + .12f;
    }

    internal void ActivateEntranceAt(Vector2 screenPosition)
    {
        if (!isActiveAndEnabled || IsPreviewing || ControlsBlocked || Camera.main == null) return;
        Vector2 point = Camera.main.ScreenToWorldPoint(screenPosition);
        Physics2D.OverlapPoint(point)?.GetComponent<NavigationEntrance>()?.ActivateFromPointer(this, point);
    }

    private void UpdateMovementHints(bool touchMode)
    {
        if (keyboardHints == null || touchHintAnchor == null) return;
        if (touchMode && touchHints == null)
        {
            touchHints = new GameObject("Touch movement hints");
            touchHints.transform.SetParent(keyboardHints.transform.parent, false);
            touchHints.transform.position = touchHintAnchor.position;
            const float moveX = -0.78f;
            const float jumpX = 0.78f;
            MobileControlGraphic.CreateStickHint(touchHints.transform, new Vector2(moveX, 1.15f), 30);
            MobileControlGraphic.CreateWorldHint(touchHints.transform, MobileControlGraphic.Symbol.Jump,
                new Vector2(jumpX, 1.15f), 0.88f, 30);
            AddTouchHintLabel("Move", moveX);
            AddTouchHintLabel("Jump", jumpX);
        }
        if (keyboardHints.activeSelf == touchMode) keyboardHints.SetActive(!touchMode);
        if (touchHints != null && touchHints.activeSelf != touchMode) touchHints.SetActive(touchMode);
    }

    private void AddTouchHintLabel(string action, float x)
    {
        var label = new GameObject(action, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        label.transform.SetParent(touchHints.transform, false);
        label.rectTransform.localPosition = new Vector3(x, 0.42f, 0f);
        label.rectTransform.sizeDelta = new Vector2(1.5f, 0.6f);
        label.fontSize = 3.8f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(32, 55, 72, 255);
        label.text = action;
        label.renderer.sortingOrder = 30;
        label.raycastTarget = false;
    }

    private void FixedUpdate()
    {
        IsGrounded = false;
        int count = shape.Cast(Vector2.down, groundFilter, groundHits, .07f);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > .65f) IsGrounded = true;

        if (IsPreviewing || ControlsBlocked) return;
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
        if (IsPreviewing || ControlsBlocked) return;
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
        // The entrance owns loading; the player holds still until the new scene takes over.
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
        ControlsBlocked = false;
        if (enteringOverlay != null) enteringOverlay.alpha = 0f;
        if (starVisual != null) starVisual.SetPropertyBlock(null);
        if (body != null) { body.simulated = true; body.linearVelocity = Vector2.zero; }
        IsPreviewing = false;
    }
}
