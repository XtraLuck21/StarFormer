using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in physical checks in a disposable project; drives the existing input/door APIs.</summary>
[InitializeOnLoad]
public static class SeesawRoomSupplyVerification
{
    private const string RunningKey = "SeesawRoomSupplyVerification.Running";
    private static PlayerPossessionController player;
    private static StarRoomDoor door;
    private static SeesawRoomSupplies supply;
    private static SeesawTilt2D tilt;
    private static bool observedLeftTilt;
    private static bool observedRightTilt;
    private static PossessableBody fan;
    private static PossessableBody outsideFan;
    private static PossessableBody otherRoomFan;
    private static WindReleasedBall ball;
    private static WindReleasedBall extraBall;
    private static float waitingY;
    private static float supplyX;
    private static float phaseAt;
    private static float moveStartX;
    private static int step;
    private static double deadline;
    private static double nextReport;

    static SeesawRoomSupplyVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
            deadline = EditorApplication.timeSinceStartup + 130;
            EditorApplication.update += Tick;
        };
    }

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/NewLevel02.unity");
        SeesawRoomSupplySetup.Validate();
        if (UnityEngine.Object.FindFirstObjectByType<SeesawTilt2D>() != null) SeesawTiltSetup.Validate();
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new Exception("Timed out at step " + step + ", player=" + player?.CurrentBody?.Form +
                    ", fan=" + Position(fan) + ", ball=" + Position(ball));
            if (player == null) player = UnityEngine.Object.FindFirstObjectByType<PlayerPossessionController>();
            if (door == null) door = UnityEngine.Object.FindFirstObjectByType<StarRoomDoor>();
            if (supply == null) supply = UnityEngine.Object.FindFirstObjectByType<SeesawRoomSupplies>();
            if (tilt == null) tilt = UnityEngine.Object.FindFirstObjectByType<SeesawTilt2D>();
            if (player == null || player.CurrentBody == null && step != 17 || door == null || supply == null) return;
            Require(!player.IsDead, "Supply tests must not kill the player.");
            if (tilt != null && tilt.IsAnimating)
            {
                float angle = Mathf.DeltaAngle(0f, tilt.CurrentAngle);
                if (tilt.LastSide == SeesawTilt2D.Side.Left && angle > 0.1f && angle < 8.9f) observedLeftTilt = true;
                if (tilt.LastSide == SeesawTilt2D.Side.Right && angle > -8.9f && angle < 8.9f) observedRightTilt = true;
            }
            if (EditorApplication.timeSinceStartup >= nextReport)
            {
                nextReport = EditorApplication.timeSinceStartup + 5;
                Debug.Log("ROOM SUPPLY PROGRESS step=" + step + ", fans=" + supply.FanCount + ", balls=" + supply.OrdinaryBallCount +
                    ", host=" + player.CurrentBody?.Form + ", ball=" + Position(ball));
            }
            switch (step)
            {
                case 0:
                    Require(!supply.IsRoomActive && supply.SpawnedFan == null && supply.SpawnedBall == null,
                        "No supplies before entering the room.");
                    if (tilt != null) Require(tilt.TriggerCount == 0 && Mathf.Abs(tilt.CurrentAngle) < 0.01f, "Board starts level.");
                    var star = player.CurrentBody;
                    Vector2 entry = door.GetComponent<Collider2D>().bounds.center;
                    star.transform.position = new Vector3(entry.x, entry.y, star.transform.position.z);
                    star.Body.position = entry;
                    star.Body.linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    Next(1);
                    break;
                case 1:
                    if (!door.HasTeleported || !player.enabled || player.CurrentBody.Form != BodyForm.Fan) break;
                    fan = supply.SpawnedFan;
                    ball = supply.SpawnedBall;
                    Require(player.CurrentBody == fan && fan.IsControlled, "Star touches and possesses the Fan after teleport.");
                    Require(ball != null && ball.GetComponent<PossessableBody>() == null, "Ordinary ball is a separate, non-possessable object.");
                    waitingY = ball.transform.position.y;
                    supplyX = ball.transform.position.x;
                    supply.EnterRoom(); // Idempotent room activation.
                    outsideFan = UnityEngine.Object.Instantiate(Get<PossessableBody>(supply, "fanPrefab"), fan.transform.position + Vector3.right * 70, Quaternion.identity);
                    outsideFan.GetComponent<FanBlower>().ToggleBlow();
                    Next(2);
                    break;
                case 2:
                    if (Time.time - phaseAt < 2f) break;
                    Require(supply.FanCount == 1 && supply.OrdinaryBallCount == 1, "No duplicate spawns, including while the Fan is controlled.");
                    Require(!ball.HasBeenReleased && ball.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Kinematic &&
                        Mathf.Abs(ball.transform.position.y - waitingY) < 0.001f, "Ball hovers; an outside-room Fan cannot release it.");
                    Debug.Log("ROOM SUPPLY HOVER PASS: door handoff, one controlled Fan, ordinary-only population and outside-room wind filtering.");
                    moveStartX = fan.transform.position.x;
                    player.SetTouchRight(true);
                    Next(21);
                    break;
                case 21:
                    if (Time.time - phaseAt < 0.8f) break;
                    Require(fan.transform.position.x > moveStartX + 1.5f && supply.FanCount == 1,
                        "The possessed Fan must move right without being pinned or duplicated.");
                    moveStartX = fan.transform.position.x;
                    player.SetTouchRight(false);
                    player.SetTouchLeft(true);
                    Next(22);
                    break;
                case 22:
                    if (Time.time - phaseAt < 0.8f) break;
                    Require(fan.transform.position.x < moveStartX - 1.5f && supply.FanCount == 1,
                        "The possessed Fan must move left without the spawner pulling it back.");
                    Debug.Log("ROOM SUPPLY MOVEMENT PASS: controlled Fan walks both directions and retains collision support.");
                    player.SetTouchLeft(false);
                    player.SetTouchRight(true);
                    Next(23);
                    break;
                case 23:
                    if (fan.transform.position.x < supplyX + 1.5f) break;
                    // Stand to the right of the ball, then face left to blow it left.
                    player.SetTouchRight(false);
                    player.SetTouchLeft(true);
                    Next(3);
                    break;
                case 3:
                    if (Time.time - phaseAt < 0.2f) break;
                    Require(fan.GetComponent<FanBlower>().Facing == -1, "A input moves and turns the Fan left.");
                    player.ReleaseAllTouchInput();
                    player.PressTouchBlow();
                    Next(4);
                    break;
                case 4:
                    if (Time.time - phaseAt < 0.3f) break;
                    Require(ball.HasBeenReleased && ball.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic &&
                        ball.transform.position.y < waitingY - 0.1f, "Turning wind on releases the hovering ball.");
                    player.PressTouchBlow();
                    Next(5);
                    break;
                case 5:
                    if (Time.time - phaseAt < 0.3f) break;
                    Require(ball.HasBeenReleased && ball.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic,
                        "Turning wind off must not refreeze the released ball.");
                    player.PressTouchBlow();
                    Next(6);
                    break;
                case 6:
                    if (ball != null && ball.transform.position.x >= supplyX - 2f) break;
                    Require(ball != null, "Left wind must visibly move the ball before disposal.");
                    Debug.Log("ROOM SUPPLY LEFT PASS: release is latched and left-facing wind moves the ordinary ball left.");
                    Next(7);
                    break;
                case 7:
                    if (ball != null) break;
                    Next(8);
                    break;
                case 8:
                    if (supply.SpawnedBall == null) break;
                    ball = supply.SpawnedBall;
                    waitingY = ball.transform.position.y;
                    Next(25);
                    break;
                case 25:
                    if (Time.time - phaseAt < 1f) break;
                    if (tilt != null)
                    {
                        Require(observedLeftTilt && tilt.LastSide == SeesawTilt2D.Side.Left && tilt.TriggerCount == 1 &&
                            Mathf.DeltaAngle(0f, tilt.CurrentAngle) > 8.5f, "Left contact smoothly lowers the left end exactly once.");
                        Debug.Log("SEESAW LEFT TILT PASS: actual ball contact, smooth animation, held angle and duplicate-contact rejection.");
                    }
                    Require(fan.GetComponent<FanBlower>().IsBlowing && !ball.HasBeenReleased &&
                        ball.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Kinematic &&
                        Mathf.Abs(ball.transform.position.y - waitingY) < 0.001f,
                        "A replacement ball must ignore wind left running from the previous ball.");
                    player.PressTouchBlow(); // Off, without granting a release.
                    Next(26);
                    break;
                case 26:
                    if (Time.time - phaseAt < 0.2f) break;
                    Require(!ball.HasBeenReleased, "Stopping old wind must not release the new ball.");
                    player.PressTouchBlow(); // A fresh on command.
                    Next(27);
                    break;
                case 27:
                    if (Time.time - phaseAt < 0.2f) break;
                    Require(ball.HasBeenReleased && ball.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic,
                        "A fresh wind activation must release the replacement ball.");
                    Debug.Log("ROOM SUPPLY REARM PASS: replacement ignores old wind, then releases on a fresh off/on command.");
                    fan.GetComponent<FanBlower>().StopBlowing();
                    // A second ordinary ball anywhere in this scene must prevent replenishment.
                    extraBall = UnityEngine.Object.Instantiate(Get<WindReleasedBall>(supply, "ballPrefab"),
                        ball.transform.position + Vector3.right * 75f, Quaternion.identity);
                    UnityEngine.Object.Destroy(ball.gameObject);
                    Next(9);
                    break;
                case 9:
                    if (Time.time - phaseAt < 1f) break;
                    Require(supply.SpawnedBall == null && supply.OrdinaryBallCount == 1,
                        "An ordinary ball elsewhere in the scene blocks the next supply.");
                    UnityEngine.Object.Destroy(extraBall.gameObject);
                    Next(10);
                    break;
                case 10:
                    if (supply.SpawnedBall == null) break;
                    ball = supply.SpawnedBall;
                    Require(!ball.HasBeenReleased && supply.OrdinaryBallCount == 1, "Only the last ball's disappearance permits replenishment.");
                    Debug.Log("ROOM SUPPLY POPULATION PASS: all ordinary balls must disappear; Bouncy Ball is excluded.");
                    player.SetTouchLeft(true);
                    Next(24);
                    break;
                case 24:
                    if (fan.transform.position.x > supplyX - 1.5f) break;
                    // Stand to the left of the next ball, then face right.
                    player.SetTouchLeft(false);
                    player.SetTouchRight(true);
                    Next(11);
                    break;
                case 11:
                    if (Time.time - phaseAt < 0.2f) break;
                    Require(fan.GetComponent<FanBlower>().Facing == 1, "D input turns the Fan right.");
                    player.ReleaseAllTouchInput();
                    player.PressTouchBlow();
                    Next(12);
                    break;
                case 12:
                    if (tilt != null && ball != null) player.SetTouchRight(ball.transform.position.x > fan.transform.position.x + 1.5f);
                    if (ball != null && ball.transform.position.x <= supplyX + 2f) break;
                    Require(ball != null && ball.HasBeenReleased, "Right wind must visibly move the released ball right.");
                    Debug.Log("ROOM SUPPLY RIGHT PASS: right-facing wind moves the ordinary ball right.");
                    Next(13);
                    break;
                case 13:
                    if (ball != null)
                    {
                        if (tilt != null) player.SetTouchRight(ball.transform.position.x > fan.transform.position.x + 1.5f);
                        break;
                    }
                    player.ReleaseAllTouchInput();
                    if (supply.SpawnedBall == null) break;
                    ball = supply.SpawnedBall;
                    waitingY = ball.transform.position.y;
                    Next(28);
                    break;
                case 28:
                    if (Time.time - phaseAt < 1.3f) break;
                    if (tilt != null)
                    {
                        Require(observedRightTilt && tilt.LastSide == SeesawTilt2D.Side.Right && tilt.TriggerCount == 2 &&
                            Mathf.DeltaAngle(0f, tilt.CurrentAngle) < -8.5f, "Next right contact smoothly reverses the tilt exactly once.");
                        Debug.Log("SEESAW RIGHT TILT PASS: opposite-end contact reverses the board while the Fan remains controllable.");
                    }
                    Require(fan.GetComponent<FanBlower>().IsBlowing && !ball.HasBeenReleased &&
                        Mathf.Abs(ball.transform.position.y - waitingY) < 0.001f,
                        "Right-exit replacement must also ignore the previous wind command.");
                    Debug.Log("ROOM SUPPLY RIGHT REARM PASS");
                    fan.GetComponent<FanBlower>().StopBlowing();
                    otherRoomFan = UnityEngine.Object.Instantiate(Get<PossessableBody>(supply, "fanPrefab"), fan.transform.position + Vector3.right * 4f, Quaternion.identity);
                    Next(14);
                    break;
                case 14:
                    if (Time.time - phaseAt < 0.4f) break;
                    Require(supply.FanCount == 2, "Both existing room Fans count without a third spawn.");
                    fan.gameObject.SetActive(false);
                    Next(15);
                    break;
                case 15:
                    if (Time.time - phaseAt < 0.8f) break;
                    Require(supply.FanCount == 1 && player.CurrentBody == otherRoomFan && otherRoomFan.IsControlled,
                        "An existing room Fan prevents respawn and safely replaces a disabled controlled host.");
                    UnityEngine.Object.Destroy(otherRoomFan.gameObject);
                    Next(17);
                    break;
                case 17:
                    if (Time.time - phaseAt < 1f) break;
                    Require(supply.SpawnedFan != null && supply.FanCount == 1 && player.CurrentBody == supply.SpawnedFan && player.CurrentBody.IsControlled,
                        "The last room Fan's disappearance must respawn and recover control, despite the outside Fan.");
                    Vector3 spawn = Get<Transform>(supply, "fanSpawnPoint").position;
                    Require(Mathf.Abs(supply.SpawnedFan.transform.position.x - spawn.x) < 0.05f &&
                        Mathf.Abs(supply.SpawnedFan.transform.position.y - spawn.y) < 0.3f && supply.SpawnedFan.IsGrounded(),
                        "Fan respawns centrally and settles onto the board under gravity.");
                    Debug.Log("ROOM SUPPLY FAN PASS: last-Fan replenishment and disabled/destroyed-host recovery.");
                    player.ReleaseAllTouchInput();
                    player = null; door = null; supply = null;
                    Next(20);
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/NewLevel02.unity", new LoadSceneParameters(LoadSceneMode.Single));
                    break;
                case 20:
                    Require(!supply.IsRoomActive && supply.SpawnedFan == null && supply.SpawnedBall == null && player.CurrentBody.Form == BodyForm.Star,
                        "Scene reload resets activation, supplies and ownership.");
                    if (tilt != null) Require(tilt.TriggerCount == 0 && tilt.LastSide == SeesawTilt2D.Side.None &&
                        Mathf.Abs(tilt.CurrentAngle) < 0.01f, "Reload resets board angle and contact latches.");
                    Debug.Log("ROOM SUPPLY RESET PASS");
                    Finish(0);
                    break;
            }
        }
        catch (Exception ex) { Debug.LogError("ROOM SUPPLY FAIL: " + ex); Finish(1); }
    }

    private static void Next(int value) { step = value; phaseAt = Time.time; }
    private static string Position(Component value) => value != null ? value.transform.position.ToString() : "gone";
    private static T Get<T>(UnityEngine.Object obj, string field) where T : UnityEngine.Object => new SerializedObject(obj).FindProperty(field).objectReferenceValue as T;
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Finish(int result)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(RunningKey, false);
        EditorApplication.Exit(result);
    }
}
