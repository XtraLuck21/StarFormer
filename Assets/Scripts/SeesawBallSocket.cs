using System.Collections.Generic;
using UnityEngine;

/// <summary>A recessed ordinary-ball receiver; touching the rim alone never powers its drive.</summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawBallSocket : MonoBehaviour
{
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private SeesawRoomStage stage;
    [SerializeField] private SeesawGear2D gear;
    [SerializeField] private SeesawTilt2D.Side side;
    [SerializeField] private float mouthWidth = .9f;
    private bool captured;
    private Material material;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<GameObject> drawings = new List<GameObject>();
    private GameObject guide;
    public bool IsFilled => captured || gear != null && gear.IsRunning;
    public float MouthWidth => mouthWidth;
    public SeesawTilt2D.Side Side => side;

    private void OnEnable()
    {
        if (gear == null) return;
        Color paint = gear.GetComponent<SpriteRenderer>().color;
        material = new Material(Shader.Find("Sprites/Default")) {mainTexture=Texture2D.whiteTexture,
            hideFlags=HideFlags.HideAndDontSave};
        var cup = new SchoolSceneDrawing();
        cup.Rect(0f,-mouthWidth*.5f,mouthWidth,mouthWidth,new Color(.08f,.13f,.13f,.8f));
        for(int sign=-1;sign<=1;sign+=2)
        {
            cup.Line(new Vector2(sign*mouthWidth*.5f,.08f),new Vector2(sign*mouthWidth*.5f,-mouthWidth),.09f,paint);
            cup.Line(new Vector2(sign*mouthWidth*.5f,.08f),new Vector2(sign*(mouthWidth*.5f+.2f),.18f),.1f,paint);
        }
        cup.Line(new Vector2(-mouthWidth*.5f,-mouthWidth),new Vector2(mouthWidth*.5f,-mouthWidth),.1f,paint);
        Draw(cup,"Recessed ball socket",3);
        var hint = new SchoolSceneDrawing();
        hint.Circle(new Vector2(0f,1.15f),.16f,new Color32(210,73,64,255));
        hint.Line(new Vector2(0f,.85f),new Vector2(0f,.42f),.075f,paint);
        hint.Polygon(paint,new Vector2(-.22f,.5f),new Vector2(0f,.25f),new Vector2(.22f,.5f));
        guide=Draw(hint,"Blow the red ball into the hole",6);
    }
    private GameObject Draw(SchoolSceneDrawing art,string name,int order)
    {
        GameObject item=art.Build(name,transform,material,order,out Mesh mesh);
        drawings.Add(item);meshes.Add(mesh);return item;
    }
    private void Update()
    {
        // A saved blue/green stage restores the visible balls as well as its latched drives.
        if(Application.isPlaying && !captured && gear!=null && gear.IsRunning)
        {
            supplies.RestoreCapturedBall(transform,mouthWidth);
            captured=true;
        }
        if (guide != null) guide.SetActive(!Application.isPlaying || !IsFilled && !stage.IsUnlocked);
    }
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger=true;
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);
    private void Check(Collider2D other)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || supplies == null || !supplies.CanRunEffects ||
            other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out WindReleasedBall ball) ||
            !ball.IsOwnedBy(supplies) || !ball.HasBeenReleased || ball.IsCaptured) return;
        if (IsFilled || stage.IsUnlocked) return;
        if (!seesaw.TryTilt(side,ball)) return;
        captured=true;
        ball.CaptureInSocket(transform,mouthWidth);
    }
    private void OnDisable()
    {
        foreach(var item in drawings) SchoolSceneDrawing.Release(item);
        foreach(var mesh in meshes) SchoolSceneDrawing.Release(mesh);
        drawings.Clear();meshes.Clear();SchoolSceneDrawing.Release(material);
    }
}
