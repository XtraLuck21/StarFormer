using TMPro;
using UnityEngine;

/// <summary>The Spider's yellow switch directly powers Yellow and opens the maintenance passage.</summary>
public sealed class SeesawOverloadSequence : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawRoomStage stage;
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private SeesawGear2D yellowGear;
    [SerializeField] private SeesawMachineRoom machine;
    [SerializeField] private BoxCollider2D entrance;
    [SerializeField] private BoxCollider2D exit;
    [SerializeField,Min(0f)] private float entranceHintDelay=5f;
    [SerializeField] private Vector3 entranceHintOffset=new Vector3(0f,1.5f,0f);
    [SerializeField,Min(.1f)] private float entranceHintFontSize=16f;
    private float passageProgress,hintAge;
    private TextMeshPro entranceHint;
    public bool HasStarted { get; private set; }
    public bool IsPassageOpen { get; private set; }

    // Retain compatibility with the room's previously authored UnityEvent.
    public void BeginYellowGust() => ActivateYellowSwitch();

    public void ActivateYellowSwitch()
    {
        if(HasStarted || !stage.IsUnlocked || stage.IsRevealing || player.IsDead || player.IsRestarting || Time.timeScale<=0f)return;
        HasStarted=true;
        yellowGear.StartTurning();
        seesaw.HoldNeutral();
        BuildEntranceHint();
    }
    private void Update()
    {
        if(!HasStarted || player.IsDead || player.IsRestarting || Time.timeScale<=0f)return;
        passageProgress=Mathf.MoveTowards(passageProgress,1f,Time.deltaTime/.6f);
        stage.RevealPurpleButton(passageProgress);
        machine.OpenMaintenancePassage(Mathf.SmoothStep(0f,1f,passageProgress));
        if(passageProgress>=1f && !IsPassageOpen)
        {
            entrance.enabled=exit.enabled=false;
            IsPassageOpen=true;
        }
        if(entranceHint==null)return;
        var current=player.CurrentBody;
        bool approaching=IsPassageOpen && current!=null && current.Form==BodyForm.Spider && current.IsControlled &&
            current.transform.position.y>entrance.transform.position.y+.5f;
        if(approaching)hintAge+=Time.deltaTime;
        entranceHint.enabled=approaching && hintAge>=entranceHintDelay;
        entranceHint.transform.position=entrance.transform.position+entranceHintOffset+Vector3.up*(Mathf.Sin(hintAge*Mathf.PI)*.15f);
    }
    private void BuildEntranceHint()
    {
        var go=new GameObject("Spider Maintenance Entrance Reminder");
        go.transform.SetParent(transform,false);
        entranceHint=go.AddComponent<TextMeshPro>();
        entranceHint.text="!";entranceHint.fontStyle=FontStyles.Bold;entranceHint.color=Color.yellow;
        entranceHint.fontSize=entranceHintFontSize;entranceHint.alignment=TextAlignmentOptions.Center;
        entranceHint.rectTransform.sizeDelta=new Vector2(3f,3f);entranceHint.raycastTarget=false;
        go.GetComponent<MeshRenderer>().sortingOrder=30;
        entranceHint.enabled=false;
    }
    private void OnDisable()
    {
        if(entranceHint!=null)entranceHint.enabled=false;
    }
}
