using UnityEngine;

[DefaultExecutionOrder(220)]
public sealed class SmileFaceFinalRoute : MonoBehaviour
{
    public const string BaseCheckpoint = "SMILE_SPIDER_BASE";
    public const string RightCheckpoint = "SMILE_RIGHT_HEAD";
    public const string ForeheadCheckpoint = "SMILE_FOREHEAD_ENTRY";
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody spider;
    [SerializeField] private TeethTriggeredDoor teeth;
    [SerializeField] private SmileFaceCompletion expression;
    [SerializeField] private level2Camera roomCamera;
    [SerializeField] private SmileGoalFlag goal;
    [SerializeField] private SmileBoxPressureButton button;
    [SerializeField] private SmileDoorSpiderGate doorGate;
    private static string pressedScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ClearSession() => pressedScene = null;

    public static bool IsRouteCheckpoint(PlayerPossessionController controller) =>
        controller.IsCurrentCheckpoint(BaseCheckpoint) || controller.IsCurrentCheckpoint(RightCheckpoint) ||
        controller.IsCurrentCheckpoint(ForeheadCheckpoint);

    public bool IsCheckpointActive(bool upper, bool forehead = false) =>
        player.IsCurrentCheckpoint(ForeheadCheckpoint) || (!forehead && player.IsCurrentCheckpoint(RightCheckpoint)) ||
        (!forehead && !upper && player.IsCurrentCheckpoint(BaseCheckpoint));

    public void EnterForeheadCamera() => expression.EnterForeheadOverview();

    private void Awake()
    {
        if (doorGate != null) spider.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (IsRouteCheckpoint(player)) {
            teeth.RestoreSolved();
            expression.RestoreCompletedPose();
            if (!player.IsCurrentCheckpoint(ForeheadCheckpoint)) {
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
                roomCamera.BeginSmileOverview(expression.RoomBounds, 1f, immediate: true);
        }
        if (pressedScene == gameObject.scene.path) {
            button.RestorePressed();
            goal.Raise(true);
        }
    }

    public bool SaveCheckpoint(bool upper, Vector3 position, bool forehead = false)
    {
        if (player.IsDead || player.IsRestarting || !teeth.IsOpen ||
            player.CurrentBody == null) return false;
        BodyForm form = player.CurrentBody.Form;
        if (forehead ? form != BodyForm.Spider && form != BodyForm.Bubble && form != BodyForm.Star
            : form != BodyForm.Spider) return false;
        string id = forehead ? ForeheadCheckpoint : upper ? RightCheckpoint : BaseCheckpoint;
        if (!forehead && player.IsCurrentCheckpoint(ForeheadCheckpoint)) return false;
        if (!upper && !forehead && player.IsCurrentCheckpoint(RightCheckpoint)) return false;
        if (player.IsCurrentCheckpoint(id)) return false;
        // A climbing checkpoint must restore the same side of the ear boundary,
        // rather than a nearby ledge inside the face's closed collider loop.
        if (upper && !forehead) position = player.CurrentBody.Body.position;
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
