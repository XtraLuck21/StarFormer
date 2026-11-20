using TMPro;
using UnityEngine;

// Only the IntroBox scene opts into this carrying lesson. Ownership still uses
// the shared contact/separation rules; this component never transfers on touch.
[DefaultExecutionOrder(50)]
public sealed class IntroBoxCarryLesson : MonoBehaviour
{
    public PlayerPossessionController player;
    public PossessableBody boxA, carrier, cargo;
    public Collider2D receivingPlatform;
    public TMP_Text hint;
    public Transform hintPanel;
    public Camera lessonCamera;
    public IntroFlagGate flag;
    public BubbleMovementHint moveKeys, jumpKey;
    public BoxGrowHint dockShrink, reconnectGrow, passageShrink;
    public float transportSpeed = 2.5f;
    public GameObject welcomeSign;
    public GameObject ledgeGuide;
    public GameObject ledgeRoute;
    public float RevealElapsed { get; private set; }
    bool revealStarted;

    public bool Docked { get; private set; }
    public bool Separated { get; private set; }
    public int RecoveryCount { get; private set; }
    public string LessonStage { get; private set; }

    Vector2 carrierSpawn, cargoSpawn;
    float cargoOffset, recoveryUntil, invalidSeconds, separatedSeconds;
    bool cargoAcquired, cameraPlaced;
    Vector3 cameraVelocity;

    void Awake()
    {
        carrierSpawn = carrier.transform.position;
        cargoSpawn = cargo.transform.position;
        cargoOffset = cargoSpawn.x - carrierSpawn.x;
    }

    void Start()
    {
        // IntroBubble's return path has already restored C at the open exit.
        if (player.CurrentBody == cargo && flag != null && flag.IsOpen)
            Docked = Separated = cargoAcquired = revealStarted = true;
    }

    void OnDisable()
    {
        SetGuide(dockShrink, false);
        SetGuide(reconnectGrow, false);
        SetGuide(passageShrink, false);
        if (hintPanel != null) hintPanel.gameObject.SetActive(false);
        if (ledgeGuide != null) ledgeGuide.SetActive(false);
        // This lesson never owns time scale or disables the player's controls.
    }

    void FixedUpdate()
    {
        if (player == null || player.IsDead || player.IsRestarting || Time.timeScale <= 0) return;
        Bounds b = carrier.BodyCollider.bounds, c = cargo.BodyCollider.bounds;
        Bounds platform = receivingPlatform.bounds;
        bool aboveLedge = c.max.x > platform.min.x + .08f && c.min.y >= platform.max.y - .08f;
        bool onLedge = aboveLedge && Mathf.Abs(c.min.y - platform.max.y) < .065f && cargo.IsGrounded();
        if (onLedge) Docked = true;

        // Moving-platform support: match lateral velocity only while C really
        // rests on B. Never parent, teleport, or attach C after separation.
        bool supported = c.min.x < b.max.x - .05f && c.max.x > b.min.x + .05f &&
            Mathf.Abs(c.min.y - b.max.y) < .065f && cargo.IsGrounded();
        if (player.CurrentBody == carrier && !Docked && supported)
        {
            Vector2 velocity = carrier.Body.linearVelocity;
            velocity.x = Mathf.Clamp(velocity.x, -transportSpeed, transportSpeed);
            carrier.Body.linearVelocity = velocity;
            cargo.Body.linearVelocity = new Vector2(velocity.x, cargo.Body.linearVelocity.y);
        }

        ColliderDistance2D gap = Physics2D.Distance(carrier.BodyCollider, cargo.BodyCollider);
        bool apart = Docked && gap.isValid && gap.distance > .08f;
        separatedSeconds = apart ? separatedSeconds + Time.fixedDeltaTime : 0;
        if (separatedSeconds >= .10f) Separated = true;
        if (player.CurrentBody == cargo && Docked && Separated) cargoAcquired = true;

        // Only a still-carried box must stay aligned with its carrier. A docked
        // passenger is supposed to stay behind when the carrier walks away.
        // A premature shrink must not strand the player in C on
        // the lower floor. Retry just the carrying lesson, not the opening jump.
        bool failed = (player.CurrentBody == cargo && !cargoAcquired) ||
            (!cargoAcquired && player.CurrentBody == carrier &&
             (c.min.y < -1.25f || (!Docked && Mathf.Abs(c.center.x - b.center.x - cargoOffset) > 1.1f))) ||
            (cargoAcquired && player.CurrentBody == cargo && c.min.y < platform.max.y - .4f);
        invalidSeconds = failed ? invalidSeconds + Time.fixedDeltaTime : 0;
        if (invalidSeconds > .35f) ResetCarry();
    }

    void ResetCarry()
    {
        carrier.GetComponent<BoxResizeAbility>().RestoreSceneEntrySize(1);
        cargo.GetComponent<BoxResizeAbility>().RestoreSceneEntrySize(1);
        Place(carrier, carrierSpawn); Place(cargo, cargoSpawn);
        Physics2D.SyncTransforms();
        player.TryRestoreLessonBody(carrier);
        Docked = Separated = cargoAcquired = false;
        invalidSeconds = separatedSeconds = 0;
        recoveryUntil = Time.time + 2;
        RecoveryCount++;
    }

    static void Place(PossessableBody body, Vector2 position)
    {
        body.transform.position = new Vector3(position.x, position.y, body.transform.position.z);
        body.Body.position = position;
        body.Body.linearVelocity = Vector2.zero;
        body.Body.angularVelocity = 0;
    }

