using UnityEngine;

/// <summary>Short-range silk: aim, shoot, pull to a climbable surface, then resume climbing.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PossessableBody), typeof(HexagonSurfaceMovement))]
public sealed class SpiderSilkAbility : MonoBehaviour
{
    [Header("Silk")]
    [SerializeField, Min(0.1f)] private float range = 3f;
    [SerializeField, Min(0.1f)] private float shootSpeed = 14f;
    [SerializeField, Min(0f)] private float cooldown = 0.7f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.25f;
    [SerializeField, Min(0.005f)] private float width = 0.045f;
    [SerializeField] private LayerMask hitMask = Physics2D.DefaultRaycastLayers;
    [SerializeField] private Color silkColor = new Color(0.85f, 0.95f, 1f, 1f);
    [Header("Pull")]
    [SerializeField, Min(0.1f)] private float pullSpeed = 8f;
    [SerializeField, Min(0.1f)] private float maximumPullSeconds = 1.5f;

    private enum Phase { Idle, Flying, Pulling, Fading }
    private Phase phase;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private PossessableBody possessable;
    private HexagonSurfaceMovement movement;
    private LineRenderer strand;
    private SpiderSilkAimVisual aimVisual;
    private float facing = 1f, nextShotAt, fadeAge, distance, pullAge;
    private Vector2 direction, shotOrigin, tip;
    private Collider2D hitSurface;
    private Vector3 localHit, localNormal;
    private Vector2 aimDirection;

    public float Range => range;
    public bool IsFiring => phase != Phase.Idle;
    public bool IsPulling => phase == Phase.Pulling;
    public bool IsAiming { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextShotAt - Time.time);
    public bool CanAim => CanUse && Time.timeScale > 0f && !IsFiring && CooldownRemaining <= 0f;
    private Vector2 Center => possessable.BodyCollider.bounds.center;

    private void Awake()
    {
        possessable = GetComponent<PossessableBody>();
        movement = GetComponent<HexagonSurfaceMovement>();
    }

    private bool CanUse => isActiveAndEnabled && possessable.isActiveAndEnabled && possessable.IsReady &&
        possessable.Form == BodyForm.Spider && possessable.IsControlled &&
        possessable.Owner.isActiveAndEnabled && !possessable.Owner.IsDead &&
        !possessable.Owner.IsRestarting && possessable.Body.simulated;

    public bool BeginAim()
    {
        if (!CanAim || !EnsureVisuals()) return false;
        UpdateFacing();
        aimDirection = movement.SurfaceTangent * facing;
        IsAiming = true;
        return true;
    }

    public void Aim(Vector2 aim)
    {
        if (IsAiming && aim.sqrMagnitude > 0.0001f) aimDirection = aim.normalized;
    }

    public bool ReleaseAim()
    {
        if (!IsAiming) return false;
        Vector2 aim = aimDirection;
        CancelAim();
        return TryFire(aim);
    }

    public void CancelAim()
    {
        IsAiming = false;
        aimVisual?.Hide();
    }

    public bool TryFire(Vector2 aim = default)
    {
        if (!CanAim || !EnsureVisuals()) return false;
        CancelAim();
        UpdateFacing();
        direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : movement.SurfaceTangent * facing;
        shotOrigin = Center;
        tip = shotOrigin;
        distance = fadeAge = pullAge = 0f;
        hitSurface = null;
        phase = Phase.Flying;
        nextShotAt = Time.time + cooldown;
        strand.startColor = strand.endColor = silkColor;
        DrawLine(strand, shotOrigin, tip);
        strand.enabled = true;
        return true;
    }

    private void UpdateFacing()
    {
        if (Mathf.Abs(possessable.Owner.HorizontalInput) > 0.01f)
            facing = Mathf.Sign(possessable.Owner.HorizontalInput);
    }

    private bool CanAnchor(Collider2D surface) => surface != null && movement.CanClimb(surface) &&
        surface.GetComponentInParent<PossessableBody>() == null &&
        surface.GetComponentInParent<SpikeHazard>() == null &&
        surface.GetComponentInParent<EnemyContactKill2D>() == null;

