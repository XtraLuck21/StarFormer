using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileGoalFlag : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private Transform flagVisual;
    [SerializeField] private float raiseSeconds=1.25f;
    private Vector3 flagRest;
    private BoxCollider2D sensor;
    private float age;
    private GameObject victory;
    private PossessableBody winner;
    private bool controllerEnabled, wasSimulated;
    public bool IsRaising{get;private set;}
    public bool IsRaised{get;private set;}
    public bool HasWon{get;private set;}
    private void Awake(){sensor=GetComponent<BoxCollider2D>();sensor.isTrigger=true;sensor.enabled=false;flagRest=flagVisual.localPosition;flagVisual.gameObject.SetActive(false);}
    public void Raise(bool immediate=false){if(IsRaised||IsRaising)return;flagVisual.gameObject.SetActive(true);IsRaising=true;age=immediate?raiseSeconds:0f;ApplyLift();}
    private void Update(){if(!IsRaising||Time.timeScale<=0)return;age+=Time.deltaTime;ApplyLift();}
    private void ApplyLift(){float t=Mathf.Clamp01(age/raiseSeconds);flagVisual.localPosition=flagRest+Vector3.down*Mathf.Lerp(1.4f,0,Mathf.SmoothStep(0,1,t));if(t>=1){IsRaised=true;IsRaising=false;sensor.enabled=true;}}
    private void OnTriggerEnter2D(Collider2D other)=>Touch(other);
    private void OnTriggerStay2D(Collider2D other)=>Touch(other);
    private void Touch(Collider2D other)
    {
        if(!IsRaised||HasWon||Time.timeScale<=0||player.IsDead||player.IsRestarting||other.attachedRigidbody==null||player.CurrentBody==null||other.attachedRigidbody!=player.CurrentBody.Body)return;
        HasWon=true;winner=player.CurrentBody;controllerEnabled=player.enabled;wasSimulated=winner.Body.simulated;
        player.ClearInputForRestartMenu();winner.Body.linearVelocity=Vector2.zero;winner.Body.angularVelocity=0;winner.Body.simulated=false;player.enabled=false;
        victory=new GameObject("Smile Congratulations",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));victory.transform.SetParent(transform,false);
        var canvas=victory.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=75;
        var scaler=victory.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        var panel=new GameObject("Victory Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(victory.transform,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.15f,.32f);rect.anchorMax=new Vector2(.85f,.68f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=panel.GetComponent<Image>();image.color=new Color(.08f,.11f,.16f,.9f);image.raycastTarget=false;
        var label=new GameObject("Victory Message",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(panel.transform,false);
        var text=label.GetComponent<TextMeshProUGUI>();text.text="Congratulations,\nyou are now a super star";text.alignment=TextAlignmentOptions.Center;text.fontSize=60;text.color=new Color(1f,.88f,.4f);text.raycastTarget=false;
        rect=label.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(30,25);rect.offsetMax=new Vector2(-30,-25);
    }
    private void OnDisable(){if(!HasWon)return;if(winner!=null)winner.Body.simulated=wasSimulated;if(player!=null)player.enabled=controllerEnabled;if(victory!=null)Destroy(victory);}
}
