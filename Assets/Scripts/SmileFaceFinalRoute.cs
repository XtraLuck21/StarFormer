using UnityEngine;

[DefaultExecutionOrder(220)]
public sealed class SmileFaceFinalRoute : MonoBehaviour
{
    public const string BaseCheckpoint = "SMILE_SPIDER_BASE";
    public const string RightCheckpoint = "SMILE_RIGHT_HEAD";
    public const string ForeheadCheckpoint = "SMILE_FOREHEAD_ENTRY";
    public const string BoxCheckpoint = "SMILE_HAND_BOX";
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody spider;
    [SerializeField] private TeethTriggeredDoor teeth;
    [SerializeField] private SmileFaceCompletion expression;
    [SerializeField] private level2Camera roomCamera;
    [SerializeField] private SmileGoalFlag goal;
    [SerializeField] private SmileBoxPressureButton button;
    [SerializeField] private SmileDoorSpiderGate doorGate;
    [SerializeField] private BoxCollider2D foreheadRecoveryZone;
    [SerializeField] private PossessableBody finalBox;
    private Vector3 boxStart;
    private static string pressedScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ClearSession() => pressedScene = null;

    public static bool IsRouteCheckpoint(PlayerPossessionController controller) =>
        controller.IsCurrentCheckpoint(BaseCheckpoint) || controller.IsCurrentCheckpoint(RightCheckpoint) ||
        controller.IsCurrentCheckpoint(ForeheadCheckpoint) || controller.IsCurrentCheckpoint(BoxCheckpoint);

    public bool IsCheckpointActive(bool upper, bool forehead = false, bool box = false) =>
        player.IsCurrentCheckpoint(BoxCheckpoint) || (!box && player.IsCurrentCheckpoint(ForeheadCheckpoint)) || (!box && !forehead && player.IsCurrentCheckpoint(RightCheckpoint)) ||
        (!box && !forehead && !upper && player.IsCurrentCheckpoint(BaseCheckpoint));

    public void EnterForeheadCamera() => expression.EnterForeheadOverview();

    private void Awake()
    {
        if (finalBox != null) boxStart = finalBox.transform.position;
        // Keep the waiting Spider visible for normal contact possession.
        if (doorGate != null && (player.IsCurrentCheckpoint(ForeheadCheckpoint) || player.IsCurrentCheckpoint(BoxCheckpoint)))
            spider.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (IsRouteCheckpoint(player)) {
            teeth.RestoreSolved();
            expression.RestoreCompletedPose();
            if (player.IsCurrentCheckpoint(BoxCheckpoint)) {
                player.CurrentBody.SetOwner(null);
                player.CurrentBody.gameObject.SetActive(false);
                finalBox.gameObject.SetActive(true);
                finalBox.transform.position = boxStart;
                finalBox.Body.position = boxStart;
                finalBox.Body.linearVelocity = Vector2.zero;
                finalBox.Body.angularVelocity = 0;
                Physics2D.SyncTransforms();
                player.TryRecoverMissingHost(finalBox);
            }
            else if (!player.IsCurrentCheckpoint(ForeheadCheckpoint)) {
                Vector3 position = player.CurrentBody.transform.position;
                if (doorGate != null && player.IsCurrentCheckpoint(BaseCheckpoint))
                    position = doorGate.SpiderCheckpointPosition;
                player.CurrentBody.SetOwner(null);
                player.CurrentBody.gameObject.SetActive(false);
                spider.gameObject.SetActive(true);
                spider.GetComponent<HexagonSurfaceMovement>().Release();
                spider.transform.position = position;
                spider.Body.position = position;
                spider.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                player.TryRecoverMissingHost(spider);
            }
            roomCamera.BeginRemainingFollow();
            if (player.IsCurrentCheckpoint(ForeheadCheckpoint))
                expression.RestoreForeheadCamera();
        }
        if (pressedScene == gameObject.scene.path) {
            button.RestorePressed();
            goal.Raise(true);
        }
    }

    private void Update()
    {
        if (foreheadRecoveryZone == null || !player.IsCurrentCheckpoint(ForeheadCheckpoint) ||
            player.CurrentBody == null || player.IsDead || player.IsRestarting || Time.timeScale <= 0f)
            return;
        // Only the lower right-ear landing is a recovery zone. Keep the upper
        // Bubble approach and the left-hand Box route available normally.
        Bounds recovery = foreheadRecoveryZone.bounds;
        recovery.Expand(new Vector3(4f, 1.2f, 0f));
        if (!recovery.Intersects(player.CurrentBody.BodyCollider.bounds)) return;
        if (!player.TryReturnToStarCheckpoint(ForeheadCheckpoint)) return;
        expression.RestoreForeheadCamera();
    }

    public bool SaveCheckpoint(bool upper, Vector3 position, bool forehead = false, bool box = false)
    {
        if (player.IsDead || player.IsRestarting || !teeth.IsOpen ||
            player.CurrentBody == null) return false;
        BodyForm form = player.CurrentBody.Form;
        if (box ? player.CurrentBody != finalBox : forehead ? form != BodyForm.Spider && form != BodyForm.Bubble && form != BodyForm.Star
            : form != BodyForm.Spider) return false;
        string id = box ? BoxCheckpoint : forehead ? ForeheadCheckpoint : upper ? RightCheckpoint : BaseCheckpoint;
        if (player.IsCurrentCheckpoint(BoxCheckpoint)) return false;
        if (!box && !forehead && player.IsCurrentCheckpoint(ForeheadCheckpoint)) return false;
        if (!box && !upper && !forehead && player.IsCurrentCheckpoint(RightCheckpoint)) return false;
        if (player.IsCurrentCheckpoint(id)) return false;
        // A climbing checkpoint must restore the same side of the ear boundary,
        // rather than a nearby ledge inside the face's closed collider loop.
        if (box) position = boxStart;
        else if (upper && !forehead) position = player.CurrentBody.Body.position;
        player.SetCheckpoint(id, position, false);
        foreach (SmileRouteCheckpoint checkpoint in GetComponentsInChildren<SmileRouteCheckpoint>())
            checkpoint.RefreshMarker();
        return true;
    }

    public void PressFinalButton()
    {
        pressedScene = gameObject.scene.path;
        goal.Raise();
    }
}
