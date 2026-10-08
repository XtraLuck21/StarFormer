using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BoxHeightSwitch : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private PossessableBody targetBox;

    [SerializeField] private Color inactiveColor =
        new Color32(255, 211, 77, 255);

    [SerializeField] private Color activatedColor =
        new Color32(102, 228, 218, 255);

    private BoxCollider2D sensor;
    private SpriteRenderer switchRenderer;
    private bool isActivated;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        switchRenderer = GetComponent<SpriteRenderer>();

        sensor.isTrigger = true;
        switchRenderer.color = inactiveColor;

        if (door == null || door == gameObject || targetBox == null)
        {
            Debug.LogError(
                "Assign a separate door and the target box.",
                this
            );

            enabled = false;
            return;
        }

        door.SetActive(true);
    }

    private void Update()
    {
        if (isActivated ||
            targetBox == null ||
            !targetBox.isActiveAndEnabled ||
            !targetBox.IsReady ||
            targetBox.Form != BodyForm.Box)
        {
            return;
        }

        Rigidbody2D body = targetBox.Body;
        Collider2D boxCollider = targetBox.BodyCollider;

        if (!body.simulated ||
            body.bodyType != RigidbodyType2D.Dynamic ||
            !boxCollider.enabled ||
            !targetBox.IsGrounded() ||
            !sensor.IsTouching(boxCollider))
        {
            return;
        }

        isActivated = true;
        switchRenderer.color = activatedColor;
        door.SetActive(false);

        Debug.Log("Height switch activated. G2 stays open.", this);
    }
}