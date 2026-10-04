using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(
    typeof(Rigidbody2D),
    typeof(BoxCollider2D),
    typeof(PossessableBody))]
public class BoxResizeAbility : MonoBehaviour
{
    [SerializeField] private float minimumSize = 0.5f;
    [SerializeField] private float maximumSize = 2f;
    [SerializeField] private float resizeSpeed = 1f;

    private const float ContactTolerance = 0.005f;
    private const float SupportTolerance = 0.02f;
    private const int SearchIterations = 10;

    private PossessableBody possessable;
    private Rigidbody2D body;
    private BoxCollider2D boxCollider;

    private Vector3 originalScale;
    private float currentSize = 1f;
    private float resizeInput;

    private readonly List<Collider2D> overlaps =
        new List<Collider2D>(16);

    private void Awake()
    {
        possessable = GetComponent<PossessableBody>();
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        originalScale = transform.localScale;

        if (possessable.Form != BodyForm.Box ||
            transform.parent != null ||
            Mathf.Abs(Mathf.DeltaAngle(body.rotation, 0f)) > 0.01f ||
            originalScale.x <= 0f ||
            originalScale.y <= 0f ||
            boxCollider.edgeRadius > 0f)
        {
            Debug.LogError(
                "Box resizing requires an upright, unparented box " +
                "with positive scale and zero collider edge radius.",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        resizeInput = 0f;

        if (!possessable.IsControlled)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.wKey.isPressed)
        {
            resizeInput += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            resizeInput -= 1f;
        }
    }

    private void FixedUpdate()
    {
        if (!possessable.IsControlled || resizeInput == 0f)
        {
            return;
        }

        float requestedSize = Mathf.Clamp(
            currentSize + resizeInput * resizeSpeed * Time.fixedDeltaTime,
            minimumSize,
            maximumSize
        );

        if (Mathf.Approximately(requestedSize, currentSize))
        {
            return;
        }

        Physics2D.SyncTransforms();

        Bounds originalBounds = boxCollider.bounds;
        bool keepBottom = possessable.IsGrounded();

        // Shrinking stays inside the current occupied space.
        // Growing requires enough room for the proposed collider.
        if (requestedSize > currentSize &&
            !HasRoom(requestedSize, originalBounds, keepBottom))
        {
            float lower = currentSize;
            float upper = requestedSize;

            // Find the largest safe size near an obstacle.
            for (int i = 0; i < SearchIterations; i++)
            {
                float middle = (lower + upper) * 0.5f;

                if (HasRoom(middle, originalBounds, keepBottom))
                {
                    lower = middle;
                }
                else
                {
                    upper = middle;
                }
            }

            requestedSize = lower;
        }

        ApplySize(requestedSize, originalBounds, keepBottom);
    }

    private void GetProposedBounds(
        float targetSize,
        Bounds originalBounds,
        bool keepBottom,
        out Vector2 center,
        out Vector2 size)
    {
        float ratio = targetSize / currentSize;

        size = (Vector2)originalBounds.size * ratio;
        center = originalBounds.center;

        if (keepBottom)
        {
            center.y = originalBounds.min.y + size.y * 0.5f;
        }
    }

    private bool HasRoom(
        float targetSize,
        Bounds originalBounds,
        bool keepBottom)
    {
        GetProposedBounds(
            targetSize,
            originalBounds,
            keepBottom,
            out Vector2 center,
            out Vector2 size
        );

        // Allow normal surface contact without treating it as penetration.
        size.x = Mathf.Max(0.001f, size.x - ContactTolerance * 2f);
        size.y = Mathf.Max(0.001f, size.y - ContactTolerance * 2f);

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(
            Physics2D.GetLayerCollisionMask(gameObject.layer)
        );
        filter.useTriggers = false;

        overlaps.Clear();
        Physics2D.OverlapBox(center, size, 0f, filter, overlaps);

        foreach (Collider2D other in overlaps)
        {
            if (other.attachedRigidbody == body)
            {
                continue;
            }

            if (Physics2D.GetIgnoreCollision(boxCollider, other))
            {
                continue;
            }

            // A grounded resize preserves the bottom of the box.
            // Surfaces entirely below its feet must not block upward growth.
            bool isBelowFeet =
                other.bounds.max.y <= originalBounds.min.y + SupportTolerance;

            if (keepBottom && isBelowFeet)
            {
                continue;
            }

            return false;
        }
        
        return true;
    }

    private void ApplySize(
        float targetSize,
        Bounds originalBounds,
        bool keepBottom)
    {
        if (Mathf.Approximately(targetSize, currentSize))
        {
            return;
        }

        GetProposedBounds(
            targetSize,
            originalBounds,
            keepBottom,
            out Vector2 center,
            out Vector2 size
        );

        float ratio = targetSize / currentSize;

        // Account for a collider offset when positioning the resized body.
        Vector2 originalOffset =
            (Vector2)originalBounds.center - body.position;

        Vector2 newPosition = center - originalOffset * ratio;

        transform.localScale = new Vector3(
            originalScale.x * targetSize,
            originalScale.y * targetSize,
            originalScale.z
        );

        body.position = newPosition;
        currentSize = targetSize;

        Physics2D.SyncTransforms();
        body.WakeUp();
    }

    public void ResetSize()
    {
        currentSize = 1f;
        resizeInput = 0f;
        transform.localScale = originalScale;
    }

    private void OnValidate()
    {
        minimumSize = Mathf.Clamp(minimumSize, 0.1f, 1f);
        maximumSize = Mathf.Max(1f, maximumSize);
        resizeSpeed = Mathf.Max(0.01f, resizeSpeed);
    }
}