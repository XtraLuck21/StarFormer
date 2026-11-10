using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileBoxPressureButton : MonoBehaviour
{
    [SerializeField] private PossessableBody targetBox;
    [SerializeField] private SmileFaceFinalRoute route;
    [SerializeField] private Transform cap;
    [SerializeField] private SpriteRenderer visual;
    private BoxCollider2D sensor;
    private Vector3 capRest;
    private float holdTime, press;
    public bool IsPressed { get; private set; }
    private void Awake() { sensor=GetComponent<BoxCollider2D>();sensor.isTrigger=true;capRest=cap.localPosition; }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsPressed || Time.timeScale<=0 || targetBox==null || other.attachedRigidbody!=targetBox.Body ||
            !targetBox.Body.simulated || targetBox.Body.bodyType!=RigidbodyType2D.Dynamic || !targetBox.IsGrounded() ||
            targetBox.BodyCollider.bounds.min.y<sensor.bounds.min.y-.1f) return;
        holdTime+=Time.fixedDeltaTime;
        if(holdTime<.16f)return;
        IsPressed=true;
        route.PressFinalButton();
    }
    private void OnTriggerExit2D(Collider2D other) { if(!IsPressed&&targetBox!=null&&other.attachedRigidbody==targetBox.Body)holdTime=0; }
    private void Update()
    {
        press=Mathf.MoveTowards(press,IsPressed?1f:0f,4f*Time.deltaTime);
        cap.localPosition=capRest+Vector3.down*(.18f*press);
        visual.color=Color.Lerp(new Color(1f,.78f,.25f),new Color(.35f,1f,.65f),press);
    }
    public void RestorePressed(){IsPressed=true;press=1f;}
}
