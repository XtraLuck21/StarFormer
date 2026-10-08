using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HeldPressurePlate : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private PossessableBody targetBox;

    [SerializeField] private Color inactiveColor =
        new Color32(203, 101, 115, 255);

    [SerializeField] private Color activatedColor =
        new Color32(102, 228, 218, 255);

    private BoxCollider2D sensor;
    private SpriteRenderer plateRenderer;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        plateRenderer = GetComponent<SpriteRenderer>();
        sensor.isTrigger = true;

        if (door == null || door == gameObject || targetBox == null)
        {
            Debug.LogError(
                "Assign a separate door and the target box.",
                this
            );

            enabled = false;
            return;
        }

        SetPressed(false);
    }

    private void Update()
    {
        SetPressed(IsBoxPressing());
    }

    private bool IsBoxPressing()
    {
        if (targetBox == null ||
            !targetBox.isActiveAndEnabled ||
            !targetBox.IsReady ||
            targetBox.Form != BodyForm.Box)
        {
            return false;
        }

        Rigidbody2D body = targetBox.Body;
        Collider2D boxCollider = targetBox.BodyCollider;

        if (!body.simulated ||
            body.bodyType != RigidbodyType2D.Dynamic ||
            !boxCollider.enabled ||
            !sensor.IsTouching(boxCollider) ||
            !targetBox.IsGrounded())
        {
            return false;
        }

        Bounds area = sensor.bounds;
        Bounds box = boxCollider.bounds;

        // Partial coverage is enough, but the box must rest
        // at the plate's height rather than touch it from the side.
        const float tolerance = 0.03f;

        bool overlapsHorizontally =
            box.max.x > area.min.x &&
            box.min.x < area.max.x;

        bool feetAtPlate =
            box.min.y >= area.min.y - tolerance &&
            box.min.y <= area.max.y + tolerance;

        return overlapsHorizontally && feetAtPlate;
    }

    private void SetPressed(bool pressed)
    {
        if (plateRenderer != null)
        {
            plateRenderer.color =
                pressed ? activatedColor : inactiveColor;
        }

        if (door != null &&
            door != gameObject &&
            door.activeSelf == pressed)
        {
            door.SetActive(!pressed);
        }
    }

    private void OnDisable()
    {
        SetPressed(false);
    }
}