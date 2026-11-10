using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileGoalFlag : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private Transform flagVisual;
    [SerializeField] private float raiseSeconds=1.25f;
    private Vector3 flagRest;
    private BoxCollider2D sensor;
    private float age;
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
        if (!IsRaised || HasWon || Time.timeScale <= 0f || player.IsDead || player.IsRestarting)
            return;

        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || player.CurrentBody == null || body != player.CurrentBody.Body)
            return;

        HasWon = true;
        player.ClearInputForRestartMenu();
        AnalyticsManager.Instance?.TrackLevelComplete(player.CurrentBody);
        LevelTransitionUI.ShowFinalResult("Navigation", player);
    }
}
