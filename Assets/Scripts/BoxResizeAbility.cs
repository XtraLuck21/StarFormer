using System.Collections.Generic;
using UnityEngine;

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

    private readonly List<Collider2D> overlaps =
        new List<Collider2D>(16);
    private readonly List<Collider2D> growthContacts =
        new List<Collider2D>(16);

    private void Awake()
    {
        possessable = GetComponent<PossessableBody>();
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        // Dynamic hosts must scale independently, even when grouped in a scene.
        // Detach before capturing the baseline scale, preserving world pose and size.
        if (possessable.Form == BodyForm.Box && transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        originalScale = transform.localScale;

        var problems = new List<string>();
        if (possessable.Form != BodyForm.Box)
            problems.Add($"Form is {possessable.Form}; expected Box");
        if (Mathf.Abs(Mathf.DeltaAngle(body.rotation, 0f)) > 0.01f)
            problems.Add($"world rotation Z is {body.rotation}; expected 0 degrees");
        if (originalScale.x <= 0f || originalScale.y <= 0f)
            problems.Add($"scale is {originalScale}; X and Y must be positive");
        if (boxCollider.edgeRadius > 0f)
            problems.Add($"BoxCollider2D Edge Radius is {boxCollider.edgeRadius}; expected 0");

        if (problems.Count > 0)
        {
            Debug.LogError(
                $"Box resizing disabled on '{name}' in scene '{gameObject.scene.name}': " +
                string.Join("; ", problems) + ".",
                this
            );

            enabled = false;
        }
    }

    // The controller captures grounding before resizing invalidates contacts,
    // then applies movement to the resized body in the same physics step.
    public void Tick(bool isGrounded)
    {
        if (!isActiveAndEnabled || !possessable.IsControlled)
        {
            return;
        }

        // The controller combines J/K and the on-screen Grow/Shrink buttons
        // into one VerticalInput value (-1 to 1); use it directly here.
        // Desktop and mobile share the same input path, so resizing still works
        // when Keyboard.current is null on a mobile device.
        PlayerPossessionController owner = possessable.Owner;

        if (owner == null)
        {
            return;
        }

        // Read the current owner's input in the physics step. A cached Update
        // value can resize a newly entered host after TransferTo clears input.
        float resizeInput = Mathf.Clamp(owner.VerticalInput, -1f, 1f);
        if (resizeInput == 0f)
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
        bool keepBottom = isGrounded;
        growthContacts.Clear();

        // Shrinking stays inside the current occupied space.
        // Growing requires enough room for the proposed collider.
        if (requestedSize > currentSize &&
            !HasRoom(requestedSize, originalBounds, keepBottom))
        {
            growthContacts.AddRange(overlaps);
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

        // A safe resize stops just before contact. Report a reached host even
        // when no collision callback is generated for that small clearance.
        foreach (Collider2D other in growthContacts)
        {
            if (other == null || other.attachedRigidbody == body ||
                Physics2D.GetIgnoreCollision(boxCollider, other)) continue;

            ColliderDistance2D distance = Physics2D.Distance(boxCollider, other);
            if (distance.isValid && distance.distance <= ContactTolerance * 2f)
            {
                possessable.RequestSurfacePossession(other);
            }
        }
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

        // Keep clearance instead of allowing a small penetration. Repeated J
        // presses otherwise overlap a neighbour anew and push it every step.
        size.x = Mathf.Max(0.001f, size.x + ContactTolerance * 2f);
        size.y = Mathf.Max(0.001f, size.y + ContactTolerance * 2f);

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

    public void RestoreSceneEntrySize(float size)
    {
        Physics2D.SyncTransforms();
        ApplySize(Mathf.Clamp(size, minimumSize, maximumSize), boxCollider.bounds, false);
    }

    private void OnValidate()
    {
        minimumSize = Mathf.Clamp(minimumSize, 0.1f, 1f);
        maximumSize = Mathf.Max(1f, maximumSize);
        resizeSpeed = Mathf.Max(0.01f, resizeSpeed);
    }
}
