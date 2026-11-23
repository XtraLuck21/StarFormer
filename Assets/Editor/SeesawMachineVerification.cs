using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>Opt-in integration checks in a disposable project, including real tray/ratchet collisions.</summary>
[InitializeOnLoad]
public static class SeesawMachineVerification
{
    private const string Running = "SeesawMachineVerification.Running";
    private static PlayerPossessionController player;
    private static SeesawRoomStage stage;
    private static SeesawRoomSupplies supply;
    private static SeesawOverloadSequence load;
    private static SeesawMachineRoom machine;
    private static SeesawBouncyFinale finale;
    private static SeesawRoomExit lift;
    private static PossessableBody fan, spider, ball;
    private static SeesawSpiderCeilingButton button;
    private static Keyboard keyboard;
    private static int step, startScene;
    private static float phaseAt, pauseY;
    private static double deadline, pausedAt;
    private static Vector2 boardSize;

    static SeesawMachineVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Running, false)) return;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            deadline = EditorApplication.timeSinceStartup + 150;
            EditorApplication.update += Tick;
        };
    }

    public static void RunBatch()
    {
        // This is intentionally restricted to a copy, so no real analytics events are sent.
        if (!Application.isBatchMode) throw new InvalidOperationException("Run headless in a disposable test project.");
        AnalyticsSettings settings = Resources.Load<AnalyticsSettings>("AnalyticsSettings");
        if (settings != null && settings.CollectionEnabled)
            throw new InvalidOperationException("Disable data collection in the disposable copy before testing.");
        if(File.Exists("Library/SeesawMachineVerification.txt"))File.Delete("Library/SeesawMachineVerification.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/Level02.unity");
        SeesawMachineRoomSetup.Configure();
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }
    private static void Bind()
    {
        player = UnityEngine.Object.FindFirstObjectByType<PlayerPossessionController>();
        stage = UnityEngine.Object.FindFirstObjectByType<SeesawRoomStage>();
        supply = UnityEngine.Object.FindFirstObjectByType<SeesawRoomSupplies>();
        load = UnityEngine.Object.FindFirstObjectByType<SeesawOverloadSequence>();
        machine = UnityEngine.Object.FindFirstObjectByType<SeesawMachineRoom>();
        finale = UnityEngine.Object.FindFirstObjectByType<SeesawBouncyFinale>();
        lift = UnityEngine.Object.FindFirstObjectByType<SeesawRoomExit>();
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new Exception("Timeout step="+step+" body="+player?.CurrentBody?.Form+" position="+player?.CurrentBody?.transform.position+
                    " yellow="+load?.HasStarted+" impacts="+finale?.BounceCount+" attached="+spider?.GetComponent<HexagonSurfaceMovement>()?.AttachedSurface?.name);
            if (step == 15 && SceneManager.GetActiveScene().handle == startScene) return;
            if (player == null) Bind();
            if (player == null || player.CurrentBody == null) return;
            Require(!player.IsDead, "Unexpected death at step "+step);
            if (step == 4 || step == 5 || step == 11 || step == 12)
            {
                Camera camera = Ref<level2Camera>(lift,"roomCamera").GetComponent<Camera>();
                stage.GetCameraFrame(camera,-10f,out Vector3 center,out float size);
                Vector2 current=player.CurrentBody.transform.position;
                Require(Mathf.Abs(current.y-center.y)<size && Mathf.Abs(current.x-center.x)<size*camera.aspect,
                    "Current body must stay visible during Spider descent and the high approach bounce");
            }
            switch (step)
            {
                case 0:
                    Require(machine.ReferencesReady, "Scene bindings");
                    Require(lift.Surface.enabled && !lift.IsReady,"The complete deck exists before any gear, but boarding stays locked");
                    foreach(var socket in stage.GetComponentsInChildren<SeesawBallSocket>())
                    {
                        bool support=false;
                        foreach(var hit in Physics2D.RaycastAll(socket.transform.position+Vector3.up,Vector2.down,3f))
                        {
                            if(hit.collider.isTrigger)continue;
                            Require(hit.distance>1.8f,"Hole must be an actual gap in the board");
                            support=true;break;
                        }
                        Require(support,"A recessed floor safely collects each ball");
                    }
                    player.SetCheckpoint("SEESAW_ROOM", new Vector3(26.23f,-18.65f,0f),false);
                    supply.EnterRoom();
                    fan = supply.SpawnedFan;
                    MoveBody(player.CurrentBody, fan.Body.position + Vector2.left * .3f);
                    player.RequestPossession(player.CurrentBody, fan);
                    Next(1); break;
                case 1:
                    if (player.CurrentBody != fan) return;
                    MoveBody(fan,new Vector2(29.7f,-18.4f));
                    fan.GetComponent<FanBlower>().SetFacing(-1);
                    fan.GetComponent<FanBlower>().ToggleBlow();
                    Next(20); break;
                case 20:
                    if(!Ref<SeesawGear2D>(stage,"greenGear").IsRunning || supply.SpawnedBall==null || supply.SpawnedBall.HasBeenReleased)
                    {
                        if(Time.time-phaseAt>18f)throw new Exception("Left socket stalled: "+supply.SpawnedBall?.transform.position+" side="+Ref<SeesawTilt2D>(stage,"seesaw").LastSide+" fan="+fan.Body.position+" wind="+fan.GetComponent<FanBlower>().IsBlowing+" active="+supply.IsRoomActive+" stopped="+supply.FanSupplyStopped+" captured="+stage.GetComponentInChildren<SeesawBallSocket>().IsFilled);
                        return;
                    }
                    fan.GetComponent<FanBlower>().StopBlowing();
                    MoveBody(fan,new Vector2(26.1f,-18.4f));
                    fan.GetComponent<FanBlower>().SetFacing(1);
                    fan.GetComponent<FanBlower>().ToggleBlow();
                    Next(21); break;
                case 21:
                    if(!Ref<SeesawGear2D>(stage,"blueGear").IsRunning)
                    {
                        if(Time.time-phaseAt>18f)throw new Exception("Right socket stalled: "+supply.SpawnedBall?.transform.position+" side="+Ref<SeesawTilt2D>(stage,"seesaw").LastSide+" fan="+fan.Body.position+" wind="+fan.GetComponent<FanBlower>().IsBlowing+" active="+supply.IsRoomActive+" stopped="+supply.FanSupplyStopped+" captured="+stage.GetComponentInChildren<SeesawBallSocket>().IsFilled);
                        return;
                    }
                    foreach(var socket in stage.GetComponentsInChildren<SeesawBallSocket>())Require(socket.IsFilled,"Both holes receive a real wind-blown ball");
                    Debug.Log("MACHINE CHECK: full lift present initially, both real holes filled using Fan wind.");
                    Next(2); break;
                case 2:
                    if (!stage.IsUnlocked || stage.IsRevealing) return;
                    Require(fan != null && fan.gameObject.activeInHierarchy && player.CurrentBody == fan,"Fan must survive the blue/green drive latch");
                    Require(!Ref<SeesawGear2D>(load,"yellowGear").IsRunning,"No yellow drive before loading");
                    player.RestoreStarAfterFanPuzzle(new Vector2(28,-18.5f));
                    button=Ref<Transform>(stage,"yellowButton").GetComponent<SeesawSpiderCeilingButton>();
                    spider=Ref<PossessableBody>(button,"targetSpider");
                    ball=Ref<PossessableBody>(finale,"ball");
                    boardSize=Ref<BoxCollider2D>(finale,"board").size;
                    MoveBody(player.CurrentBody,spider.Body.position+Vector2.left*.2f);
                    player.RequestPossession(player.CurrentBody,spider);
                    Next(3); break;
                case 3:
                    if (player.CurrentBody!=spider) return;
                    Move(Key.D); Next(4); break;
                case 4:
                    Move(Key.D);
                    if (!button.IsActivated)
                    {
                        if(Time.time-phaseAt>22f)throw new Exception("Spider route stalled: "+spider.Body.position+" attached="+spider.GetComponent<HexagonSurfaceMovement>().AttachedSurface?.name);
                        return;
                    }
                    Move();
                    Require(Ref<SeesawGear2D>(load,"yellowGear").IsRunning,"Yellow powers its drive directly");
                    Debug.Log("MACHINE CHECK: Spider physically climbed the raised wall and pressed the ceiling wind gate.");
                    Next(5); break;
                case 5:
                    if (!load.IsPassageOpen) return;
                    Require(load.HasStarted && Ref<SeesawGear2D>(load,"yellowGear").IsRunning,"Button opens the passage without extra balls");
                    var cover=stage.GetComponent<SeesawUnderpassCover>();
                    Require(cover.HasRevealed && cover.Opacity<.01f,"The whole underpass is visible before Spider entry");
                    pauseY=Ref<BoxCollider2D>(load,"entrance").transform.position.y;
                    Time.timeScale=0f; pausedAt=EditorApplication.timeSinceStartup; Next(6); break;
                case 6:
                    if (EditorApplication.timeSinceStartup-pausedAt<.4) return;
                    Require(Mathf.Abs(Ref<BoxCollider2D>(load,"entrance").transform.position.y-pauseY)<.001f,"Paused mechanisms must stop");
                    Time.timeScale=1f;
                    spider.GetComponent<HexagonSurfaceMovement>().Release();
                    MoveBody(spider,ball.Body.position+Vector2.right*.2f);
                    player.RequestPossession(spider,ball);
                    Next(7); break;
                case 7:
                    if (player.CurrentBody!=ball) return;
                    MoveBody(ball,new Vector2(machine.ImpactSurface.bounds.center.x,-22.5f));
                    ball.Body.linearVelocity=Vector2.up*8f;
                    Next(8); break;
                case 8:
                    if (finale.BounceCount<4)
                    {
                        if (Time.time-phaseAt>12f) throw new Exception("Ratchet stalled count="+finale.BounceCount+" ball="+ball.Body.position+" velocity="+ball.Body.linearVelocity+" piston="+machine.ImpactSurface.bounds+" beam="+Ref<BoxCollider2D>(finale,"board").bounds);
                        return;
                    }
                    Require(finale.BounceCount==4,"Four physical piston collisions");
                    Next(9); break;
                case 9:
                    if (!lift.IsReady) return;
                    Require(machine.IsBridgeReady && finale.IsLeftBridgeReady,"Bridge must settle before boarding");
                    Require(finale.BounceCount==4,"No repeated counting after ratchet latches");
                    Require(Ref<BoxCollider2D>(finale,"board").size==boardSize,"Intact beam after final drive");
                    Debug.Log("MACHINE CHECK: Direct yellow switch, visible underpass, pause, 4 physical piston strikes, bridge and powered lift passed.");
                    Move(Key.A);
                    Next(11); break;
                case 11:
                    if(ball.Body.position.x>15.7f) { Move(Key.A); return; }
                    Move();
                    if(ball.BodyCollider.bounds.min.y<=Ref<BoxCollider2D>(finale,"board").bounds.max.y+.15f) return;
                    Move(Key.D); Next(12); break;
                case 12:
                    if(lift.IsTransferring || lift.HasTransferred) { Move(); Next(10); break; }
                    float offset=lift.Surface.bounds.center.x-ball.Body.position.x;
                    Move(Mathf.Abs(offset)<.25f?Key.None:offset>0f?Key.D:Key.A);
                    break;
                case 10:
                    if (!lift.HasTransferred) return;
                    Require(player.IsCurrentCheckpoint(SeesawRoomExit.SmileCheckpointId),"Original Smile Face arrival checkpoint");
                    Require(player.CurrentBody.Form==BodyForm.Star,"Original Star restored on the elevator");
                    // Re-enter the existing saved blue/green room stage in a fresh scene.
                    player.SetCheckpoint("SEESAW_ROOM",new Vector3(26.23f,-18.65f,0f),false);
                    stage.GetComponent<SeesawCheckpoint>()?.SaveBlueGreenStage();
                    startScene=SceneManager.GetActiveScene().handle;
                    Next(15);
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Level02.unity",new LoadSceneParameters(LoadSceneMode.Single));
                    break;
                case 15:
                    Bind();
                    if (!stage.IsUnlocked || stage.IsRevealing) return;
                    Require(!load.HasStarted && finale.BounceCount==0,"Retry resets yellow switch and ratchet only");
                    Require(!Ref<SeesawGear2D>(load,"yellowGear").IsRunning,"Retry does not inherit the yellow drive");
                    Finish(0,"PASS: complete initial carriage, two physical wind-filled sockets, Spider route, direct yellow switch, four-strike ratchet, bridge, lift/Star handoff, visible player and stage retry.");
                    break;
            }
        }
        catch(Exception e) { Finish(1,e.ToString()); }
    }
    private static T Ref<T>(object target,string name) where T:class
        => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target) as T;
    private static void MoveBody(PossessableBody body,Vector2 position)
    {
        body.transform.position=new Vector3(position.x,position.y,body.transform.position.z);
        body.Body.position=position;body.Body.linearVelocity=Vector2.zero;body.Body.angularVelocity=0f;
        Physics2D.SyncTransforms();
    }
    private static void Move(Key key=Key.None) => InputSystem.QueueStateEvent(keyboard,key==Key.None?new KeyboardState():new KeyboardState(key));
    private static void Next(int value) { step=value;phaseAt=Time.time;Debug.Log("MACHINE STEP "+step); }
    private static void Require(bool value,string message) { if(!value)throw new Exception(message); }
    private static void Finish(int result,string message)
    {
        Time.timeScale=1f;SessionState.SetBool(Running,false);EditorApplication.update-=Tick;
        File.WriteAllText("Library/SeesawMachineVerification.txt",message);
        if(result==0)Debug.Log(message);else Debug.LogError(message);
        EditorApplication.Exit(result);
    }
}
