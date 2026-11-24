using UnityEngine;

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
    [SerializeField] private SpriteRenderer yellowGearVisual;
    [SerializeField] private SpriteRenderer purpleGearVisual;
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
    private Vector3 yellowRest, yellowFullScale, purpleFullScale, spiderRest;
    private Collider2D yellowSensor, purpleSensor;
    private SpriteRenderer yellowRenderer, purpleRenderer;
    private PossessableBody spider;
    private Rigidbody2D spiderBody;
    private bool spiderWasSimulated;
    private bool emergenceStarted;
    private ParticleSystem emergenceDust;
    private float yellowHeight, ceilingEdge;
    private PossessableBody frozenBody;
    private RigidbodyType2D previousBodyType;
    private RigidbodyConstraints2D previousConstraints;
    private FanBlower frozenBlower;
    private bool controllerWasEnabled, blowerWasEnabled, controlsLocked;
    private bool gateHintShown;

    internal void BuildPlaygroundDetails(SchoolMorningScenery scenery, float floorY)
    {
        Color metal = new Color32(67, 82, 84, 255);
        Color brass = new Color32(222, 171, 105, 255);
        var hardware = new SchoolSceneDrawing();
        float pivotX = seesaw.transform.position.x;
        float baseWidth = seesaw.Fulcrum.transform.lossyScale.x + .36f;
        hardware.Rect(pivotX, floorY + .10f, baseWidth, .20f, metal);
        hardware.Rect(pivotX, floorY + .18f, baseWidth, .048f, brass);
        scenery.Draw(hardware, "Seesaw floor plinth", 1);

        var hub = new SchoolSceneDrawing();
        hub.Circle(Vector2.zero, .27f, metal);
        hub.Circle(Vector2.zero, .192f, brass);
        hub.Circle(Vector2.zero, .075f, metal);
        hub.Rect(0f, 0f, .12f, .036f, brass);
        scenery.Draw(hub, "Seesaw axle", 5, seesaw.transform);

    }

    public bool TryClaimGateHint()
    {
        if (gateHintShown || !supplies.FanDemonstrationIsOver) return false;
        gateHintShown = true;
        return true;
    }

    private void Awake()
    {
        yellowRest = yellowButton.position;
        yellowFullScale = yellowButton.localScale;
        purpleFullScale = purpleButton.localScale;
        yellowSensor = yellowButton.GetComponent<Collider2D>();
        purpleSensor = purpleButton.GetComponent<Collider2D>();
        yellowRenderer = yellowButton.GetComponent<SpriteRenderer>();
        purpleRenderer = purpleButton.GetComponent<SpriteRenderer>();
        var ceilingButton = yellowButton.GetComponent<SeesawSpiderCeilingButton>();
        yellowHeight = yellowRenderer.bounds.size.y;
        ceilingEdge = ceilingButton.Ceiling.bounds.min.y;
        yellowRest.y = ceilingEdge - yellowHeight * .5f;
        spider = ceilingButton.TargetSpider;
        spiderRest = spider.transform.position;
        spiderBody = spider.GetComponent<Rigidbody2D>();
        spiderWasSimulated = spiderBody.simulated;
        spiderBody.simulated = false;
        spider.gameObject.SetActive(false);
        yellowGearVisual.enabled = purpleGearVisual.enabled = false;

        yellowSensor.enabled = purpleSensor.enabled = false;
        purpleSensor.isTrigger = false;
        yellowButton.gameObject.SetActive(false);
        purpleButton.gameObject.SetActive(false);
        if (!purpleButton.TryGetComponent<SeesawButtonSymbol>(out _))
            purpleButton.gameObject.AddComponent<SeesawButtonSymbol>();
        rightPlatform.SetRevealProgress(0f);
        ConfigureGate(gate);
        ConfigureGate(leftGate);
        gate.gameObject.AddComponent<SeesawGateFeedback>().Initialize(this, player, gateVisual.GetComponent<SpriteRenderer>());
        leftGate.gameObject.AddComponent<SeesawGateFeedback>().Initialize(this, player, leftGateVisual.GetComponent<SpriteRenderer>());
    }

    private void OnEnable()
    {
        // Remove labels left by the previous scripts during a Play Mode reload.
        foreach (string name in new[] { "Blue Gear Hint", "Green Gear Hint" })
        {
            Transform oldHint = transform.Find(name);
            if (oldHint == null) continue;
            oldHint.gameObject.SetActive(false);
            Destroy(oldHint.gameObject);
        }
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
        if (gearBarrier != null) gearBarrier.BlockBall(ball);
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
        if (!IsRevealing || Time.timeScale <= 0f ||
            blueGear.WaitingForAssemblyTurn || greenGear.WaitingForAssemblyTurn) return;
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
        ApplyReveal(0f);
    }

    private void ApplyReveal(float t)
    {
        rightPlatform.SetRevealProgress(t);
        rightGateShatter.SetProgress(t);
        leftGateShatter.SetProgress(t);

        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .77f, t));
        float grow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .92f, t));
        if (rise <= 0f) return;

        Vector3 spiderPosition = spiderRest + Vector3.down * (1.8f * (1f - rise));
        spider.transform.position = spiderPosition;
        spiderBody.position = spiderPosition;
        if (!emergenceStarted)
        {
            emergenceStarted = true;
            spider.gameObject.SetActive(true);
            yellowGearVisual.enabled = true;
            if (t < 1f)
            {
                BurstDust(spiderRest + Vector3.down * .86f, Vector2.up, new Color32(188, 153, 120, 230));
                BurstDust(new Vector3(yellowRest.x, ceilingEdge, yellowRest.z), Vector2.down,
                    new Color32(219, 192, 145, 220));
            }
        }

        Vector3 scale = yellowFullScale;
        scale.y *= Mathf.Max(.001f, grow);
        yellowButton.localScale = scale;
        yellowButton.position = yellowRest + Vector3.up * (yellowHeight * .5f * (1f - grow));
        yellowButton.gameObject.SetActive(grow > 0f);
    }

    private void FinishReveal()
    {
        IsRevealing = false;
        RevealProgress = 1f;
        spiderBody.simulated = spiderWasSimulated;
        yellowButton.position = yellowRest;
        yellowSensor.enabled = true;
        gate.enabled = false;
        gateVisual.SetActive(false);
        leftGate.enabled = false;
        leftGateVisual.SetActive(false);
        RestoreControls();
    }

    public void RevealPurpleButton(float progress)
    {
        if (progress <= 0f) return;
        purpleGearVisual.enabled = true;
        if (!purpleButton.gameObject.activeSelf) purpleButton.gameObject.SetActive(true);
        float t = Mathf.SmoothStep(0f, 1f, progress);
        Bounds floor = rightPlatform.GetComponent<BoxCollider2D>().bounds;
        Vector3 scale = purpleFullScale;
        scale.y *= Mathf.Max(.001f, t);
        purpleButton.localScale = scale;
        Bounds cap = purpleRenderer.bounds;
        Vector3 position = purpleButton.position;
        // Keep the whole cap supported at the inner end of the extended floor.
        position.x += floor.min.x + cap.extents.x - cap.center.x;
        position.y += floor.max.y - cap.min.y;
        purpleButton.position = position;
        purpleSensor.enabled = progress >= 1f;
    }

    private void BurstDust(Vector3 origin, Vector2 direction, Color32 color)
    {
        if (emergenceDust == null)
        {
            var dust = new GameObject("Seesaw emergence dust");
            dust.transform.SetParent(transform, false);
            emergenceDust = dust.AddComponent<ParticleSystem>();
            emergenceDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = emergenceDust.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .35f;
            var emission = emergenceDust.emission;
            emission.enabled = false;
            var fade = emergenceDust.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = dust.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = yellowRenderer.sharedMaterial;
            renderer.sortingLayerID = yellowRenderer.sortingLayerID;
            renderer.sortingOrder = 14;
        }

        emergenceDust.Play();
        for (int i = 0; i < 16; i++)
        {
            float spread = i / 15f * 2f - 1f;
            emergenceDust.Emit(new ParticleSystem.EmitParams
            {
                position = origin + Vector3.right * (spread * .4f),
                velocity = Vector2.right * (spread * 2.2f) + direction * (1.35f + (i % 4) * .45f),
                startLifetime = .38f + (i % 4) * .072f,
                startSize = .08f + (i % 3) * .035f,
                startColor = color
            }, 1);
        }
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
        // Preserve the vertical view of the underpass when browser toolbars make the canvas wide.
        float aspect = Mathf.Clamp(camera.aspect, .1f, 16f / 9f);
        position = new Vector3(fullView.center.x, fullView.center.y, z);
        size = Mathf.Max(fullView.height * .5f, fullView.width / (2f * aspect));
    }

    public bool ContainsPosition(Vector2 position)
        => position.x >= fullView.xMin - 1f && position.x <= fullView.xMax + 1f &&
           position.y >= fullView.yMin - 6f && position.y <= fullView.yMax + 1f;
}
