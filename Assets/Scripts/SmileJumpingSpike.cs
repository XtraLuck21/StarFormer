using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SmileJumpingSpike : MonoBehaviour
{
    [SerializeField] private float period = 3.1f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private float leapHeight = .75f;
    [SerializeField] private float leapSeconds = .85f;
    [SerializeField] private SpriteRenderer visual;
    private Rigidbody2D body;
    private Vector2 rest;
    private float age;
    public bool IsLeaping { get; private set; }
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.useFullKinematicContacts = true;
        rest = body.position;
    }
    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f) return;
        age += Time.fixedDeltaTime;
        float t = Mathf.Repeat(age + phaseOffset, period);
        const float warning = .45f;
        IsLeaping = t >= warning && t < warning + leapSeconds;
        float height = IsLeaping ? leapHeight * Mathf.Sin((t - warning) / leapSeconds * Mathf.PI) : 0;
        body.MovePosition(rest + Vector2.up * height);
        if (visual != null) visual.color = t < warning
            ? Color.Lerp(new Color(1f,.35f,.4f), new Color(1f,.8f,.35f), t / warning)
            : new Color(1f,.35f,.4f);
    }
}
