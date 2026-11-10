using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawSpiderCeilingButton : MonoBehaviour
{
    [SerializeField] private PossessableBody targetSpider;
    [SerializeField] private BoxCollider2D ceiling;
    [SerializeField] private UnityEvent onPressed = new UnityEvent();
    public bool IsActivated { get; private set; }
    private BoxCollider2D sensor;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        sensor.isTrigger = true;
    }

    private void Update()
    {
        if (IsActivated || targetSpider == null || ceiling == null ||
            !targetSpider.isActiveAndEnabled || !targetSpider.IsControlled ||
            targetSpider.Form != BodyForm.Spider || !targetSpider.IsReady ||
            !targetSpider.TryGetComponent(out HexagonSurfaceMovement movement) ||
            !movement.IsAttached || movement.AttachedSurface != ceiling ||
            !targetSpider.Body.simulated || !targetSpider.BodyCollider.enabled ||
            !sensor.IsTouching(targetSpider.BodyCollider)) return;

        // The attached ceiling must be above the spider, not a wall or its top face.
        Bounds body = targetSpider.BodyCollider.bounds;
        if (body.center.y >= ceiling.bounds.min.y ||
            Mathf.Abs(body.max.y - ceiling.bounds.min.y) > .15f) return;

        RestoreActivated();
    }

    public void RestoreActivated()
    {
        if (IsActivated) return;

        IsActivated = true;
        sensor.enabled = false;
        try { onPressed.Invoke(); }
        finally { gameObject.SetActive(false); }
    }
}
