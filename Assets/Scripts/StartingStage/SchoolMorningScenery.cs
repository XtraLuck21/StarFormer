using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public sealed class SchoolMorningScenery : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] bridgePlanks;
    [SerializeField] private BoxCollider2D leftChute;
    [SerializeField] private BoxCollider2D landingWall;
    [SerializeField] private BoxCollider2D landingFloor;
    [SerializeField] private Transform schoolDoor;
    [SerializeField] private Transform deskRuler;
    [SerializeField] private Transform smileRoom;
    [SerializeField] private SeesawRoomStage seesawStage;
    [SerializeField] private BoxCollider2D seesawFloor;
    [SerializeField] private SeesawRoomExit seesawExit;
    [SerializeField] private BoxCollider2D seesawLeftWall;
    [SerializeField] private BoxCollider2D seesawRightWall;
    [SerializeField] private bool showRulerMarkings = true;
    private readonly List<GameObject> drawings = new List<GameObject>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private Material material;
    private SchoolPlaygroundRain rain;
    private SchoolRainStreaks smileRain;
    private SmileFaceCompletion smileCompletion;
    private MeshRenderer smileStorm;
    private MeshRenderer smileSun;
    private MaterialPropertyBlock weatherTint;
    private static readonly int TintColor = Shader.PropertyToID("_Color");
    private float smileSunRadius;
    private bool rebuild;
    private static readonly Color Ink = new Color32(39,59,65,255);
    private static readonly Color Wood = new Color32(126,85,55,255);
    private static readonly Color Warm = new Color32(255,212,125,255);
    private void OnEnable() => rebuild=true;
    private void OnValidate() => rebuild=true;
    private void Update()
    {
        if(rebuild) { rebuild=false; Build(); }
        rain?.Update(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (smileCompletion == null) return;
        float overcast = 1f - smileCompletion.ClearSky;
        smileRain.Update(Time.deltaTime, overcast);
        SetWeatherOpacity(smileStorm, overcast);
        float sun = smileCompletion.SunAmount;
        SetWeatherOpacity(smileSun, sun);
        if (smileSun.enabled)
            smileSun.transform.localScale = Vector3.one * (smileSunRadius * Mathf.Lerp(.96f, 1f, sun));
    }

    private void SetWeatherOpacity(MeshRenderer renderer, float opacity)
    {
        renderer.enabled = opacity > 0f;
        if (!renderer.enabled) return;
        weatherTint.SetColor(TintColor, new Color(1f, 1f, 1f, opacity));
        renderer.SetPropertyBlock(weatherTint);
    }
    internal GameObject Draw(SchoolSceneDrawing art,string name,int order,Transform parent=null)
        => Draw(art, name, order, out _, parent);

    internal GameObject Draw(SchoolSceneDrawing art, string name, int order, out Mesh mesh, Transform parent = null)
    {
        var obj=art.Build(name,parent!=null?parent:transform,material,order,out mesh);
        drawings.Add(obj);meshes.Add(mesh);return obj;
    }
    private void Build()
    {
        Clear();
        material=new Material(Shader.Find("Sprites/Default")) { hideFlags=HideFlags.HideAndDontSave };
        material.mainTexture=Texture2D.whiteTexture;
        BuildStreet(); BuildAlley();
        BuildSeesawPlayground();
        BuildSmileCourtyard();
        if(bridgePlanks!=null)foreach(var plank in bridgePlanks)
        {
            if(plank==null)continue;
            var a=new SchoolSceneDrawing();
            a.Rect(0,0,1,1,Wood); a.Rect(0,.38f,1,.24f,new Color32(198,155,99,255));
            a.Rect(-.49f,0,.018f,1,new Color32(139,105,75,255));
            Draw(a,"Old footbridge planks",plank.sortingOrder+1,plank.transform);
        }
        GetComponent<SchoolDayJourney>().BuildArt(this);
    }
    private void BuildSeesawPlayground()
    {
        if (deskRuler == null) return;
        var yard = new SchoolSceneDrawing();
        yard.ShadedRect(-2f, -44f, 58f, 4f, YardColor(151, 165, 151), YardColor(93, 128, 130));
        Color roof = YardColor(110, 132, 124);
        Color windows = YardColor(110, 137, 133);
        yard.Rect(18f, -16.75f, 10.4f, 2.7f, YardColor(138, 154, 143));
        yard.Rect(18f, -15.4f, 10.8f, .19f, roof);
        for (int column = 0; column < 4; column++)
        {
            float x = 14.45f + column * 2.35f;
            yard.Rect(x, -16.6f, 1.65f, .68f, windows);
        }
        Color leaves = YardColor(107, 135, 121);
        foreach (float x in new[] { 10.8f, 44f })
        {
            yard.Rect(x, -17.45f, .22f, 1.5f, roof);
            yard.Circle(new Vector2(x, -16.25f), 1.08f, leaves);
            yard.Circle(new Vector2(x + .66f, -16.55f), .74f, leaves);
        }
        yard.Rect(28f, -31f, 60f, 26f, YardColor(123, 144, 130));
        yard.ShadedRect(-2f, -44f, 58f, -20.2f, YardColor(108, 101, 94), YardColor(143, 130, 118));
        Color chalk = YardColor(154, 141, 128);
        yard.Rect(28f, -21.8f, 60f, .035f, chalk);
        yard.Rect(28f, -23.5f, 60f, .035f, chalk);
        yard.Rect(40.1f, -17.45f, .16f, 3.45f, roof);
        yard.Rect(40.1f, -15.8f, 1.8f, 1.12f, YardColor(140, 154, 143));
        yard.Rect(40.1f, -16.05f, .68f, .09f, roof);
        Draw(yard, "Elementary school playground", -39);
        if (Application.IsPlaying(gameObject) && seesawExit != null && seesawExit.Ceiling != null &&
            seesawLeftWall != null && seesawRightWall != null)
            rain = new SchoolPlaygroundRain(this, seesawExit, seesawLeftWall, seesawRightWall);
        if (seesawStage != null && seesawFloor != null)
            seesawStage.BuildPlaygroundDetails(this, seesawFloor.bounds.max.y);

        var mechanism = new SchoolSceneDrawing();
        mechanism.Rect(12.55f,-13.55f,1.2f,.5f,Wood);
        mechanism.Circle(new Vector2(12.55f,-13.55f),.18f,Ink);
        Draw(mechanism, "Desk pencil holder", -12);

        var pencil = new SchoolSceneDrawing();
        pencil.Rect(12.55f, -12.94f, .24f, .92f, new Color32(236, 184, 77, 255));
        pencil.Rect(12.61f, -12.94f, .045f, .92f, new Color32(242, 232, 207, 255));
        pencil.Rect(12.55f, -12.46f, .25f, .12f, new Color32(198, 126, 111, 255));
        Draw(pencil, "Pencil in desk sharpener", -11);

        if (!showRulerMarkings) return;
        var ruler = new SchoolSceneDrawing();
        ruler.Rect(0, 0, 1f, 1f, new Color32(224, 184, 109, 255));
        ruler.Rect(0, -.38f, 1f, .20f, new Color32(193, 145, 76, 255));
        for (int i = 0; i <= 60; i++)
        {
            float length = i % 5 == 0 ? .29f : .14f;
            ruler.Rect(-.49f + i * .98f / 60f, .49f - length / 2f, .0017f, length, Ink);
        }
        Draw(ruler, "Tilting wooden ruler", 1, deskRuler);

        static Color YardColor(byte r, byte g, byte b)
        {
            Color color = new Color32(r, g, b, 255);
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        }
    }

    private void BuildSmileCourtyard()
    {
        if (smileRoom == null) return;
        if (!Application.IsPlaying(gameObject) || !smileRoom.TryGetComponent(out smileCompletion))
        {
            Draw(BuildSmileCourtyardArt(false), "Smile face school courtyard in rain", -120, smileRoom);
            smileCompletion = null;
            return;
        }

        Draw(BuildSmileCourtyardArt(true), "Smile face school courtyard in sunshine", -120, smileRoom);
        smileStorm = Draw(BuildSmileCourtyardArt(false), "Smile face school courtyard in rain", -119, smileRoom)
            .GetComponent<MeshRenderer>();
        weatherTint ??= new MaterialPropertyBlock();
        Bounds face = smileCompletion.FaceBounds;
        Rect rainArea = Rect.MinMaxRect(face.min.x - 10f, face.min.y - 8f, face.max.x + 10f, face.max.y + 9f);
        int faceOrder = smileCompletion.FaceSortingOrder;
        smileRain = new SchoolRainStreaks(this, "Smile face rain", faceOrder + 3, rainArea, 360, 921,
            streakWidth: .075f, lengthScale: 1.8f, opacityScale: 1.65f);
        smileSun = Draw(BuildSmileSunRays(), "Smile face sun rays", faceOrder - 1, smileRoom)
            .GetComponent<MeshRenderer>();
        smileSun.transform.position = face.center;
        smileSunRadius = face.extents.y * .89f;
        smileSun.enabled = false;
    }

    private static SchoolSceneDrawing BuildSmileCourtyardArt(bool sunny)
    {
        var yard = new SchoolSceneDrawing();
        Color horizon = sunny ? new Color32(199, 221, 211, 255) : new Color32(111, 137, 150, 255);
        Color sky = sunny ? new Color32(106, 177, 218, 255) : new Color32(73, 97, 119, 255);
        yard.ShadedRect(-12, -74, 104, -32,
            sunny ? new Color32(251, 220, 170, 255) : new Color32(169, 176, 176, 255), horizon);
        yard.ShadedRect(-12, -32, 104, -5, horizon, sky);
        Vector2[] ridge = {
            new Vector2(-12, -36), new Vector2(8, -32), new Vector2(22, -35),
            new Vector2(39, -39), new Vector2(54, -33), new Vector2(63, -27.6f),
            new Vector2(74, -31), new Vector2(87, -28), new Vector2(104, -34)
        };
        Color hills = sunny ? new Color32(157, 171, 141, 255) : new Color32(110, 133, 139, 255);
        for (int i = 1; i < ridge.Length; i++)
            yard.Polygon(hills, new Vector2(ridge[i - 1].x, -49), ridge[i - 1],
                ridge[i], new Vector2(ridge[i].x, -49));

        // Frame the face with two distant school wings. Keep the forehead against open sky.
        Color plaster = sunny ? new Color32(210, 186, 164, 255) : new Color32(141, 150, 155, 255);
        Color trim = sunny ? new Color32(162, 146, 129, 255) : new Color32(109, 122, 131, 255);
        Color glass = sunny ? new Color32(232, 209, 167, 255) : new Color32(157, 174, 182, 255);
        foreach (float x in new[] { 14f, 75f })
        {
            yard.Rect(x, -44, 17, 9, plaster);
            yard.Rect(x, -39.5f, 17.5f, .35f, trim);
            for (int column = 0; column < 7; column++)
            {
                float windowX = x - 6.6f + column * 2.2f;
                yard.Rect(windowX, -41.5f, 1.4f, 1.7f, glass);
                yard.Rect(windowX, -44.6f, 1.4f, 1.7f, glass);
            }
        }
        Color canopy = sunny ? new Color32(137, 161, 122, 255) : new Color32(107, 128, 128, 255);
        foreach (float x in new[] { 9f, 21f, 27f, 64f, 72f, 81f })
        {
            yard.Rect(x, -46.8f, .5f, 4.6f, trim);
            yard.Circle(new Vector2(x, -44), 2.6f, canopy);
            yard.Circle(new Vector2(x + 1.6f, -44.6f), 1.8f, canopy);
        }
        yard.ShadedRect(-12, -74, 104, -48.7f,
            sunny ? new Color32(157, 158, 145, 255) : new Color32(118, 130, 134, 255),
            sunny ? new Color32(193, 187, 159, 255) : new Color32(145, 151, 150, 255));
        yard.ShadedRect(-12, -58, 104, -52.5f,
            sunny ? new Color32(174, 137, 139, 255) : new Color32(127, 131, 140, 255),
            sunny ? new Color32(206, 161, 145, 255) : new Color32(148, 153, 157, 255));
        Color lane = sunny ? new Color32(229, 194, 166, 255) : new Color32(169, 182, 183, 255);
        yard.Rect(46, -53.6f, 116, .07f, lane);
        yard.Rect(46, -55.4f, 116, .09f, lane);
        return yard;
    }

    private static SchoolSceneDrawing BuildSmileSunRays()
    {
        var sun = new SchoolSceneDrawing();
        Color gold = GlowColor(255, 192, 68, 106);
        Color bright = GlowColor(255, 220, 93, 236);
        Color inner = GlowColor(255, 244, 165, 255);
        Color clear = GlowColor(255, 193, 79, 0);
        const int rayCount = 14;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * 2f * Mathf.PI / rayCount;
            Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 tangent = new Vector2(-outward.y, outward.x);
            float reach = i % 3 == 0 ? 1.55f : 1.43f;
            sun.ShadedQuad(outward - tangent * .045f, outward * reach - tangent * .13f,
                outward * reach + tangent * .13f, outward + tangent * .045f, gold, clear);
        }

        // The bright band follows the head, then feathers into the tips of the rays.
        const int segments = rayCount * 8;
        for (int i = 0; i < segments; i++)
        {
            float start = i * 2f * Mathf.PI / segments;
            float end = (i + 1) * 2f * Mathf.PI / segments;
            Vector2 a = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
            Vector2 b = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
            float crestA = .5f + .5f * Mathf.Cos(start * rayCount);
            float crestB = .5f + .5f * Mathf.Cos(end * rayCount);
            crestA *= crestA;
            crestB *= crestB;
            Vector2 brightA = a * (1.015f + .10f * crestA);
            Vector2 brightB = b * (1.015f + .10f * crestB);
            sun.ShadedQuad(a * .90f, brightA, brightB, b * .90f, inner, bright);
            sun.ShadedQuad(brightA, a * (1.16f + .23f * crestA), b * (1.16f + .23f * crestB),
                brightB, bright, clear);
        }
        return sun;

        static Color GlowColor(byte r, byte g, byte b, byte alpha)
        {
            Color color = new Color32(r, g, b, alpha);
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        }
    }

    private void BuildStreet()
    {
        var a=new SchoolSceneDrawing();
        a.Rect(-59,0,122,120,new Color32(218,234,227,255));
        Draw(a, "Morning sky", -45);
        a = new SchoolSceneDrawing();
        Color facade = new Color32(195,214,208,255);
        Color roof = new Color32(182,204,198,255);
        Color window = new Color32(211,226,218,255);
        foreach (Vector2 cottage in new[] { new Vector2(-45, 4.1f), new Vector2(-32, 2.7f) })
        {
            float x = cottage.x;
            float eaves = 10.1f + cottage.y;
            a.Rect(x, (10.1f + eaves) * .5f, 5.1f, cottage.y, facade);
            a.Polygon(roof, new Vector2(x - 2.85f, eaves), new Vector2(x + .35f, eaves + 1.35f),
                new Vector2(x + 2.85f, eaves));
            a.Rect(x - 1.1f, eaves - 1.1f, .7f, .85f, window);
            a.Rect(x + 1.1f, eaves - 1.1f, .7f, .85f, window);
        }
        Draw(a, "Morning neighborhood", -40);
        BuildStreetTrees();
        a = new SchoolSceneDrawing();
        Color timber = new Color32(166,149,123,255);
        for (float x = -75; x < -27; x += 10.5f)
        {
            a.Rect(x, 8.25f, .21f, 3.5f, timber);
            a.Line(new Vector2(x, 9.05f), new Vector2(x + .95f, 10f), .13f, timber);
        }
        Draw(a, "Footbridge supports", -10);
    }

    private void BuildStreetTrees()
    {
        // Layer soft silhouettes behind the houses, with two nearer trees between them.
        var distant = new SchoolSceneDrawing();
        Color farLeaves = new Color32(178, 201, 180, 255);
        Color farLight = new Color32(191, 210, 188, 255);
        Color farTrunk = new Color32(181, 174, 151, 255);
        StreetTree(distant, -53.2f, 4.3f, farLeaves, farLight, farTrunk);
        StreetTree(distant, -42f, 3.7f, farLeaves, farLight, farTrunk);
        StreetTree(distant, -30f, 4.8f, farLeaves, farLight, farTrunk);
        Draw(distant, "Distant roadside trees", -42);

        var nearby = new SchoolSceneDrawing();
        Color leaves = new Color32(145, 177, 149, 255);
        Color light = new Color32(165, 191, 160, 255);
        Color trunk = new Color32(166, 149, 123, 255);
        StreetTree(nearby, -48.6f, 5.6f, leaves, light, trunk);
        StreetTree(nearby, -36.8f, 5f, leaves, light, trunk);
        Draw(nearby, "Near roadside trees", -38);
    }

    private static void StreetTree(SchoolSceneDrawing art, float x, float height,
        Color leaves, Color light, Color trunk)
    {
        const float ground = 10.1f;
        art.Rect(x, ground + height * .3f, height * .043f, height * .6f, trunk);
        art.Line(new Vector2(x, ground + height * .36f),
            new Vector2(x - height * .18f, ground + height * .64f), height * .018f, trunk);
        art.Line(new Vector2(x, ground + height * .45f),
            new Vector2(x + height * .19f, ground + height * .67f), height * .016f, trunk);
        art.Circle(new Vector2(x - height * .2f, ground + height * .61f), height * .23f, leaves);
        art.Circle(new Vector2(x + height * .22f, ground + height * .6f), height * .22f, leaves);
        art.Circle(new Vector2(x, ground + height * .73f), height * .28f, leaves);
        art.Circle(new Vector2(x - height * .055f, ground + height * .79f), height * .20f, light);
    }

    private void BuildBackLane()
    {
        var lane = new SchoolSceneDrawing();
        var brick = new Color32(98, 119, 126, 255);
        var sill = new Color32(154, 173, 168, 255);
        // The back facade starts at the chute, behind the retaining wall.
        // Its lower edge meets the school wall instead of overlapping it.
        float facadeLeft = leftChute != null ? leftChute.bounds.min.x : -28f;
        float right = schoolDoor != null ? schoolDoor.position.x + 2.2f : 6f;
        lane.ShadedRect(facadeLeft, -17.4f, right, 10, new Color32(73, 91, 98, 255), brick);
        foreach (float y in new[] { 5.6f, -2.5f, -10.5f })
        {
            const float x = -23.3f;
            lane.Rect(x, y, 1.6f, 1.85f, Ink);
            lane.Rect(x, y + .05f, 1.35f, 1.58f, sill);
            lane.Rect(x, y + .05f, .07f, 1.58f, Ink);
            lane.Rect(x, y - .92f, 1.9f, .13f, sill);
        }

        Draw(lane, "Back windows on the lane", -9);
    }

    private void BuildLandingCorner()
    {
        if (!leftChute || !landingWall || !landingFloor) return;
        Vector2 half = leftChute.size * .5f;
        Vector2 upper = leftChute.transform.TransformPoint(leftChute.offset + half);
        Vector2 joint = leftChute.transform.TransformPoint(leftChute.offset + new Vector2(half.x, -half.y));
        float face = landingWall.bounds.max.x;
        float ground = landingFloor.bounds.max.y;
        float left = face - 22f;
        float baseY = ground - 10f;
        var masonry = new SchoolSceneDrawing();
        var stone = new Color32(25, 38, 46, 255);
        var jointColor = new Color32(45, 59, 66, 255);
        var cornerStone = new Color32(105, 122, 123, 255);
        // Give the sloping chute and its vertical foot one solid silhouette.
        masonry.Polygon(stone, new Vector2(left, upper.y - 6f), upper, joint, new Vector2(left, joint.y));
        masonry.Rect((left + face) / 2, (joint.y + baseY) / 2, face - left, joint.y - baseY, stone);
        masonry.Line(upper + Vector2.left * .09f, joint + Vector2.left * .09f, .17f, cornerStone);
        masonry.Rect(face - .09f, (joint.y + ground) / 2, .18f, joint.y - ground, cornerStone);
        // Offset the mortar courses from the finer brickwork on the distant school wall.
        int row = 0;
        for (float y = ground + .6f; y < upper.y - 7.3f; y += 1.3f, row++)
        {
            float edge = y < joint.y ? face : Mathf.Lerp(joint.x, upper.x, Mathf.InverseLerp(joint.y, upper.y, y));
            masonry.Rect((edge + left) / 2, y, edge - left, .035f, jointColor);
            for (float x = left + 1.4f + (row % 2) * 1.5f; x < edge - .45f; x += 3f)
                masonry.Rect(x, y + .65f, .035f, 1.3f, jointColor);
        }
        float right = schoolDoor != null ? schoolDoor.position.x + 2.2f : landingFloor.bounds.max.x;
        masonry.ShadedRect(left, baseY, right, ground - .09f,
            new Color32(40, 52, 59, 255), stone);
        Draw(masonry, "Continuous retaining wall and alley foundation", 92);
    }

    private void BuildAlley()
    {
        var a=new SchoolSceneDrawing();
        BuildBackLane();
        BuildLandingCorner();
        float right = schoolDoor != null ? schoolDoor.position.x + 2.2f : 6f;
        float bottom = landingFloor != null ? landingFloor.bounds.max.y - 10f : -31f;
        float width = right + 39f;
        float center = (right - 39f) * .5f;
        a.ShadedRect(-39, bottom, right, -17.4f, new Color32(54,70,78,255), new Color32(73,91,98,255));
        for(float y=-23;y< -18.2f;y+=.85f)
        {
            a.Rect(center,y,width,.035f,new Color32(91,106,110,255));
            for(float x=-38+((int)(y*10)%2==0?0:1);x<right-1f;x+=2)
                a.Rect(x,y+.42f,.035f,.85f,new Color32(91,106,110,255));
        }
        a.Rect(center,-17.4f,width,.2f,Ink);
        a.Rect(-12.7f,-19.25f,18.2f,.45f,new Color32(61,78,86,255));
        Draw(a,"School wall",80);
        a=new SchoolSceneDrawing();
        for(float x=-24;x<-4;x+=1.3f)a.Rect(x,-23.15f,1.2f,.16f,new Color32(137,144,137,255));
        Draw(a,"Alley paving",89);
        if(schoolDoor==null)return;
        Vector2 p=schoolDoor.position;
        a=new SchoolSceneDrawing();
        a.Rect(p.x,p.y+.1f,4.4f,4.2f,Ink);
        a.Rect(p.x,p.y+.1f,3.9f,3.8f,new Color32(110,133,129,255));
        a.Rect(p.x,p.y,1.65f,3.35f,new Color32(255,219,146,255));
        a.Rect(p.x-1.75f,p.y,.24f,4,Wood);a.Rect(p.x+1.75f,p.y,.24f,4,Wood);
        a.Polygon(Warm,new Vector2(p.x-1.85f,p.y+2),new Vector2(p.x,p.y+3),new Vector2(p.x+1.85f,p.y+2));
        Draw(a,"School gate and warm entrance",110);
        Sign(new Vector2(p.x,p.y+2.25f),"ELEMENTARY",3.4f,115);
        a=new SchoolSceneDrawing();
        a.Polygon(new Color(1f,.78f,.35f,.13f),new Vector2(p.x-.8f,p.y-1.5f),new Vector2(p.x+.8f,p.y-1.5f),
            new Vector2(p.x+.3f,p.y-1.65f),new Vector2(p.x-4.5f,p.y-1.65f));
        Draw(a,"Warm light beneath the gate",111);
    }
    internal static void Clock(SchoolSceneDrawing a,Vector2 p,float radius)
    {
        a.Circle(p,radius,Ink);a.Circle(p,radius*.86f,new Color32(250,233,188,255));
        for(int i=0;i<12;i++)
        {
            float t=i*Mathf.PI/6;Vector2 v=new Vector2(Mathf.Sin(t),Mathf.Cos(t));
            a.Line(p+v*radius*.66f,p+v*radius*.77f,.035f,Ink);
        }
        a.Line(p,p+Vector2.up*radius*.57f,.07f,Ink);
        a.Line(p,p+new Vector2(-.4f,-.22f)*radius,.09f,Ink);
    }
    private void Sign(Vector2 position,string caption,float width,int order=-5)
    {
        var a=new SchoolSceneDrawing();a.Rect(position.x,position.y,width,.62f,Ink);
        Draw(a,caption+" sign",order);
        var obj=new GameObject(caption,typeof(TextMeshPro)){hideFlags=HideFlags.HideAndDontSave};
        obj.transform.SetParent(transform,false);obj.transform.position=position;
        var text=obj.GetComponent<TextMeshPro>();text.text=caption;text.fontSize=2.3f;
        text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;
        text.color=Warm;text.rectTransform.sizeDelta=new Vector2(width-.15f,.55f);
        text.renderer.sortingOrder=order+1;drawings.Add(obj);
    }
    private void OnDisable()=>Clear();
    private void OnDestroy()=>Clear();
    private void Clear()
    {
        rain = null;
        smileCompletion = null;
        smileRain = null;
        foreach(var obj in drawings)if(obj!=null){obj.SetActive(false);SchoolSceneDrawing.Release(obj);}
        foreach(var mesh in meshes)SchoolSceneDrawing.Release(mesh);
        drawings.Clear();meshes.Clear();SchoolSceneDrawing.Release(material);material=null;
    }
}
