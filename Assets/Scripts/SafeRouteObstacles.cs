using System;
using UnityEngine;

// SAFE panels keep their authored size and translate vertically.
// A reveal-only mask hides the entrance animation below the shared floor.
public sealed class SafeRouteObstacles : MonoBehaviour
{
    [Serializable]
    private sealed class Baffle
    {
        public BoxCollider2D collider;
        public bool moving;
        public float minimumBottom = 11.25f;
        public float maximumBottom = 12.55f;
        public bool startHigh;
        [NonSerialized] public float bottom;
        [NonSerialized] public float x;
        [NonSerialized] public Vector3 scale;
        [NonSerialized] public SpriteRenderer renderer;
    }

    [SerializeField] private Baffle[] baffles;
    [SerializeField] private float floorY = 11.25f;
    [SerializeField] private SpriteMask revealMask;
    [SerializeField, Min(0f)] private float bodyClearance = .06f;
    [SerializeField, Min(.1f)] private float travelSeconds = 1.5f;
    [SerializeField, Min(0f)] private float holdSeconds = .75f;
    private float progress;
    private float wait;
    private bool towardHigh;

    // Show both exchanges and an endpoint pause before returning the camera.
    public float MinimumPreviewSeconds => travelSeconds * 2f + holdSeconds * 3f;

    private readonly Collider2D[] overlaps = new Collider2D[32];
    private bool initialized;
    private bool playing;
    private bool demonstrating;

    private void Initialize()
    {
        if (initialized) return;
        foreach (Baffle b in baffles)
        {
            b.x = b.collider.transform.position.x;
            b.scale = b.collider.transform.localScale;
            b.renderer = b.collider.GetComponent<SpriteRenderer>();
        }
        initialized = true;
    }

    private void ResetMotion()
    {
        Initialize();
        progress = 0f;
        towardHigh = true;
        wait = holdSeconds;
        foreach (Baffle b in baffles) b.bottom = BottomAt(b, progress);
    }

    public void BeginReveal()
    {
        ResetMotion();
        playing = demonstrating = false;
        foreach (Baffle b in baffles) b.collider.enabled = false;
        SetRevealAmount(0f);
    }

    public void SetRevealAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);
        revealMask.enabled = amount < 1f;
        foreach (Baffle b in baffles) ApplyGeometry(b, amount);
    }

    public void BeginDemonstration()
    {
        SetRevealAmount(1f);
        demonstrating = true;
    }

    public void FinishDemonstration()
    {
        demonstrating = false;
        playing = true;
        foreach (Baffle b in baffles) b.collider.enabled = true;
        Physics2D.SyncTransforms();
    }

    // Checkpoint recovery deliberately restarts a predictable motion cycle.
    public void RestoreLayout()
    {
        ResetMotion();
        SetRevealAmount(1f);
        FinishDemonstration();
    }

    private void Update()
    {
        // Only the explicit route demonstration moves while gameplay is paused.
        if (demonstrating) Advance(Time.unscaledDeltaTime, false);
    }

    private void FixedUpdate()
    {
        if (playing && !demonstrating) Advance(Time.fixedDeltaTime, true);
    }

    private static float BottomAt(Baffle b, float phase)
    {
        return Mathf.Lerp(b.minimumBottom, b.maximumBottom, b.startHigh ? 1f - phase : phase);
    }

    private void Advance(float deltaTime, bool protectBodies)
    {
        // One shared phase keeps unequal travel ranges in opposite directions.
        while (deltaTime > 0f)
        {
            float pause = Mathf.Min(wait, deltaTime);
            wait -= pause;
            deltaTime -= pause;
            if (deltaTime <= 0f) break;

            float target = towardHigh ? 1f : 0f;
            float stepTime = Mathf.Min(deltaTime, Mathf.Abs(target - progress) * travelSeconds);
            float nextProgress = Mathf.MoveTowards(progress, target, stepTime / travelSeconds);
            if (protectBodies)
            {
                // Check both leading edges before moving either board. If one is
                // occupied, pause the pair without advancing their shared phase.
                foreach (Baffle b in baffles)
                    if (b.moving && MovementWouldTouchBody(b, BottomAt(b, nextProgress))) return;
            }

            progress = nextProgress;
            foreach (Baffle b in baffles)
            {
                if (!b.moving) continue;
                b.bottom = BottomAt(b, progress);
                ApplyGeometry(b, 1f);
            }
            Physics2D.SyncTransforms();
            deltaTime -= stepTime;
            if (Mathf.Abs(progress - target) < .00001f)
            {
                towardHigh = !towardHigh;
                wait = holdSeconds;
            }
        }
    }

    private bool MovementWouldTouchBody(Baffle b, float next)
    {
        bool movingUp = next > b.bottom;
        float edgeOffset = movingUp ? b.scale.y : 0f;
        float edge = b.bottom + edgeOffset;
        float newEdge = next + edgeOffset;
        Vector2 center = new(b.x, (edge + newEdge) * .5f);
        Vector2 size = new(Mathf.Abs(b.scale.x) + bodyClearance * 2f,
            Mathf.Abs(newEdge - edge) + bodyClearance * 2f);
        var filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.OverlapBox(center, size, 0f, filter, overlaps);
        if (count == overlaps.Length) return true;
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D other = overlaps[i].attachedRigidbody;
            if (other != null && other.TryGetComponent(out PossessableBody body) &&
                body.isActiveAndEnabled) return true;
        }
        return false;
    }

    private void ApplyGeometry(Baffle b, float reveal)
    {
        Transform panel = b.collider.transform;
        float bottom = Mathf.Lerp(floorY - b.scale.y - .02f, b.bottom, reveal);
        panel.position = new Vector3(b.x, bottom + b.scale.y * .5f, panel.position.z);
        panel.localScale = b.scale;
        b.renderer.maskInteraction = reveal < 1f
            ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
        b.renderer.enabled = reveal > 0f;
    }

    private void OnDisable()
    {
        playing = demonstrating = false;
    }
}
