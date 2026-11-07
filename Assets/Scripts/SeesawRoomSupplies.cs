using System.Collections.Generic;
using UnityEngine;

/// <summary>One room's Fan supply, and a wind-released ordinary ball supply.</summary>
[DefaultExecutionOrder(50)]
public class SeesawRoomSupplies : MonoBehaviour
{
    [Header("Room and arrival")]
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private Collider2D boardSurface;
    [SerializeField] private BoxCollider2D roomVolume;
    [SerializeField] private bool activateOnStart;
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private SeesawRoomStage roomStage;
    [SerializeField] private Collider2D bouncyOnlyBarrier;

    [Header("Fan spawner")]
    [SerializeField] private PossessableBody fanPrefab;
    [SerializeField] private Transform fanSpawnPoint;

    [Header("Ordinary ball spawner; excludes possessable forms")]
    [SerializeField] private WindReleasedBall ballPrefab;
    [SerializeField] private Transform ballSpawnPoint;
    [Tooltip("Wait for all ordinary balls in this Unity scene, rather than just this room.")]
    [SerializeField] private bool countBallsAcrossScene = true;
    [SerializeField, Min(0f)] private float respawnDelay = 0.5f;
    [SerializeField, Min(0.02f)] private float populationCheckInterval = 0.1f;

    [Header("Ball exits relative to the actual board bounds")]
    [Tooltip("Let balls roll off the board and be collected by this room's floor spikes.")]
    [SerializeField] private bool useFloorSpikeExits;
    [Tooltip("Keep balls alive beyond the board until they reach the cliff fall zones.")]
    [SerializeField] private bool useCliffExits;
    [Tooltip("Remove balls before they become trapped against the old end baffles.")]
    [SerializeField, Min(0f)] private float horizontalExitInset = 1.6f;
    [SerializeField, Min(0f)] private float despawnDepthBelowBoard = 1f;
    [SerializeField, Min(1f)] private float upperBallLimit = 10f;

    public bool IsRoomActive { get; private set; }
    public bool IsPuzzleComplete { get; private set; }
    public int FanCount { get; private set; }
    public int OrdinaryBallCount { get; private set; }
    public PossessableBody SpawnedFan { get; private set; }
    public WindReleasedBall SpawnedBall { get; private set; }
    public bool FanSupplyStopped { get; private set; }
    private bool gustBatchStarted;
    public bool ReferencesReady => player != null && boardSurface != null && roomVolume != null &&
        fanPrefab != null && fanPrefab.Form == BodyForm.Fan && fanSpawnPoint != null &&
        ballPrefab != null && ballSpawnPoint != null;

    private readonly List<PossessableBody> roomFans = new List<PossessableBody>();
    private readonly HashSet<Rigidbody2D> ordinaryBodies = new HashSet<Rigidbody2D>();
    private float nextPopulationCheck;
    private float fanMissingSince = -1f;
    private float ballMissingSince = -1f;
    private bool wasControllingRoomFan;

    public bool AnyFanBlowing
    {
        get
        {
            if (!IsRoomActive) return false;
            foreach (PossessableBody fan in roomFans)
                if (fan != null && fan.isActiveAndEnabled && IsInsideRoom(fan.transform.position) &&
                    fan.TryGetComponent(out FanBlower wind) && wind.isActiveAndEnabled && wind.IsBlowing)
                    return true;
            return false;
        }
    }

    // A new ball waits for a NEW start command, even when a Fan is already running.
    // The sequence also retains short on/off commands between physics steps.
    public bool HasFanBlowCommandAfter(ulong activation)
    {
        if (!IsRoomActive) return false;
        foreach (PossessableBody fan in roomFans)
            if (fan != null && fan.isActiveAndEnabled && IsInsideRoom(fan.transform.position) &&
                fan.TryGetComponent(out FanBlower wind) && wind.isActiveAndEnabled && wind.LastBlowActivation > activation)
                return true;
        return false;
    }

    private void Start()
    {
        if (activateOnStart) EnterRoom();
    }

    // Called by the door before placing the Star. It is safe to call more than once.
    public void EnterRoom()
    {
        if (IsPuzzleComplete || IsRoomActive || !isActiveAndEnabled) return;
        if (!ReferencesReady)
        {
            Debug.LogError("Assign the board, room bounds, two prefabs, spawn points and player to SeesawRoomSupplies.", this);
            return;
        }
        IsRoomActive = true;
        RefreshPopulation();
        if (!FanSupplyStopped && FanCount == 0) SpawnFan();
        if (OrdinaryBallCount == 0) SpawnBall();
        nextPopulationCheck = Time.time + populationCheckInterval;
    }

