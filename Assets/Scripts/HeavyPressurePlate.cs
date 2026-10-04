using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HeavyPressurePlate : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private float requiredMass = 3f;

    [SerializeField] private Color inactiveColor =
        new Color(0.9f, 0.3f, 0.3f, 1f);

    [SerializeField] private Color activatedColor =
        new Color(0.3f, 1f, 0.5f, 1f);

    private BoxCollider2D sensor;
    private SpriteRenderer plateRenderer;

    private bool activationPending;
    private bool isActivated;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        plateRenderer = GetComponent<SpriteRenderer>();

        sensor.isTrigger = true;
        plateRenderer.color = inactiveColor;

        if (door == null || door == gameObject)
        {
            Debug.LogError(
                "Assign the separate TestDoor object to HeavyPressurePlate.",
                this
            );

            enabled = false;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActiveAndEnabled || isActivated || activationPending)
        {
            return;
        }

        Rigidbody2D incomingBody = other.attachedRigidbody;

        if (incomingBody == null ||
            incomingBody.bodyType != RigidbodyType2D.Dynamic ||
            incomingBody.mass < requiredMass)
        {
            return;
        }

        // Require a supporting contact beneath the body inside the plate area.
        // A heavy object passing through the sensor in midair is not enough.
        Bounds area = sensor.bounds;
        int contactCount = incomingBody.GetContacts(contacts);

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = contacts[i];

            if (contact.normal.y <= 0.65f)
            {
                continue;
            }

            Vector2 point = contact.point;
            const float tolerance = 0.03f;

            bool withinWidth =
                point.x >= area.min.x &&
                point.x <= area.max.x;

            bool nearSurface =
                point.y >= area.min.y - tolerance &&
                point.y <= area.max.y + tolerance;

            if (withinWidth && nearSurface)
            {
                activationPending = true;
                return;
            }
        }
    }

    private void Update()
    {
        if (!activationPending || isActivated)
        {
            return;
        }

        activationPending = false;
        isActivated = true;

        plateRenderer.color = activatedColor;
        door.SetActive(false);

        Debug.Log("Heavy pressure plate activated. Door stays open.", this);
    }
}