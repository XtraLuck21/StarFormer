using UnityEngine;

// One ceiling tooth drops when the controlled form enters its horizontal band.
// The route coordinator owns mode changes; ordinary checkpoint retries rearm it.
public sealed class HardRouteFallingSpike : MonoBehaviour
{
    [SerializeField] private Transform spike;
    [SerializeField] private AdvancedRouteSwitch route;
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private float floorY = 11.25f;
    [SerializeField, Min(.1f)] private float triggerHalfWidth = 2f;
    [SerializeField, Min(0f)] private float initialSpeed = 2f;
    [SerializeField, Min(.1f)] private float acceleration = 32f;
    [SerializeField, Min(.1f)] private float maximumSpeed = 18f;

    private enum Phase { Armed, Falling, Landed }
    private Phase phase;
    private Vector3 restPosition;
    private Collider2D spikeCollider;
    private SpriteRenderer spikeRenderer;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private bool initialized;
    private float speed;
    private float landingY;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        restPosition = spike.localPosition;
        spikeCollider = spike.GetComponent<Collider2D>();
        spikeRenderer = spike.GetComponent<SpriteRenderer>();
        initialized = true;
    }

    public void ResetTrap()
    {
        Initialize();
        phase = Phase.Armed;
        speed = initialSpeed;
        spike.localPosition = restPosition;
        // HazardGroup restores visibility and collision with the other teeth.
    }

    private void FixedUpdate()
    {
        if (route.CurrentRoute != AdvancedRouteSwitch.RouteState.Hard || route.IsPresenting ||
            !playerController.isActiveAndEnabled || playerController.IsDead) return;

        PossessableBody controlled = playerController.CurrentBody;
        if (controlled == null || !controlled.IsReady || !controlled.IsControlled ||
            !controlled.Body.simulated || !controlled.BodyCollider.enabled) return;

        if (phase == Phase.Armed)
        {
            Bounds form = controlled.BodyCollider.bounds;
            // Ignore the lower room and unpossessed bodies below the shared floor.
            if (!spikeCollider.enabled || form.center.y < floorY ||
                form.center.y >= spikeCollider.bounds.min.y ||
                Mathf.Abs(form.center.x - spike.position.x) > triggerHalfWidth) return;
            landingY = floorY + spike.position.y - spikeRenderer.bounds.min.y;
            phase = Phase.Falling;
        }

        if (phase == Phase.Falling)
        {
            speed = Mathf.Min(maximumSpeed, speed + acceleration * Time.fixedDeltaTime);
            float nextY = Mathf.Max(landingY, spike.position.y - speed * Time.fixedDeltaTime);
            float distance = spike.position.y - nextY;
            // Sweep the actual triangle so the fast drop cannot skip the player.
            var filter = new ContactFilter2D { useTriggers = false };
            int count = spikeCollider.Cast(Vector2.down, filter, hits, distance);
            bool struck = spikeCollider.Distance(controlled.BodyCollider).isOverlapped;
            for (int i = 0; i < count; i++)
                if (hits[i].collider == controlled.BodyCollider) struck = true;
            if (struck)
            {
                playerController.KillControlledBody(controlled, AnalyticsDeathCause.Spike);
                return;
            }
            spike.position = new Vector3(spike.position.x, nextY, spike.position.z);
            Physics2D.SyncTransforms();
            // Keep the landed tooth visible and lethal until the route or attempt resets.
            if (nextY <= landingY) phase = Phase.Landed;
        }
    }

    private void OnDisable()
    {
        if (initialized) ResetTrap();
    }

    private void OnDrawGizmosSelected()
    {
        if (spike == null) return;
        Gizmos.color = new Color(1f, .65f, .1f, .7f);
        float top = spike.position.y;
        Gizmos.DrawWireCube(new Vector3(spike.position.x, (floorY + top) * .5f, 0f),
            new Vector3(triggerHalfWidth * 2f, Mathf.Max(0f, top - floorY), 0f));
    }
}
