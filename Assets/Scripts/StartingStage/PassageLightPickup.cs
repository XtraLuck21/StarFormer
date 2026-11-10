using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class PassageLightPickup : MonoBehaviour
{
    [SerializeField] private DarkStarPassage passage;
    [SerializeField] private SpriteRenderer halo;
    [SerializeField, Min(0.01f)] private float absorbDuration = 0.3f;
    [SerializeField, Min(0f)] private float bobHeight = 0.06f;
    [SerializeField] private float pulsePhase;

    private SpriteRenderer core;
    private CircleCollider2D pickupCollider;
    private Vector3 restingPosition;
    private Vector3 coreScale;
    private Vector3 haloScale;
    private Vector3 absorbStart;
    private PossessableBody recipient;
    private float absorbAge;
    private bool absorbing;
    private bool consumed;

    public bool IsConsumed => consumed;

    private void Awake()
    {
        core = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        restingPosition = transform.position;
        coreScale = transform.localScale;
        if (halo != null)
        {
            haloScale = halo.transform.localScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void OnEnable()
    {
        if (halo != null && !consumed)
        {
            halo.enabled = true;
        }
    }

    private void TryCollect(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (absorbing || consumed || passage == null || body == null ||
            !body.TryGetComponent<PossessableBody>(out var star) || !passage.CanCollect(star))
        {
            return;
        }
        absorbing = true;
        recipient = star;
        absorbStart = transform.position;
        absorbAge = 0f;
        pickupCollider.enabled = false;
    }

    private void Update()
    {
        if (consumed)
        {
            return;
        }
        if (absorbing)
        {
            if (passage == null || !passage.CanCollect(recipient))
            {
                CancelAbsorption();
                return;
            }
            absorbAge += Time.deltaTime;
            float t = Mathf.Clamp01(absorbAge / absorbDuration);
            Vector3 target = recipient.transform.position;
            transform.position = Vector3.Lerp(absorbStart, target, t * t) +
                Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.16f);
            transform.localScale = coreScale * Mathf.Lerp(1f, 0.12f, t);
            core.color = new Color(1f, 0.9f, 0.6f, 1f - t * 0.3f);
            if (halo != null)
            {
                halo.transform.position = transform.position;
                halo.transform.localScale = haloScale * Mathf.Lerp(1f, 0.25f, t);
                halo.color = new Color(1f, 0.75f, 0.3f, 0.3f * (1f - t));
            }
            if (t >= 1f)
            {
                if (passage.TryAbsorbLight(this, recipient))
                {
                    consumed = true;
                    gameObject.SetActive(false);
                }
                else
                {
                    CancelAbsorption();
                }
            }
            return;
        }

        float wave = Time.time * 2.3f - pulsePhase;
        float pulse = 0.78f + 0.22f * Mathf.Sin(wave);
        transform.position = restingPosition + Vector3.up * (Mathf.Sin(wave) * bobHeight);
        core.color = new Color(1f, 0.83f, 0.45f, 0.85f + pulse * 0.15f);
        if (halo != null)
        {
            halo.transform.position = transform.position;
            halo.color = new Color(1f, 0.68f, 0.24f, pulse * 0.28f);
        }
    }

    private void CancelAbsorption()
    {
        absorbing = false;
        recipient = null;
        transform.position = restingPosition;
        transform.localScale = coreScale;
        pickupCollider.enabled = true;
        if (halo != null)
        {
            halo.transform.localScale = haloScale;
        }
    }

    private void OnDisable()
    {
        if (halo != null)
        {
            halo.enabled = false;
        }
    }
}