    private void Update()
    {
        if (IsPuzzleComplete || !IsRoomActive || player == null || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        if (player.CurrentBody != null)
            wasControllingRoomFan = player.CurrentBody.Form == BodyForm.Fan && IsInsideRoom(player.CurrentBody.transform.position);
        if (Time.time < nextPopulationCheck) return;
        nextPopulationCheck = Time.time + populationCheckInterval;
        RefreshPopulation();
        if (FanSupplyStopped) ClearUncontrolledFans();
        if (wasControllingRoomFan && (player.CurrentBody == null || !player.CurrentBody.isActiveAndEnabled) && FanCount > 0)
            player.TryRecoverMissingHost(roomFans[0]);
        if (FanCount > 0) fanMissingSince = -1f;
        else if (!FanSupplyStopped)
        {
            if (fanMissingSince < 0f) fanMissingSince = Time.time;
            if (Time.time - fanMissingSince >= respawnDelay) SpawnFan();
        }
        if (OrdinaryBallCount > 0) ballMissingSince = -1f;
        else if (!gustBatchStarted)
        {
            if (ballMissingSince < 0f) ballMissingSince = Time.time;
            if (Time.time - ballMissingSince >= respawnDelay) SpawnBall();
        }
    }

    private void RefreshPopulation()
    {
        roomFans.Clear();
        foreach (PossessableBody candidate in FindObjectsByType<PossessableBody>(FindObjectsSortMode.None))
            if (candidate.isActiveAndEnabled && candidate.gameObject.scene == gameObject.scene &&
                candidate.Form == BodyForm.Fan && IsInsideRoom(candidate.transform.position))
                roomFans.Add(candidate); // A controlled Fan counts too.
        FanCount = roomFans.Count;

        ordinaryBodies.Clear();
        // Also count ordinary red balls that were authored manually using the original prefab.
        foreach (CircleVisual visual in FindObjectsByType<CircleVisual>(FindObjectsSortMode.None))
            if (visual.TryGetComponent(out Rigidbody2D body)) CountOrdinaryBody(body);
        foreach (WindReleasedBall ball in FindObjectsByType<WindReleasedBall>(FindObjectsSortMode.None))
            CountOrdinaryBody(ball.GetComponent<Rigidbody2D>());
        OrdinaryBallCount = ordinaryBodies.Count;
    }

    private void CountOrdinaryBody(Rigidbody2D body)
    {
        if (body == null || !body.gameObject.activeInHierarchy || body.gameObject.scene != gameObject.scene ||
            body.GetComponent<PossessableBody>() != null ||
            (!countBallsAcrossScene && !IsInsideRoom(body.position))) return;
        ordinaryBodies.Add(body);
    }

    private void SpawnFan()
    {
        SpawnedFan = Instantiate(fanPrefab, fanSpawnPoint.position, Quaternion.identity, transform);
        SpawnedFan.name = "Room Fan";
        SpawnedFan.GetComponent<FanBlower>().StopBlowing();
        if (bouncyOnlyBarrier != null)
            foreach (Collider2D collider in SpawnedFan.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(bouncyOnlyBarrier, collider);
        Physics2D.SyncTransforms();
        if (wasControllingRoomFan && (player.CurrentBody == null || !player.CurrentBody.isActiveAndEnabled))
            player.TryRecoverMissingHost(SpawnedFan);
        fanMissingSince = -1f;
        RefreshPopulation();
    }

    public void StopFanSupply()
    {
        FanSupplyStopped = true;
        fanSpawnPoint.gameObject.SetActive(false);
        RefreshPopulation();
        ClearUncontrolledFans();
    }

    private void ClearUncontrolledFans()
    {
        foreach (PossessableBody fan in roomFans)
        {
            if (fan == player.CurrentBody && fan.IsControlled) continue;
            fan.GetComponent<FanBlower>().StopBlowing();
            fan.gameObject.SetActive(false);
            Destroy(fan.gameObject);
        }
        roomFans.RemoveAll(fan => fan == null || !fan.gameObject.activeInHierarchy);
        FanCount = roomFans.Count;
    }

    public void BeginGustBatch() => gustBatchStarted = true;

    public WindReleasedBall TakeGustBall(bool reuseWaitingBall)
    {
        if (!reuseWaitingBall || SpawnedBall == null || !SpawnedBall.gameObject.activeInHierarchy || SpawnedBall.HasBeenReleased)
            SpawnBall();
        return SpawnedBall;
    }

    public bool TryBreakFanOnSpikes(PossessableBody fan)
        => RemoveRoomFan(fan, true);

    public bool TryRemoveFallenFan(PossessableBody fan)
        => RemoveRoomFan(fan, false);

    private bool RemoveRoomFan(PossessableBody fan, bool shatter)
    {
        if (fan == null || !fan.isActiveAndEnabled || fan.Form != BodyForm.Fan ||
            fan.gameObject.scene != gameObject.scene || !IsInsideRoom(fan.transform.position)) return false;
        wasControllingRoomFan |= fan.IsControlled;
        if (fan.TryGetComponent(out FanBlower blower)) blower.StopBlowing();
        if (shatter) SeesawFanShatter.Play(fan);
        fan.gameObject.SetActive(false);
        Destroy(fan.gameObject);
        return true;
    }

    public void CompletePuzzle()
    {
        if (IsPuzzleComplete) return;
        IsPuzzleComplete = true;
        RefreshPopulation();
        if (wasControllingRoomFan || (player.CurrentBody != null &&
            player.CurrentBody.Form == BodyForm.Fan && IsInsideRoom(player.CurrentBody.transform.position)))
            player.RestoreStarAfterFanPuzzle((Vector2)boardSurface.bounds.center + Vector2.up);
        wasControllingRoomFan = false;
        foreach (PossessableBody fan in roomFans)
        {
            if (fan.TryGetComponent(out FanBlower blower)) blower.StopBlowing();
            fan.gameObject.SetActive(false);
            Destroy(fan.gameObject);
        }
        foreach (WindReleasedBall ball in FindObjectsByType<WindReleasedBall>(FindObjectsSortMode.None))
            if (ball.IsOwnedBy(this))
            {
                ball.gameObject.SetActive(false);
                Destroy(ball.gameObject);
            }
        fanSpawnPoint.gameObject.SetActive(false);
        ballSpawnPoint.gameObject.SetActive(false);
        roomFans.Clear();
        ordinaryBodies.Clear();
        FanCount = OrdinaryBallCount = 0;
        if (seesaw != null) seesaw.CompletePuzzle();
    }

    private void SpawnBall()
    {
        SpawnedBall = Instantiate(ballPrefab, ballSpawnPoint.position, Quaternion.identity, transform);
        SpawnedBall.name = "Wind Released Ball";
        SpawnedBall.Bind(this);
        ballMissingSince = -1f;
        RefreshPopulation();
    }

    public bool IsInsideRoom(Vector2 point) => roomVolume != null && roomVolume.bounds.Contains(new Vector3(point.x, point.y, roomVolume.bounds.center.z));

    public void RegisterBallCollider(Collider2D ball)
    {
        if (roomStage != null) roomStage.AllowBall(ball);
        if (bouncyOnlyBarrier != null) Physics2D.IgnoreCollision(bouncyOnlyBarrier, ball);
    }

    public bool HasBallLeftBoard(Vector2 point)
    {
        // Spike rooms keep balls alive after the end sensor and during their fall.
        // Retain an outside-room cleanup in case a ball escapes the enclosure.
        if (useFloorSpikeExits || useCliffExits) return !IsInsideRoom(point);
        if (seesaw != null)
            return seesaw.HasBallExited(point, horizontalExitInset, despawnDepthBelowBoard, upperBallLimit);
        Bounds bounds = boardSurface.bounds;
        float inset = Mathf.Min(horizontalExitInset, bounds.size.x * 0.4f);
        return point.x < bounds.min.x + inset || point.x > bounds.max.x - inset ||
            point.y < bounds.min.y - despawnDepthBelowBoard || point.y > bounds.max.y + upperBallLimit;
    }

    // Put the Star above a Fan already standing at the arrival point, rather than inside its collider.
    public Vector2 ClearArrivalPosition(Vector2 arrival, float starHalfHeight, float clearance)
    {
        foreach (PossessableBody fan in roomFans)
        {
            if (fan == null || fan.BodyCollider == null) continue;
            Bounds b = fan.BodyCollider.bounds;
            if (arrival.x >= b.min.x - starHalfHeight && arrival.x <= b.max.x + starHalfHeight)
                arrival.y = Mathf.Max(arrival.y, b.max.y + starHalfHeight + clearance);
        }
        return arrival;
    }

    private void OnDrawGizmosSelected()
    {
        if (fanSpawnPoint != null)
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f);
            Gizmos.DrawWireCube(fanSpawnPoint.position, new Vector3(0.8f, 0.8f, 0.1f));
        }
        if (ballSpawnPoint != null)
        {
            Gizmos.color = new Color(1f, 0.35f, 0.25f);
            Gizmos.DrawWireSphere(ballSpawnPoint.position, 0.35f);
        }
    }
}