    private bool Cast(Vector2 origin, Vector2 delta, float radius, out RaycastHit2D first)
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;
        int count = radius > 0f
            ? Physics2D.CircleCast(origin, radius, delta.normalized, filter, hits, delta.magnitude)
            : Physics2D.Raycast(origin, delta.normalized, filter, hits, delta.magnitude);
        first = default;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || hit.rigidbody == possessable.Body ||
                hit.collider.transform.IsChildOf(transform)) continue;
            if (radius > 0f && hit.distance <= 0.001f)
            {
                // Initial-overlap casts report the inverse cast direction, not a surface normal.
                Vector2 outward = origin - hit.collider.ClosestPoint(origin);
                if (outward.sqrMagnitude > 0.0001f && Vector2.Dot(delta.normalized, outward.normalized) >= -0.01f)
                    continue;
            }
            if (first.collider == null || hit.distance < first.distance) first = hit;
        }
        return first.collider != null;
    }

    // Called by the possession controller before normal climbing. True owns this physics step.
    public bool TickPhysics()
    {
        if (!CanUse) { Cancel(); return false; }
        if (Time.timeScale <= 0f) return IsPulling;
        if (phase == Phase.Flying)
        {
            distance = Mathf.Min(range, distance + shootSpeed * Time.fixedDeltaTime);
            tip = shotOrigin + direction * distance;
            Vector2 delta = Vector2.ClampMagnitude(tip - Center, range);
            tip = Center + delta;
            if (Cast(Center, delta, 0f, out var hit))
            {
                tip = hit.point;
                if (CanAnchor(hit.collider))
                {
                    hitSurface = hit.collider;
                    localHit = hitSurface.transform.InverseTransformPoint(hit.point);
                    localNormal = hitSurface.transform.InverseTransformDirection(hit.normal);
                    movement.Release();
                    possessable.Body.gravityScale = 0f;
                    possessable.Body.linearVelocity = Vector2.zero;
                    phase = Phase.Pulling;
                }
                else phase = Phase.Fading;
            }
            else if (distance >= range) phase = Phase.Fading;
        }
        if (phase != Phase.Pulling) return false;

        if (!CanAnchor(hitSurface) || !hitSurface.enabled || !hitSurface.gameObject.activeInHierarchy)
        {
            FinishPull(false);
            return true;
        }
        tip = hitSurface.transform.TransformPoint(localHit);
        Vector2 normal = hitSurface.transform.TransformDirection(localNormal).normalized;
        Vector2 toAnchor = tip - Center;
        pullAge += Time.fixedDeltaTime;
        if (toAnchor.magnitude > range + 0.5f || pullAge > maximumPullSeconds ||
            (Cast(Center, toAnchor, 0f, out var obstruction) && obstruction.collider != hitSurface))
        {
            FinishPull(false);
            return true;
        }

        Vector2 target = tip + normal * movement.AttachmentDistance;
        Vector2 travel = target - Center;
        if (travel.magnitude <= 0.04f)
        {
            FinishPull(true);
            return true;
        }
        float step = Mathf.Min(pullSpeed * Time.fixedDeltaTime, travel.magnitude);
        // Sweep the whole body, not just the silk; thin walls and corners block the pull.
        if (Cast(Center, travel.normalized * (step + 0.02f), movement.WorldRadius + 0.02f, out var block))
        {
            step = Mathf.Min(step, Mathf.Max(0f, block.distance - 0.02f));
            if (step <= 0.001f)
            {
                FinishPull(block.collider == hitSurface);
                return true;
            }
        }
        possessable.Body.linearVelocity = travel.normalized * (step / Time.fixedDeltaTime);
        return true;
    }

    private void FinishPull(bool attach)
    {
        possessable.Body.linearVelocity = Vector2.zero;
        movement.Release();
        if (attach && hitSurface != null)
            movement.TryAttachFromSilk(hitSurface, hitSurface.transform.TransformDirection(localNormal));
        phase = Phase.Fading;
        fadeAge = 0f;
    }

    private void LateUpdate()
    {
        if (!CanUse) { Cancel(); return; }
        if (Time.deltaTime <= 0f) { CancelAim(); return; }
        UpdateFacing();
        if (IsAiming) DrawAim();
        if (!IsFiring) return;
        if (IsPulling && hitSurface != null) tip = hitSurface.transform.TransformPoint(localHit);
        if (phase == Phase.Fading) fadeAge += Time.deltaTime;
        if (fadeAge >= fadeSeconds) { Cancel(); return; }
        Color tint = silkColor;
        tint.a *= 1f - fadeAge / fadeSeconds;
        strand.startColor = strand.endColor = tint;
        DrawLine(strand, Center, tip);
    }

    private void DrawAim()
    {
        Vector2 end = Center + aimDirection * range;
        bool hasHit = Cast(Center, aimDirection * range, 0f, out var hit);
        if (hasHit)
        {
            end = hit.point;
        }
        aimVisual.Draw(Center, transform.position.z, aimDirection, range,
            Vector2.Distance(Center, end), movement.WorldRadius, hasHit);
    }

    private bool EnsureVisuals()
    {
        if (strand != null) return true;
        if (movement.Visual == null || !movement.Visual.TryGetComponent(out SpriteRenderer sprite)) return false;
        strand = CreateLine("Spider Silk", sprite, width, false);
        aimVisual = new SpiderSilkAimVisual(transform, sprite);
        return true;
    }

    private LineRenderer CreateLine(string label, SpriteRenderer sprite, float thickness, bool loop)
    {
        var root = new GameObject(label);
        root.layer = gameObject.layer;
        root.transform.SetParent(transform, false);
        var line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = loop ? 64 : 2;
        line.startWidth = line.endWidth = thickness;
        line.numCapVertices = 4;
        line.sharedMaterial = sprite.sharedMaterial;
        line.sortingLayerID = sprite.sortingLayerID;
        line.sortingOrder = sprite.sortingOrder + 5;
        line.enabled = false;
        return line;
    }

    private void DrawLine(LineRenderer line, Vector2 origin, Vector2 end)
    {
        float z = transform.position.z;
        line.SetPosition(0, new Vector3(origin.x, origin.y, z));
        line.SetPosition(1, new Vector3(end.x, end.y, z));
    }

    public void Cancel()
    {
        CancelAim();
        if (IsPulling && possessable.Body != null)
        {
            possessable.Body.linearVelocity = Vector2.zero;
            movement.Release();
        }
        phase = Phase.Idle;
        hitSurface = null;
        if (strand != null) strand.enabled = false;
    }

    private void OnDisable() => Cancel();
    private void OnDestroy()
    {
        aimVisual?.Dispose();
        if (strand != null) Destroy(strand.gameObject);
    }
}
