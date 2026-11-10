using UnityEngine;
using TMPro;

/// <summary>Fixed room view, player-only gate and the blue/green completion reveal.</summary>
[DefaultExecutionOrder(-50)]
public sealed class SeesawRoomStage : MonoBehaviour
{
    [SerializeField] private BoxCollider2D gate;
    [SerializeField] private GameObject gateVisual;
    [SerializeField] private BoxCollider2D leftGate;
    [SerializeField] private GameObject leftGateVisual;
    [SerializeField] private SeesawGearBarrier gearBarrier;
    [SerializeField] private SeesawGateShatter rightGateShatter;
    [SerializeField] private SeesawGateShatter leftGateShatter;
    [SerializeField] private SeesawGear2D blueGear;
    [SerializeField] private SeesawGear2D greenGear;
    [SerializeField] private SeesawColorContact blueContact;
    [SerializeField] private SeesawColorContact greenContact;
    [SerializeField] private SeesawCheckpoint checkpoint;
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private Transform yellowButton;
    [SerializeField] private Transform purpleButton;
    [SerializeField] private SeesawCliffBridge rightPlatform;
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private Rect fullView = new Rect(10.3f, -24.8f, 36.7f, 12.3f);
    [SerializeField, Min(.1f)] private float revealDuration = 2f;
    [Header("Blue and green gear hints")]
    [SerializeField, TextArea] private string blueGearHint = "As Fan, blow the ball RIGHT.\nLift the blue key into its lock.";
    [SerializeField, TextArea] private string greenGearHint = "As Fan, blow the ball LEFT.\nLower the green key into its lock.";
    private TextMeshPro blueHint, greenHint;

    public bool IsUnlocked { get; private set; }
    public float RevealProgress { get; private set; }
    public bool IsRevealing { get; private set; }
    private Vector3 yellowRest, purpleRest;
    private Collider2D yellowSensor, purpleSensor;
    private SpriteRenderer yellowRenderer, purpleRenderer;
    private int yellowOrder, purpleOrder;
    private PossessableBody frozenBody;
    private RigidbodyType2D previousBodyType;
    private RigidbodyConstraints2D previousConstraints;
    private FanBlower frozenBlower;
    private bool controllerWasEnabled, blowerWasEnabled, controlsLocked;
    private bool gateHintShown;

    public bool TryClaimGateHint()
    {
        if (gateHintShown) return false;
        gateHintShown = true;
        return true;
    }

    private void Awake()
    {
        yellowRest = yellowButton.position;
        purpleRest = purpleButton.position;
        yellowSensor = yellowButton.GetComponent<Collider2D>();
        purpleSensor = purpleButton.GetComponent<Collider2D>();
        yellowRenderer = yellowButton.GetComponent<SpriteRenderer>();
        purpleRenderer = purpleButton.GetComponent<SpriteRenderer>();
        yellowOrder = yellowRenderer.sortingOrder;
        purpleOrder = purpleRenderer.sortingOrder;
        yellowSensor.enabled = purpleSensor.enabled = false;
        yellowButton.gameObject.SetActive(false);
        purpleButton.gameObject.SetActive(false);
        rightPlatform.SetRevealProgress(0f);
        ConfigureGate(gate);
        ConfigureGate(leftGate);
        gate.gameObject.AddComponent<SeesawGateFeedback>().Initialize(this, player, gateVisual.GetComponent<SpriteRenderer>());
        leftGate.gameObject.AddComponent<SeesawGateFeedback>().Initialize(this, player, leftGateVisual.GetComponent<SpriteRenderer>());
        blueHint = BuildGearHint("Blue Gear Hint", blueGearHint, new Color(.45f, .78f, 1f));
        greenHint = BuildGearHint("Green Gear Hint", greenGearHint, new Color(.55f, 1f, .65f));
    }

    private TextMeshPro BuildGearHint(string name, string message, Color color)
    {
        var label = new GameObject(name);
        label.transform.SetParent(transform, false);
        label.transform.position = new Vector3(fullView.center.x, fullView.yMax - 1.25f, 0f);
        var text = label.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = 4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineColor = new Color32(15, 20, 30, 255);
        text.outlineWidth = .18f;
        text.rectTransform.sizeDelta = new Vector2(20f, 2f);
        text.raycastTarget = false;
        text.GetComponent<MeshRenderer>().sortingOrder = 30;
        text.enabled = false;
        return text;
    }

    private void UpdateGearHints()
    {
        bool active = supplies.IsRoomActive && !IsUnlocked && !player.IsDead && !player.IsRestarting &&
            player.CurrentBody != null && supplies.IsInsideRoom(player.CurrentBody.transform.position);
        blueHint.enabled = active && !blueGear.IsRunning;
        // Show one action at a time; either order of actual gear completion is valid.
        greenHint.enabled = active && blueGear.IsRunning && !greenGear.IsRunning;
    }