    void LateUpdate()
    {
        if (player == null || player.CurrentBody == null || hint == null) return;
        var body = player.CurrentBody;
        bool show = !player.IsDead && !player.IsRestarting && Time.timeScale > 0;
        hintPanel.gameObject.SetActive(show);
        if (ledgeGuide != null) ledgeGuide.SetActive(show && body == carrier && !cargoAcquired);
        if (ledgeRoute != null) ledgeRoute.SetActive(!Docked && !Separated);
        if (show && body == carrier && !revealStarted)
        {
            revealStarted = true;
            RevealElapsed = 0;
        }
        if (show && revealStarted) RevealElapsed += Time.deltaTime;
        SetGuide(moveKeys, show && body.Form == BodyForm.Star && body.Body.position.x < -14.2f);
        SetGuide(jumpKey, show && body.Form == BodyForm.Star && body.Body.position.x >= -14.2f);
        if (!show)
        {
            SetGuide(dockShrink, false);
            SetGuide(reconnectGrow, false);
            SetGuide(passageShrink, false);
            return;
        }
        bool showDock = false, showReconnect = false, showPassage = false;
        Vector2 anchor = new Vector2(body.Body.position.x, body.BodyCollider.bounds.max.y + .9f);
        string text = "";
        if (Time.time < recoveryUntil)
        {
            LessonStage = "Recovery";
            text = "Carry it to the ledge.";
        }
        else if (body.Form == BodyForm.Star)
        {
            LessonStage = body.Body.position.x < -14.2f ? "Move" :
                body.Body.position.x < -10.8f ? "Jump" : "PossessA";
        }
        else if (body == boxA)
        {
            LessonStage = "PossessB";
            text = "Touch the next box.";
            anchor = new Vector2(carrier.Body.position.x, cargo.BodyCollider.bounds.max.y + .9f);
        }
        else if (body == carrier)
        {
            Bounds c = cargo.BodyCollider.bounds;
            bool atDock = carrier.BodyCollider.bounds.max.x >= receivingPlatform.bounds.min.x - .10f &&
                c.max.x > receivingPlatform.bounds.min.x + .08f;
            // Dock text and resize diagrams belong to the teaching station,
            // never to the moving carrier. Walking away leaves them in place.
            anchor = new Vector2(receivingPlatform.bounds.min.x - .2f, 1.1f);
            bool nearDock = Mathf.Abs(body.Body.position.x - (receivingPlatform.bounds.min.x - 1)) <= 2.1f;
            if (Separated)
            {
                LessonStage = "Reconnect";
                bool againstWall = body.Body.position.x > receivingPlatform.bounds.min.x - 1f + .08f;
                text = nearDock ? (againstWall ? "Move a little left." :
                    "Touch it again to switch.\nOnly a new touch counts.") : "";
                showReconnect = !againstWall && nearDock;
            }
            else if (RevealElapsed < .65f)
            {
                LessonStage = "Reveal";
            }
            else if (RevealElapsed < 3.8f)
            {
                LessonStage = "Carry";
                text = "Can you figure out how to become\nthe box above you?";
                anchor = new Vector2(.1f, 1.8f);
            }
            else if (atDock || Docked)
            {
                LessonStage = "Separate";
                text = nearDock ? "Leave it here.\nMove apart, then touch again." : "";
                showDock = nearDock;
            }
            else
            {
                LessonStage = "CarryToLedge";
                text = "A spot for your passenger!";
                anchor = new Vector2(4.3f, 1.55f);
            }
        }

        else
        {
            LessonStage = "Exit";
            showPassage = body == cargo && body.Body.position.x < 8.5f;
        }
        SetGuide(dockShrink, showDock);
        SetGuide(reconnectGrow, showReconnect);
        SetGuide(passageShrink, showPassage);
        hint.text = text;
        // Brief local prompts unfold with progress; control instructions stay visual.
        anchor.y = Mathf.Min(anchor.y, 3.1f);
        hintPanel.position = new Vector3(anchor.x, anchor.y, -1);
        if (lessonCamera != null)
        {
            float halfWidth = lessonCamera.orthographicSize * lessonCamera.aspect;
            // Ease the view to the right before posing the question. Keep the
            // controlled box on screen and leave movement/physics uninterrupted.
            float look = body == carrier && !Separated ?
                Mathf.SmoothStep(0, 1, RevealElapsed / 1.2f) : 0;
            float lead = Mathf.Min(1.3f + 2.8f * look, Mathf.Max(.3f, halfWidth - 1.8f));
            Vector3 desired = new Vector3(Mathf.Clamp(body.Body.position.x + lead,
                -20f + halfWidth, 16f - halfWidth), .5f, -10);
            if (halfWidth >= 18) desired.x = -2;
            lessonCamera.transform.position = cameraPlaced ?
                Vector3.SmoothDamp(lessonCamera.transform.position, desired, ref cameraVelocity,
                    body == carrier ? .6f : .2f) : desired;
            cameraPlaced = true;
            // Do not clamp world-space instructions to the screen: they should
            // leave view naturally when the player leaves their teaching area.
        }
    }
    static void SetGuide(Behaviour guide, bool visible)
    {
        if (guide != null && guide.gameObject.activeSelf != visible)
            guide.gameObject.SetActive(visible);
    }

}
