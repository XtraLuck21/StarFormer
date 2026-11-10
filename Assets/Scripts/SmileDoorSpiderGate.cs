using UnityEngine;

/// <summary>Close the return passage only after contact possession of the waiting Spider.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileDoorSpiderGate : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private TeethTriggeredDoor toothDoor;
    [SerializeField] private SmileFaceFinalRoute route;
    [SerializeField] private PossessableBody spider;
    [SerializeField] private Transform returnWall;
    [SerializeField] private float wallRiseSeconds = 0.8f;
    [SerializeField] private Vector3 spiderCheckpointPosition = new Vector3(58.5f, 6.4f, 0f);
    public Vector3 SpiderCheckpointPosition => spiderCheckpointPosition;
    public bool HasChangedForm { get; private set; }
    private BoxCollider2D blocker;
    private Vector3 wallRaisedPosition;
    private float riseProgress;

    private void Awake()
    {
        blocker = GetComponent<BoxCollider2D>();
        blocker.isTrigger = false;
        blocker.enabled = false;
        wallRaisedPosition = returnWall.localPosition;
        returnWall.gameObject.SetActive(false);
    }

    private void OnEnable() => player.PossessionTransferred += OnPossessionTransferred;
    private void OnDisable()
    {
        if (player != null) player.PossessionTransferred -= OnPossessionTransferred;
    }

    private void Start()
    {
        if (SmileFaceFinalRoute.IsRouteCheckpoint(player)) ClosePassage(true);
    }

    private void OnPossessionTransferred(PossessableBody previous, PossessableBody current)
    {
        if (HasChangedForm || current != spider || !toothDoor.IsOpen || player.IsDead || player.IsRestarting)
            return;
        route.SaveCheckpoint(false, spiderCheckpointPosition);
        // The player is already safely beyond the wall at the waiting Spider.
        // Block immediately while the visible wall rises, so reversing cannot escape.
        ClosePassage(false);
        if (previous != null && previous.Form == BodyForm.BouncyBall)
            previous.gameObject.SetActive(false);
    }

    private void ClosePassage(bool immediate)
    {
        HasChangedForm = true;
        blocker.enabled = true;
        riseProgress = immediate ? 1f : 0f;
        returnWall.gameObject.SetActive(true);
        UpdateWallPosition();
    }

    private void Update()
    {
        if (!HasChangedForm || riseProgress >= 1f || Time.timeScale <= 0f) return;
        riseProgress = Mathf.MoveTowards(riseProgress, 1f, Time.deltaTime / Mathf.Max(0.01f, wallRiseSeconds));
        UpdateWallPosition();
    }

    private void UpdateWallPosition()
    {
        float height = blocker.bounds.size.y;
        float rise = Mathf.SmoothStep(0f, 1f, riseProgress);
        returnWall.localPosition = wallRaisedPosition + Vector3.down * (height * (1f - rise));
    }
}