    private void ConfigureGate(BoxCollider2D barrier)
    {
        barrier.isTrigger = false;
        // Do not change shared layers: the gate only collides with possessable bodies.
        foreach (Collider2D other in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (other == barrier || other.gameObject.scene != gameObject.scene) continue;
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent(out PossessableBody _))
                Physics2D.IgnoreCollision(barrier, other);
        }
    }

    public void AllowBall(Collider2D ball)
    {
        if (ball == null) return;
        if (gearBarrier != null) gearBarrier.AllowBall(ball);
        if (gate.enabled) Physics2D.IgnoreCollision(gate, ball);
        if (leftGate.enabled) Physics2D.IgnoreCollision(leftGate, ball);
    }

    private void Update()
    {
        UpdateGearHints();
        if (!IsUnlocked && blueGear.IsRunning && greenGear.IsRunning)
        {
            // Keep the death/restart controller running if the last button and a fall
            // happened in the same physics step. The checkpoint restores the final state.
            if (player.IsDead || player.IsRestarting)
            {
                IsUnlocked = true;
                supplies.StopFanSupply();
                seesaw.HoldNeutral();
                checkpoint.SaveBlueGreenStage();
                return;
            }
            if (Time.timeScale > 0f) BeginReveal();
        }
        if (!IsRevealing || Time.timeScale <= 0f) return;
        RevealProgress = Mathf.Min(1f, RevealProgress + Time.deltaTime / revealDuration);
        ApplyReveal(Mathf.SmoothStep(0f, 1f, RevealProgress));
        if (RevealProgress >= 1f) FinishReveal();
    }

    private void BeginReveal()
    {
        IsUnlocked = true;
        IsRevealing = true;
        supplies.StopFanSupply();
        seesaw.HoldNeutral();
        checkpoint.SaveBlueGreenStage();
        LockControls();
        // The wall/floor sprites occlude the buried parts while the buttons emerge.
        yellowRenderer.sortingOrder = purpleRenderer.sortingOrder = -1;
        ApplyReveal(0f);
    }

    private void ApplyReveal(float t)
    {
        yellowButton.position = yellowRest + Vector3.up * (.5f * (1f - t));
        purpleButton.position = purpleRest - Vector3.up * (.95f * (1f - t));
        yellowButton.gameObject.SetActive(true);
        purpleButton.gameObject.SetActive(true);
        yellowSensor.enabled = purpleSensor.enabled = false;
        rightPlatform.SetRevealProgress(t);
        rightGateShatter.SetProgress(t);
        leftGateShatter.SetProgress(t);
    }

    private void FinishReveal()
    {
        IsRevealing = false;
        RevealProgress = 1f;
        yellowButton.position = yellowRest;
        purpleButton.position = purpleRest;
        yellowRenderer.sortingOrder = yellowOrder;
        purpleRenderer.sortingOrder = purpleOrder;
        yellowSensor.enabled = purpleSensor.enabled = true;
        gate.enabled = false;
        gateVisual.SetActive(false);
        leftGate.enabled = false;
        leftGateVisual.SetActive(false);
        RestoreControls();
    }

    private void LockControls()
    {
        controlsLocked = true;
        controllerWasEnabled = player.enabled;
        player.ClearInputForRestartMenu();
        frozenBody = player.CurrentBody;
        if (frozenBody != null)
        {
            Rigidbody2D body = frozenBody.Body;
            previousBodyType = body.bodyType;
            previousConstraints = body.constraints;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            if (frozenBody.TryGetComponent(out frozenBlower))
            {
                blowerWasEnabled = frozenBlower.enabled;
                frozenBlower.StopBlowing();
                frozenBlower.enabled = false;
            }
        }
        player.enabled = false;
    }

    private void RestoreControls()
    {
        if (!controlsLocked) return;
        controlsLocked = false;
        if (frozenBody != null)
        {
            frozenBody.Body.bodyType = previousBodyType;
            frozenBody.Body.constraints = previousConstraints;
            frozenBody.Body.linearVelocity = Vector2.zero;
            frozenBody.Body.angularVelocity = 0f;
            frozenBody.Body.WakeUp();
        }
        if (frozenBlower != null) frozenBlower.enabled = blowerWasEnabled;
        if (player != null)
        {
            player.ClearInputForRestartMenu();
            player.enabled = controllerWasEnabled;
        }
        frozenBody = null;
        frozenBlower = null;
    }

    private void OnDisable()
    {
        RestoreControls();
        if (blueHint != null) blueHint.enabled = false;
        if (greenHint != null) greenHint.enabled = false;
    }

    public void RestoreUnlocked()
    {
        blueContact.RestorePressed();
        greenContact.RestorePressed();
        IsUnlocked = true;
        supplies.StopFanSupply();
        seesaw.HoldNeutral();
        ApplyReveal(1f);
        FinishReveal();
    }

    public void GetCameraFrame(Camera camera, float z, out Vector3 position, out float size)
    {
        float aspect = Mathf.Max(.1f, camera.aspect);
        position = new Vector3(fullView.center.x, fullView.center.y, z);
        size = Mathf.Max(fullView.height * .5f, fullView.width / (2f * aspect));
    }

    public bool ContainsPosition(Vector2 position)
        => position.x >= fullView.xMin - 1f && position.x <= fullView.xMax + 1f &&
           position.y >= fullView.yMin - 6f && position.y <= fullView.yMax + 1f;
}
