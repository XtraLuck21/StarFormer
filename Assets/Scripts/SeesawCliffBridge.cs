using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class SeesawCliffBridge : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField, Min(.1f)] private float fullWidth = 6.1f;
    [SerializeField, Min(.1f)] private float height = .35f;
    [SerializeField, Min(.1f)] private float extensionDuration = 1f;
    [SerializeField] private bool growRight;
    public bool IsExtended { get; private set; }
    public bool IsExtending { get; private set; }
    private Rigidbody2D body;
    private BoxCollider2D surface;
    private Vector3 anchor;
    private float elapsed;
    private bool initialized;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        surface = GetComponent<BoxCollider2D>();
        surface.isTrigger = false;
        anchor = transform.localPosition;
        SetWidth(.04f, false);
    }

    public void Extend()
    {
        if (!IsExtended && !IsExtending) IsExtending = true;
    }

    public void SetRevealProgress(float progress)
    {
        Initialize();
        float t = Mathf.Clamp01(progress);
        IsExtending = false;
        IsExtended = t >= 1f;
        elapsed = t * extensionDuration;
        surface.enabled = t > .001f;
        visual.gameObject.SetActive(t > .001f);
        SetWidth(Mathf.Lerp(.04f, fullWidth, t), false);
    }

    private void FixedUpdate()
    {
        if (!IsExtending || Time.timeScale <= 0f) return;
        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed / extensionDuration);
        SetWidth(Mathf.Lerp(.04f, fullWidth, Mathf.SmoothStep(0f, 1f, t)), true);
        if (t >= 1f) { IsExtended = true; IsExtending = false; }
    }

    private void SetWidth(float width, bool move)
    {
        surface.size = new Vector2(width, height);
        visual.localPosition = Vector3.zero;
        visual.localScale = new Vector3(width, height, 1f);
        Vector3 localCenter = anchor + Vector3.right * (width * .5f) * (growRight ? 1f : -1f);
        Vector3 worldCenter = transform.parent != null ? transform.parent.TransformPoint(localCenter) : localCenter;
        if (move) body.MovePosition(worldCenter);
        else body.position = worldCenter;
    }
}
