using UnityEngine;

// Opt-in, scene-local checkpoint configuration. Only completed progress survives reloads;
// loose bodies and the lower reversible door use their authored starting layouts.
[RequireComponent(typeof(PlayerPossessionController))]
public sealed class AdvancedCheckpointController : MonoBehaviour
{
    [SerializeField] private AdvancedCheckpoint[] checkpoints;
    [SerializeField] private AdvancedRouteSwitch route;
    [SerializeField] private RatchetingCeiling upperLift;
    [SerializeField] private RuinButtonGuard ruinGuard;

    private sealed class Progress
    {
        public string scene;
        public int active;
        public int reachedMask;
        public AdvancedRouteSwitch.RouteState route;
        public float liftBottom;
        public bool guardBroken;
        public bool seenPreview;
        public bool seenSafe;
        public bool seenHard;
    }

    private static Progress progress;
    private PlayerPossessionController player;
    private bool restoring;
    public int ActiveIndex => HasProgress ? progress.active : 0;
    private bool HasProgress => progress != null && progress.scene == gameObject.scene.path;

    public static void ClearSession() => progress = null;

    private void Awake() => player = GetComponent<PlayerPossessionController>();

    public bool WasReached(int index) => HasProgress && (progress.reachedMask & (1 << index)) != 0;

    public bool TryActivate(AdvancedCheckpoint checkpoint, PossessableBody body)
    {
        if (restoring || Time.timeScale <= 0f || !player.isActiveAndEnabled || player.IsDead || player.IsRestarting ||
            body == null || body != player.CurrentBody || !body.IsControlled ||
            checkpoint.Index <= ActiveIndex || route.IsPresenting)
            return false;

        if (!HasProgress) progress = new Progress { scene = gameObject.scene.path };
        progress.active = checkpoint.Index;
        progress.reachedMask |= 1 << checkpoint.Index;
        CaptureProgress();
        player.SetCheckpoint("ADV_CP" + checkpoint.Index, checkpoint.SpawnPosition, showFeedback: false);
        return true;
    }

    private void LateUpdate()
    {
        if (HasProgress && !restoring) CaptureProgress();
    }

    private void CaptureProgress()
    {
        // Never snapshot a partially revealed hazard animation.
        if (!route.IsPresenting)
        {
            progress.route = route.CurrentRoute;
            progress.seenPreview = route.HasShownPreview;
            progress.seenSafe = route.HasShownSafe;
            progress.seenHard = route.HasShownHard;
        }
        progress.liftBottom = Mathf.Max(progress.liftBottom, upperLift.CurrentBottom);
        progress.guardBroken |= ruinGuard.IsBroken;
    }

    public bool TryRestore(out PossessableBody body)
    {
        body = null;
        if (!isActiveAndEnabled || !HasProgress) return false;
        AdvancedCheckpoint checkpoint = null;
        foreach (AdvancedCheckpoint item in checkpoints)
            if (item.Index == progress.active) { checkpoint = item; break; }
        if (checkpoint == null || checkpoint.RespawnBody == null || !checkpoint.RespawnBody.IsReady)
        {
            Debug.LogError("The active advanced checkpoint has no configured respawn body.", this);
            return false;
        }

        restoring = true;
        upperLift.LiftForBox(progress.liftBottom);
        if (progress.guardBroken) ruinGuard.RestoreBrokenState();
        route.RestoreCheckpointRoute(progress.route, progress.seenPreview,
            progress.seenSafe, progress.seenHard);
        body = checkpoint.RespawnBody;
        body.gameObject.SetActive(true);
        body.transform.position = checkpoint.SpawnPosition;
        body.Body.position = checkpoint.SpawnPosition;
        body.Body.bodyType = RigidbodyType2D.Dynamic;
        body.Body.linearVelocity = Vector2.zero;
        body.Body.angularVelocity = 0f;
        Physics2D.SyncTransforms();
        restoring = false;
        return true;
    }
}
