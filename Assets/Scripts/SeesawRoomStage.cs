using UnityEngine;

/// <summary>Fixed room view, player-only gate and the blue/green completion reveal.</summary>
[DefaultExecutionOrder(-50)]
public sealed class SeesawRoomStage : MonoBehaviour
{
    [SerializeField] private BoxCollider2D gate;
    [SerializeField] private GameObject gateVisual;
    [SerializeField] private BoxCollider2D leftGate;
    [SerializeField] private GameObject leftGateVisual;
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
        if (gate.enabled) Physics2D.IgnoreCollision(gate, ball);
        if (leftGate.enabled) Physics2D.IgnoreCollision(leftGate, ball);
    }

    private void Update()
    {
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

    private void OnDisable() => RestoreControls();

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
