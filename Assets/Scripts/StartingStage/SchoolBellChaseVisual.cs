using System.Collections.Generic;
using UnityEngine;

// Re-skins this scene's existing pursuer; its authored colliders and chase events still own gameplay.
[ExecuteAlways]
[RequireComponent(typeof(SpikeWallEnemyChase))]
public sealed class SchoolBellChaseVisual : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    private readonly List<Mesh> meshes=new List<Mesh>();
    private readonly Dictionary<SpriteRenderer,bool> originalVisibility=new Dictionary<SpriteRenderer,bool>();
    private GameObject art;
    private Transform clock;
    private Transform angerMark;
    private Material material;
    private SpikeWallEnemyChase chase;
    private spikeWallSpawn spawn;
    private bool rebuild;
    private float age;
    private void OnEnable()=>rebuild=true;
    private void OnValidate()=>rebuild=true;
    private void Update()
    {
        if(rebuild){rebuild=false;Build();}
        if(art==null)return;
        bool playing=Application.IsPlaying(gameObject);
        bool visible=!playing || chase.IsChasing || (!chase.HasStarted && spawn != null && spawn.HasBegunEmergence);
        art.SetActive(visible);
        if (!playing || !visible || Time.timeScale <= 0f || player == null || player.IsDead || player.IsRestarting) return;
        age += Time.deltaTime;
        clock.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 9f) * 3f);
        angerMark.localScale = Vector3.one * (1f + .12f * Mathf.Sin(age * 12f));
    }
    private void Build()
    {
        Clear();age=0;chase=GetComponent<SpikeWallEnemyChase>();spawn=GetComponent<spikeWallSpawn>();
        Bounds bounds=new Bounds(transform.position,Vector3.zero);bool found=false;
        foreach(var c in GetComponentsInChildren<Collider2D>())
            if(c.enabled){if(!found){bounds=c.bounds;found=true;}else bounds.Encapsulate(c.bounds);}
        if(!found)return;
        foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())
        {originalVisibility[renderer]=renderer.enabled;renderer.enabled=false;}
        material=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
        material.mainTexture=Texture2D.whiteTexture;
        art=new GameObject("School alarm"){hideFlags=HideFlags.HideAndDontSave};
        art.transform.SetParent(transform,false);
        float front=bounds.max.x-transform.position.x;
        float middle=bounds.center.y-transform.position.y;
        var a=new SchoolSceneDrawing();
        SchoolMorningScenery.Clock(a,Vector2.zero,1.2f);
        Color ink=new Color32(39,59,65,255),gold=new Color32(238,172,63,255);
        a.Circle(new Vector2(-.9f,1.05f),.44f,ink);a.Circle(new Vector2(.9f,1.05f),.44f,ink);
        a.Circle(new Vector2(-.9f,1.09f),.33f,gold);a.Circle(new Vector2(.9f,1.09f),.33f,gold);
        a.Line(new Vector2(-.7f,-.85f),new Vector2(-1,-1.3f),.18f,ink);
        a.Line(new Vector2(.7f,-.85f),new Vector2(1,-1.3f),.18f,ink);
        clock=BuildMesh(a,"School bell clock",art.transform,24).transform;
        clock.localPosition=new Vector3(front-1.34f,middle,0);
        var face = new SchoolSceneDrawing();
        face.Circle(Vector2.zero, .83f, new Color32(249, 203, 161, 255));
        face.Line(new Vector2(-.60f,.43f),new Vector2(-.16f,.25f),.12f,ink);
        face.Line(new Vector2(.15f,.25f),new Vector2(.62f,.43f),.12f,ink);
        face.Circle(new Vector2(-.30f,.10f),.085f,ink);
        face.Circle(new Vector2(.36f,.10f),.085f,ink);
        face.Arc(new Vector2(.04f,-.55f),.28f,25f,155f,.085f,ink);
        BuildMesh(face,"Angry clock face",clock,25);
        var anger = new SchoolSceneDrawing();
        Color red = new Color32(196, 77, 55, 255);
        anger.Line(new Vector2(-.22f,.25f),new Vector2(-.22f,.06f),.075f,red);
        anger.Line(new Vector2(-.22f,.06f),new Vector2(-.05f,.06f),.075f,red);
        anger.Line(new Vector2(.05f,.06f),new Vector2(.22f,.06f),.075f,red);
        anger.Line(new Vector2(.22f,.06f),new Vector2(.22f,.25f),.075f,red);
        anger.Line(new Vector2(-.22f,-.25f),new Vector2(-.22f,-.06f),.075f,red);
        anger.Line(new Vector2(-.22f,-.06f),new Vector2(-.05f,-.06f),.075f,red);
        anger.Line(new Vector2(.05f,-.06f),new Vector2(.22f,-.06f),.075f,red);
        anger.Line(new Vector2(.22f,-.06f),new Vector2(.22f,-.25f),.075f,red);
        angerMark=BuildMesh(anger,"Pulsing clock anger mark",clock,26).transform;
        angerMark.localPosition=new Vector3(1.35f,.55f,0);
    }
    private GameObject BuildMesh(SchoolSceneDrawing a,string name,Transform parent,int order)
    { var obj=a.Build(name,parent,material,order,out var mesh);meshes.Add(mesh);return obj; }
    private void OnDisable()=>Clear();
    private void OnDestroy()=>Clear();
    private void Clear()
    {
        foreach(var pair in originalVisibility)if(pair.Key!=null)pair.Key.enabled=pair.Value;
        originalVisibility.Clear();
        if(art!=null){art.SetActive(false);SchoolSceneDrawing.Release(art);}
        foreach(var mesh in meshes)SchoolSceneDrawing.Release(mesh);meshes.Clear();
        SchoolSceneDrawing.Release(material);
        art=null;material=null;
    }
}
