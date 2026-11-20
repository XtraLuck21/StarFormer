using TMPro;
using UnityEngine;

// The morning uses the same movement and jump controls as the rest of Level 2.
public sealed class SchoolDayJourney : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private level2Camera levelCamera;
    [SerializeField] private SpikeWallEnemyChase alarm;
    [SerializeField] private DarkStarPassage passage;
    [SerializeField] private GameObject busPlatform;

    [SerializeField] private StarRoomDoor schoolEntrance;

    private const float Ground = 10.51f, BusX = -26.55f;
    private static readonly Vector2 Stop = new Vector2(-23f, -22.70f);
    private static readonly float[] Stations = { -72f, -66.5f, -61f };
    private static readonly Color Ink = new Color32(49,66,73,255), Honey = new Color32(244,181,69,255),
        Cream = new Color32(255,234,190,255), Teal = new Color32(87,151,149,255),
        Glass = new Color32(177,214,213,255), Timber = new Color32(148,104,76,255);

    private enum Stage { Ready, Chase, Boarding, Falling, School }
    private Stage stage;
    private SchoolMorningScenery scenery;
    private Transform bus, busDoor;
    private readonly Transform[] tokens = new Transform[3];
    private SpriteRenderer passenger;
    private int savedOrder;
    private static readonly Vector2 BusLanding = new Vector2(Stop.x - .7f, Stop.y - .4f);
    private readonly Transform[] students = new Transform[5];
    private readonly float[] taskTime = { -1, -1, -1 };
    private static readonly Vector2[] MeetingSpots = {
        new Vector2(-17.55f,-22.83f), new Vector2(-15.05f,-22.71f),
        new Vector2(-12.5f,-22.83f), new Vector2(-10f,-22.70f), new Vector2(-7.45f,-22.82f)
    };
    // A null line is a silent fist bump.
    private static readonly string[] Greetings = { "Hi!", "Hello!", "What's up?", "How are you?", null };
    private enum StudentStage { Waiting, Greeting, Running, Inside }
    private readonly StudentStage[] studentStages = new StudentStage[MeetingSpots.Length];
    private readonly float[] greetingAges = new float[MeetingSpots.Length];
    private readonly Transform[] bubbles = new Transform[MeetingSpots.Length];
    private Transform starFist, friendFist, bumpFlash;
    private bool greetingOwnsInput;
    private float timer;
    private PossessableBody rider;
    private bool savedInput, savedSimulation;
    private Vector3 boardFrom;
    private static Vector2 P(float x, float y) => new Vector2(x, y);

    private Transform Place(SchoolSceneDrawing drawing, string name, float x, float y, int order, Transform parent = null)
    {
        Transform item = scenery.Draw(drawing, name, order, parent).transform;
        item.localPosition = new Vector3(x, y, 0);
        return item;
    }

    internal void BuildArt(SchoolMorningScenery owner)
    {
        scenery = owner;
        var room = new SchoolSceneDrawing();
        room.Rect(-73f,14.6f,32.4f,8.3f,new Color32(239,220,197,255));

        // One window at the breakfast table gives the interior a sense of morning.
        room.Rect(-66.5f,13.6f,2.5f,1.85f,Cream);
        room.Rect(-66.5f,13.6f,2.26f,1.62f,Glass);
        room.Rect(-66.5f,13.6f,.08f,1.62f,Cream);
        room.Rect(-76.65f,10.96f,3.1f,.36f,Timber);
        room.Rect(-78.15f,11.17f,.23f,1.32f,Timber);
        room.Rect(-76.65f,11.23f,3.1f,.26f,Cream);
        room.Rect(-76.22f,11.24f,2.24f,.26f,Teal);
        room.Rect(-77.67f,11.4f,.74f,.19f,new Color32(255,247,221,255));
        Place(room,"Bedroom",0,0,-8);

        for (int i = 0; i < tokens.Length; i++)
        {
            var disc = new SchoolSceneDrawing();
            disc.Circle(Vector2.zero,.61f,new Color32(231,183,91,255));
            disc.Circle(Vector2.zero,.55f,Cream);
            tokens[i] = Place(disc,"Morning pickup",Stations[i],Ground+.87f,3);
        }
        var item = new SchoolSceneDrawing();
        item.Polygon(Teal,P(-.26f,.32f),P(.26f,.32f),P(.53f,.04f),P(.32f,-.12f),P(.25f,-.04f),P(.25f,-.44f),P(-.25f,-.44f),P(-.25f,-.04f),P(-.32f,-.12f),P(-.53f,.04f));
        item.Rect(0,-.07f,.045f,.6f,Cream);
        Place(item,"School jacket",0,0,126,tokens[0]);
        item=new SchoolSceneDrawing();
        item.Rect(0,0,.58f,.47f,Timber); item.Circle(P(0,.18f),.33f,Timber);
        item.Rect(0,.02f,.48f,.37f,Honey); item.Circle(P(0,.17f),.26f,Honey);
        Place(item,"Breakfast toast",0,0,4,tokens[1]);
        item=new SchoolSceneDrawing(); item.Rect(0,0,.75f,.11f,Teal);
        item.Rect(.24f,.12f,.25f,.18f,Glass);
        var brush=Place(item,"Toothbrush",0,0,5,tokens[2]);
        brush.localRotation=Quaternion.Euler(0,0,20);

        BuildBus();
        var curb=new SchoolSceneDrawing();
        curb.Rect(-14.3f,-23.06f,20.5f,.08f,new Color32(125,142,141,255));
        Place(curb,"Schoolyard curb",0,0,90);
        Color[] colors={Teal,new Color32(222,152,122,255),new Color32(156,149,192,255),Honey,Glass};
        for(int i=0;i<students.Length;i++)
        {
            item=new SchoolSceneDrawing();item.Star(Vector2.zero,.32f+i%2*.05f,colors[i]);
            item.Rect(-.2f,-.02f,.17f,.25f,Ink);item.Rect(-.2f,.04f,.14f,.07f,Cream);
            item.Circle(P(.10f,.06f),.034f,Ink);
            item.Circle(P(.20f,.06f),.034f,Ink);
            Vector2 spot=MeetingSpots[i];
            students[i]=Place(item,"Waiting classmate",spot.x,spot.y,122);
            students[i].localScale=PScale(-1,1);
            if (Greetings[i] != null)
                bubbles[i] = BuildBubble(Greetings[i], spot);
        }
        BuildGreetings();
    }

    private Transform BuildBubble(string words, Vector2 spot)
    {
        var bubble = new SchoolSceneDrawing();
        float halfWidth = words.Length > 12 ? 1.65f : 1.06f;
        bubble.Rect(0,0,halfWidth*2f,.62f,Cream);
        bubble.Circle(P(-halfWidth,0),.31f,Cream);
        bubble.Circle(P(halfWidth,0),.31f,Cream);
        bubble.Polygon(Cream,P(-.22f,-.28f),P(-.08f,-.50f),P(.12f,-.28f));
        var speech=Place(bubble,"Class greeting",spot.x,spot.y+.95f,135);
        var caption=new GameObject("Spoken words",typeof(TextMeshPro));
        caption.hideFlags=HideFlags.HideAndDontSave;
        caption.transform.SetParent(speech,false);
        var speechText=caption.GetComponent<TextMeshPro>();
        speechText.text=words;
        speechText.alignment=TextAlignmentOptions.Center;
        speechText.fontSize=3.5f;
        speechText.fontStyle=FontStyles.Bold;
        speechText.color=Ink;
        speechText.rectTransform.sizeDelta=P(halfWidth*2f+.3f,.6f);
        speechText.renderer.sortingOrder=136;
        speech.gameObject.SetActive(false);
        return speech;
    }

    private void BuildGreetings()
    {
        starFist=BuildFist(Honey);
        friendFist=BuildFist(Glass);
        var flash=new SchoolSceneDrawing();
        for (int i=0;i<5;i++)
        {
            float angle=(25+i*32)*Mathf.Deg2Rad;
            Vector2 direction=P(Mathf.Cos(angle),Mathf.Sin(angle));
            flash.Line(direction*.16f,direction*.31f,.034f,Cream);
        }
        bumpFlash=Place(flash,"Fist bump contact",0,0,134);
        bumpFlash.gameObject.SetActive(false);
    }

    private Transform BuildFist(Color sleeve)
    {
        var hand=new SchoolSceneDrawing();
        hand.Rect(-.12f,0,.25f,.13f,sleeve);
        hand.Circle(Vector2.zero,.12f,Cream);
        hand.Circle(P(-.03f,.085f),.065f,Cream);
        var fist=Place(hand,"Greeting fist",0,0,132);
        fist.gameObject.SetActive(false);
        return fist;
    }

    private void BuildBus()
    {
        var bus=new SchoolSceneDrawing();
        bus.Rect(2.15f,1.38f,5.75f,1.95f,Honey);
        bus.Rect(2.05f,2.4f,5.45f,.27f,Honey);
        bus.Rect(4.92f,1.06f,.78f,.94f,Honey);
        bus.Rect(2.1f,.93f,5.55f,.12f,Ink);
        for(int i=0;i<4;i++) bus.Rect(1.3f+i*.9f,1.94f,.76f,.68f,Glass);
        foreach(float wheel in new[]{.36f,4.34f})
        {bus.Circle(P(wheel,.5f),.48f,Ink);bus.Circle(P(wheel,.5f),.22f,Cream);}
        bus.Rect(0,1.36f,1.05f,1.68f,Ink);
        bus.Rect(0,.57f,1.05f,.15f,Cream);
        this.bus = Place(bus,"School bus",BusX,Ground,108);
        var panel=new SchoolSceneDrawing();
        panel.Rect(.5f,.84f,1f,1.68f,Teal);panel.Rect(.5f,1.16f,.78f,.75f,Glass);
        busDoor=Place(panel,"School bus folding door",-.5f,.61f,112,this.bus);
        busDoor.localScale=new Vector3(.08f,1,1);
    }

    private void Update()
    {
        if(!Application.isPlaying || !bus || player==null || player.CurrentBody==null ||
           player.IsDead || player.IsRestarting || Time.timeScale<=0) return;
        var star=player.CurrentBody;
        if(star.Form!=BodyForm.Star || passage.IsComplete)
        {
            HideCompany();
            return;
        }
        Vector2 pos=star.Body.position;
        if(passage.HasLanded && stage!=Stage.Falling && stage!=Stage.Boarding && stage!=Stage.School)
            ArriveWithClass();
        if(stage==Stage.School) { MeetClass(star); return; }
        if(stage==Stage.Boarding || stage==Stage.Falling) { Travel(); return; }
        CollectOnContact(star);
        if (stage == Stage.Ready && star.IsReady && taskTime[0] >= 0f)
        {
            stage = Stage.Chase;
            alarm.gameObject.SetActive(true);
            Physics2D.SyncTransforms();
            levelCamera.BeginDoorChase();
        }
        if (stage != Stage.Chase) return;
        if(pos.x>=BusX-.55f && pos.y>11.36f)
        {
            stage=Stage.Boarding;timer=0;rider=star;boardFrom=star.transform.position;
            savedInput=player.enabled;savedSimulation=rider.Body.simulated;
            player.enabled=false;rider.Body.linearVelocity=Vector2.zero;rider.Body.simulated=false;
            alarm.EndChase();
            levelCamera.BeginSchoolBusTrip(boardFrom, Stop);
            passenger = star.GetComponentInChildren<StarVisual>().GetComponent<SpriteRenderer>();
            savedOrder = passenger.sortingOrder;
            passenger.sortingOrder = 120;
        }
    }

    private static Vector3 PScale(float x,float y) => new Vector3(x,y,1);
    private void CollectOnContact(PossessableBody star)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            Transform token = tokens[i];
            if (taskTime[i] < 0)
            {
                float bob = .12f * Mathf.Sin(Time.time * 4 + i);
                token.position = P(Stations[i], Ground + .84f + bob);
                Vector2 center = token.position;
                if ((star.BodyCollider.ClosestPoint(center) - center).sqrMagnitude > .61f * .61f) continue;
                taskTime[i] = 0;
            }
            if (taskTime[i] >= 1) continue;
            taskTime[i] = Mathf.MoveTowards(taskTime[i], 1, Time.deltaTime * 6);
            float t = taskTime[i];
            token.position = Vector3.Lerp(token.position, star.transform.position, t);
            token.localScale = Vector3.one * (1-t);
            if (t >= 1) token.gameObject.SetActive(false);
        }
    }

    private void SeatRider(Vector3 position)
    {
        rider.transform.position=position;
        rider.Body.position=position;
    }
    private void Travel()
    {
        timer += Time.deltaTime;
        if(stage == Stage.Boarding)
        {
            float t=Mathf.Clamp01(timer/.24f);
            SeatRider(Vector3.Lerp(boardFrom,bus.TransformPoint(P(1.3f,1.91f)),t)
                      + Vector3.up*(.22f*Mathf.Sin(t*Mathf.PI)));
            busDoor.localScale=PScale(Mathf.Lerp(.08f,1,t),1);
            levelCamera.FrameSchoolBusTrip(rider.transform.position, 0f);
            if(t<1)return;
            busPlatform.SetActive(false);
            stage=Stage.Falling;
            timer=0;
        }
        float progress=Mathf.Clamp01(timer/1.08f);
        bus.position=P(Mathf.Lerp(BusX,BusLanding.x,Mathf.SmoothStep(0,1,progress)),
                       Mathf.Lerp(Ground,BusLanding.y,progress*progress));
        bus.rotation=Quaternion.Euler(0,0,-4f*Mathf.Sin(progress*Mathf.PI));
        SeatRider(bus.TransformPoint(P(1.3f,1.91f)));
        levelCamera.FrameSchoolBusTrip(rider.transform.position, progress);
        if(progress<1)return;
        float exit=Mathf.Clamp01((timer-1.08f)/.18f);
        busDoor.localScale=PScale(Mathf.Lerp(1,.08f,exit),1);
        SeatRider(Vector3.Lerp(bus.TransformPoint(P(1.3f,1.91f)),Stop,exit));
        if(exit<1)return;
        RestoreRider();
        Physics2D.SyncTransforms();
        passage.RestoreAtBottom(player.CurrentBody);
        ArriveWithClass();
    }
    private void RestoreRider()
    {
        if(!rider)return;
        if (passenger) passenger.sortingOrder=savedOrder;
        rider.Body.simulated=savedSimulation;
        player.enabled=savedInput;
        rider=null;
    }
    private void ArriveWithClass()
    {
        stage=Stage.School; timer=0;
        foreach (var token in tokens) token.gameObject.SetActive(false);
        bus.position=BusLanding;
        busDoor.localScale=PScale(.08f,1);
        busPlatform.SetActive(false);
    }

    private void MeetClass(PossessableBody star)
    {
        timer+=Time.deltaTime;
        bus.position=P(BusLanding.x-Mathf.Clamp(timer-1f,0,3)*4f,BusLanding.y);
        for (int i = 0; i < students.Length; i++)
        {
            Transform mate = students[i];
            switch (studentStages[i])
            {
                case StudentStage.Waiting:
                    mate.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(timer * 2.5f + i) * 4f);
                    Vector2 spot = mate.position;
                    if ((star.BodyCollider.ClosestPoint(spot) - spot).sqrMagnitude > .30f * .30f) break;

                    studentStages[i] = StudentStage.Greeting;
                    mate.localRotation = Quaternion.identity;
                    mate.localScale = PScale(star.Body.position.x < spot.x ? -1 : 1, 1);
                    if (bubbles[i] != null)
                        bubbles[i].gameObject.SetActive(true);
                    else
                    {
                        // Hold just long enough for the two hands to meet.
                        greetingOwnsInput = player.enabled;
                        player.enabled = false;
                        star.Body.linearVelocity = Vector2.zero;
                    }
                    break;

                case StudentStage.Greeting:
                    greetingAges[i] += Time.deltaTime;
                    if (bubbles[i] != null)
                    {
                        if (greetingAges[i] < .24f) break;
                    }
                    else
                    {
                        float progress = Mathf.Clamp01(greetingAges[i] / .42f);
                        AnimateBump(star.Body.position, mate.position, progress);
                        if (progress < 1f) break;
                        starFist.gameObject.SetActive(false);
                        friendFist.gameObject.SetActive(false);
                        RestoreGreetingInput();
                    }
                    studentStages[i] = StudentStage.Running;
                    mate.localScale = Vector3.one;
                    break;

                case StudentStage.Running:
                    float gateX = schoolEntrance.transform.position.x;
                    float x = Mathf.MoveTowards(mate.position.x, gateX, 7.6f * Time.deltaTime);
                    mate.position = P(x, MeetingSpots[i].y + Mathf.Abs(Mathf.Sin(timer * 12f + i)) * .16f);
                    if (bubbles[i] != null && bubbles[i].gameObject.activeSelf)
                    {
                        greetingAges[i] += Time.deltaTime;
                        bubbles[i].position = mate.position + Vector3.up * .95f;
                        if (greetingAges[i] >= .85f || x >= gateX)
                            bubbles[i].gameObject.SetActive(false);
                    }
                    if (x >= gateX)
                    {
                        mate.gameObject.SetActive(false);
                        studentStages[i] = StudentStage.Inside;
                    }
                    break;
            }
        }
    }

    private void AnimateBump(Vector2 star, Vector2 friend, float progress)
    {
        float extend = Mathf.Sin(progress * Mathf.PI);
        Vector2 contact = (star + friend) * .5f;
        float facing = friend.x >= star.x ? 1f : -1f;
        PoseFist(starFist, star, contact, facing, extend);
        PoseFist(friendFist, friend, contact, -facing, extend);
        bumpFlash.position = contact;
        bumpFlash.gameObject.SetActive(extend > .88f);
    }

    private static void PoseFist(Transform fist,Vector2 shoulder,Vector2 contact,float facing,float extend)
    {
        fist.gameObject.SetActive(true);
        fist.localScale=PScale(facing,1);
        fist.position=Vector2.Lerp(shoulder+P(facing*.18f,.02f),contact-P(facing*.09f,0),extend);
    }
    private void RestoreGreetingInput()
    {
        if(!greetingOwnsInput)return;
        greetingOwnsInput=false;
        player.enabled=true;
    }
    private void HideCompany()
    {
        foreach(var mate in students) if(mate && mate.gameObject.activeSelf)mate.gameObject.SetActive(false);
        foreach (var bubble in bubbles) if (bubble && bubble.gameObject.activeSelf) bubble.gameObject.SetActive(false);
    }
    private void OnDisable() { RestoreRider(); RestoreGreetingInput(); }
}
