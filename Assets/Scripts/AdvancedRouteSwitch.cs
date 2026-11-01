using TMPro;
using UnityEngine;

// Optional, latched Box D switch for the upper return in Level01_Advanced.
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class AdvancedRouteSwitch : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody targetBox;
    [SerializeField] private GameObject routeA;
    [SerializeField] private GameObject routeB;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color idleColor = new Color32(255, 211, 77, 255);
    [SerializeField] private Color activatedColor = new Color32(102, 228, 218, 255);

    private BoxCollider2D sensor;
    private SpriteRenderer buttonRenderer;
    public bool IsActivated { get; private set; }

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        buttonRenderer = GetComponent<SpriteRenderer>();
        sensor.isTrigger = true;
        if (playerController == null || targetBox == null || routeA == null ||
            routeB == null || routeA == routeB ||
            transform.IsChildOf(routeA.transform) || transform.IsChildOf(routeB.transform))
        {
            Debug.LogError("Assign the player, Box D, and separate A/B route roots.", this);
            enabled = false;
            return;
        }
        routeA.SetActive(true);
        routeB.SetActive(false);
        buttonRenderer.color = idleColor;
        if (label != null) label.text = "save my life";
    }

    private void Update()
    {
        if (IsActivated || Time.timeScale <= 0f || playerController.IsDead ||
            !targetBox.isActiveAndEnabled || !targetBox.IsReady ||
            targetBox.Form != BodyForm.Box || !targetBox.IsControlled ||
            playerController.CurrentBody != targetBox)
            return;

        Rigidbody2D body = targetBox.Body;
        Collider2D boxCollider = targetBox.BodyCollider;
        if (!body.simulated || body.bodyType != RigidbodyType2D.Dynamic ||
            !boxCollider.enabled || !targetBox.IsGrounded() ||
            !sensor.IsTouching(boxCollider))
            return;

        // Require the top of the grounded box to reach the underside of the button.
        Bounds box = boxCollider.bounds;
        Bounds button = sensor.bounds;
        if (box.center.y >= button.min.y || box.max.y < button.min.y ||
            box.max.x <= button.min.x || box.min.x >= button.max.x)
            return;

        IsActivated = true;
        routeA.SetActive(false);
        routeB.SetActive(true);
        buttonRenderer.color = activatedColor;
        if (label != null) label.text = "life saved";
    }
}
