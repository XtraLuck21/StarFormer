using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerPossessionController : MonoBehaviour
{
    [SerializeField] private PossessableBody initialBody;
    [Tooltip("Optional host controlled at a fresh scene start. Keep Initial Body assigned to the Star.")]
    [SerializeField] private PossessableBody startingHost;

    [Header("Height Limit")]
    [Tooltip("When enabled, leaving the Y range below restarts the level. " +
             "Turn this off for large maps that have no ceiling/floor kill.")]
    [SerializeField] private bool useHeightLimit = true;

    [Tooltip("Falling below this Y restarts (only if Height Limit is on).")]
    [SerializeField] private float minimumHeight = -10f;

    [Tooltip("Rising above this Y restarts (only if Height Limit is on).")]
    [SerializeField] private float maximumHeight = 8f;
    [SerializeField] private float maximumRiseSpeed = 3f;
    [SerializeField] private float deathDelay = 2f;

    private RestartMenuUI restartMenu;
    private CheckpointFeedbackUI checkpointFeedback;
    public bool HasSavedCheckpoint => HasCheckpointInCurrentScene || HasBouncyBallCheckpointInCurrentScene;

    public CheckpointFeedbackUI CheckpointFeedback
    {
        get
        {
            if (checkpointFeedback == null)
            {
                checkpointFeedback = GetComponent<CheckpointFeedbackUI>();
                if (checkpointFeedback == null)
                    checkpointFeedback = gameObject.AddComponent<CheckpointFeedbackUI>();
                checkpointFeedback.Initialize(this);
            }
            return checkpointFeedback;
        }
    }

    [Header("Fan Repossess Cooldown")]
    [Tooltip("Seconds after leaving a fan before the star can possess a fan again.")]
    [SerializeField] private float fanRepossessCooldown = 2f;

    [Header("Water Movement")]
    [SerializeField, Min(0f)] private float starWaterSpeedMultiplier = 0.3f;
    [SerializeField, Min(0f)] private float boxWaterSpeedMultiplier = 0.7f;
    [SerializeField, Min(0f)] private float bubbleWaterSpeedMultiplier = 1.3f;
    [SerializeField, Min(0.01f)] private float swimAcceleration = 12f;
    [SerializeField, Min(0.01f)] private float swimBraking = 10f;
    [SerializeField, Range(0f, 1f)] private float starWaterJumpMultiplier = 0.75f;

    [Header("Bouncy Ball Checkpoints")]
    [Tooltip("Opt in to respawning as the last possessed Bouncy Ball after death. Enabled in Level01 only.")]
    [SerializeField] private bool enableBouncyBallCheckpoints;

    [Header("Bouncy Ball Movement")]
    [SerializeField, Min(0.01f)] private float ballAcceleration = 35f;
    [SerializeField, Min(0.01f)] private float ballBraking = 28f;
    [SerializeField, Range(0f, 1f)] private float ballWaterSpeedMultiplier = 0.75f;
    [SerializeField] private float ballMaximumHeight = 40f;

    [Header("Possession Stability")]
    [SerializeField, Min(0f)] private float possessionSeparationDistance = 0.35f;
    [SerializeField, Min(1)] private int possessionSeparationFrames = 3;

    [Tooltip("Spider hovers slightly off surfaces, so allow a small gap to " +
             "still count as touching when completing a possession.")]
    [SerializeField, Min(0f)] private float possessionTouchTolerance = 0.1f;

    private bool isDead;
    private float deathRestartTime;

    public bool IsDead => isDead;
    public PossessableBody CurrentBody => currentBody;

    /// <summary>
    /// This frame's vertical input (-1 to 1), combining W/S and touch Up/Down.
    /// Box resizing reads this value instead of polling the keyboard separately.
    /// </summary>
    public float VerticalInput => verticalInput;

    /// <summary>
    /// This frame's horizontal input (-1 to 1), combining A/D and touch Left/Right.
    /// Fan facing reads this value instead of polling the keyboard separately.
    /// </summary>
    public float HorizontalInput => horizontalInput;

    private PossessableBody currentBody;
    private PossessableBody pendingSource;
    private PossessableBody pendingTarget;

    private readonly HashSet<PossessableBody> blockedTargets =
        new HashSet<PossessableBody>();
    private readonly Dictionary<PossessableBody, int> separationFrames =
        new Dictionary<PossessableBody, int>();
    private readonly List<PossessableBody> blockedTargetBuffer =
        new List<PossessableBody>();

    private readonly Dictionary<PossessableBody, Vector3> bouncyBallSpawnPositions =
        new Dictionary<PossessableBody, Vector3>();
    private BouncyBallCheckpointSession checkpointSession;

    private bool HasBouncyBallCheckpointInCurrentScene =>
        enableBouncyBallCheckpoints && checkpointSession != null &&
        checkpointSession.HasCheckpoint &&
        checkpointSession.ScenePath == gameObject.scene.path;

    private sealed class BubbleHandoffCollision
    {
        public Collider2D Box;
        public Collider2D Bubble;
        public int SeparatedFrames;
    }

    // Independent of possession locks: a later transfer must not leave an old
    // box/bubble pair permanently ignoring each other.
    private readonly List<BubbleHandoffCollision> bubbleHandoffCollisions =
        new List<BubbleHandoffCollision>();

    private float horizontalInput;
    private float verticalInput;
    private bool actionRequested;
    // E / Release handles bubble popping and leaving Spider or Fan.
    private bool releaseRequested;
    // Queue hazard-triggered bubble pops separately from player input.
    private bool bubblePopRequested;
    private float fanRepossessBlockedUntil;
    private bool restartRequested;

    // ===== Touch button state (written by TouchHoldButton) =====
    private bool touchLeftHeld;
    private bool touchRightHeld;
    private bool touchUpHeld;
    private bool touchDownHeld;

    private static bool hasCheckpoint;
    private static string checkpointScene;
    private static string checkpointId;
    private static Vector3 checkpointPosition;

    public bool HasCheckpointInCurrentScene =>
        hasCheckpoint &&
        checkpointScene == gameObject.scene.path;

    public bool IsCurrentCheckpoint(string id)
    {
        return HasCheckpointInCurrentScene && checkpointId == id;
    }

    public bool IsCurrentBouncyBallCheckpoint(string ballName)
    {
        return !HasCheckpointInCurrentScene && HasBouncyBallCheckpointInCurrentScene &&
            checkpointSession.BallName == ballName;
    }

    // Public so main-menu level entry can also clear the session for a fresh run.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCheckpointSession()
    {
        hasCheckpoint = false;
        checkpointScene = null;
        checkpointId = null;
        checkpointPosition = Vector3.zero;
        BouncyBallCheckpointSession.ClearCurrent();
    }

    public void SetCheckpoint(string id, Vector3 position)
    {
        if (isDead || restartRequested || string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        hasCheckpoint = true;
        checkpointScene = gameObject.scene.path;
        checkpointId = id;
        checkpointPosition = position;

        Debug.Log($"check point activated:{id}", this);
        AnalyticsManager.Instance?.TrackCheckpoint(id, currentBody);
        CheckpointFeedback.ShowSaved();
    }

    // ===================================================================
    // Touch input API: UI buttons use these methods without changing physics logic.
    // ===================================================================

    public void SetTouchLeft(bool held) => touchLeftHeld = held;
    public void SetTouchRight(bool held) => touchRightHeld = held;
    public void SetTouchUp(bool held) => touchUpHeld = held;
    public void SetTouchDown(bool held) => touchDownHeld = held;

    /// <summary>Space: Star jump / SpikeWall fire. Requests an active ability only.</summary>
    public void PressTouchAction()
    {
        if (!isActiveAndEnabled || isDead || restartRequested || Time.timeScale <= 0f)
        {
            return;
        }

        actionRequested = true;
    }

    /// <summary>
    /// E / Release: leave the current form.
    /// Bubble and Spider use PopBubble to restore Star; Fan uses LeaveFan.
    /// </summary>
    public void RequestRelease()
    {
        if (!isActiveAndEnabled ||
            isDead ||
            restartRequested ||
            Time.timeScale == 0f)
        {
            return;
        }

        releaseRequested = true;
    }

    /// <summary>Legacy name retained as an alias for RequestRelease.</summary>
    public void PressTouchPop()
    {
        RequestRelease();
    }

    /// <summary>
    /// Toggle the Fan's wind (W on the keyboard): press once for on, again for off.
    /// Only applies while possessing Fan, matching the form-specific firing behavior.
    /// </summary>
    public void PressTouchBlow()
    {
        if (!isActiveAndEnabled ||
            isDead ||
            restartRequested ||
            Time.timeScale == 0f ||
            currentBody == null ||
            currentBody.Form != BodyForm.Fan)
        {
            return;
        }

        if (currentBody.TryGetComponent(out FanBlower blower))
        {
            blower.ToggleBlow();
        }
    }

    /// <summary>Open the same restart choices as the R key.</summary>
    public void PressTouchRestart()
    {
        if (!isActiveAndEnabled || restartRequested || (!isDead && Time.timeScale <= 0f)) return;
        if (restartMenu == null) restartMenu = gameObject.AddComponent<RestartMenuUI>();
        restartMenu.Show(this);
    }

    public bool ConfirmManualRestart(bool clearCheckpoint)
    {
        if (!isActiveAndEnabled || restartRequested) return false;
        if (gameObject.scene.buildIndex < 0)
        {
            Debug.LogError("Add the current scene to the enabled build scene list before restarting.", this);
            return false;
        }
        if (!clearCheckpoint && !HasSavedCheckpoint) return false;
        AnalyticsManager.Instance?.TrackManualRestart();
        if (clearCheckpoint) ResetCheckpointSession();
        RequestRestart();
        return true;
    }

    public void ClearInputForRestartMenu()
    {
        horizontalInput = verticalInput = 0f;
        actionRequested = releaseRequested = false;
        ReleaseAllTouchInput();
    }

    public void ResumeAfterRestartMenu(float pausedDuration)
    {
        // Death recovery also uses unscaled time; browsing the menu must not consume its delay.
        if (isDead) deathRestartTime += pausedDuration;
        ClearInputForRestartMenu();
    }

    /// <summary>Release all directions when changing forms, dying, or restarting.</summary>
    public void ReleaseAllTouchInput()
    {
        touchLeftHeld = false;
        touchRightHeld = false;
        touchUpHeld = false;
        touchDownHeld = false;
    }

    private void Start()
    {
        if (initialBody == null ||
            !initialBody.isActiveAndEnabled ||
            !initialBody.IsReady)
        {
            Debug.LogError(
                "Assign a configured, active initial body.",
                this
            );

            enabled = false;
            return;
        }

        if (gameObject.scene.name != "Mainmenu")
        {
            CheckpointFeedback.Initialize(this);
        }

        if (enableBouncyBallCheckpoints)
        {
            checkpointSession = BouncyBallCheckpointSession.Instance;
            CacheBouncyBallSpawnPositions();
            // Existing positional checkpoints retain priority over ball respawns.
            if (!HasCheckpointInCurrentScene && TryRestoreBouncyBallCheckpoint())
            {
                BeginAnalyticsForCurrentBody();
                return;
            }
        }

        if (HasCheckpointInCurrentScene)
        {
            initialBody.transform.position = checkpointPosition;
            initialBody.Body.position = (Vector2)checkpointPosition;
            initialBody.Body.linearVelocity = Vector2.zero;
            initialBody.Body.angularVelocity = 0f;

            Physics2D.SyncTransforms();
        }

        // Existing checkpoint respawns keep their original Star behavior.
        bool enterStartingHost = !HasCheckpointInCurrentScene && startingHost != null;
        if (enterStartingHost &&
            (initialBody.Form != BodyForm.Star || startingHost == initialBody ||
             startingHost.Form == BodyForm.Star || !startingHost.isActiveAndEnabled ||
             !startingHost.IsReady || startingHost.gameObject.scene != gameObject.scene))
        {
            Debug.LogError("Starting Host must be an active, configured non-Star body in this scene, with Initial Body set to the Star.", this);
            enabled = false;
            return;
        }

        currentBody = initialBody;
        currentBody.SetOwner(this);
        if (enterStartingHost)
        {
            TransferTo(startingHost);
        }

        BeginAnalyticsForCurrentBody();
    }

    private void Update()
    {
        if (restartRequested)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
            return;
        }

        horizontalInput = 0f;
        verticalInput = 0f;
        Keyboard keyboard = Keyboard.current;

        if (restartMenu != null && restartMenu.IsOpen)
        {
            if (keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                restartMenu.Close();
            return;
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            PressTouchRestart();
            return;
        }

        if (isDead)
        {
            actionRequested = false;
            ReleaseAllTouchInput();

            if (Time.unscaledTime >= deathRestartTime)
            {
                RequestRestart();
            }

            return;
        }

        if (currentBody != null &&
            currentBody.Form == BodyForm.gravityBall)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            actionRequested = false;
            releaseRequested = false;
            return;
        }

        // ---- Keyboard input (skip this block when no keyboard is available) ----
        // Do not return and clear actionRequested when keyboard is null:
        // that would discard a jump requested by a touch button on the next frame.
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontalInput -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontalInput += 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                verticalInput += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                verticalInput -= 1f;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                // E consistently means Release: pop Bubble or leave Spider/Fan.
                RequestRelease();
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                // Space requests active abilities only: Star jump or SpikeWall fire.
                actionRequested = true;
            }
        }

        // ---- Add touch input to keyboard input (both work in the Editor) ----
        if (touchLeftHeld)
        {
            horizontalInput -= 1f;
        }

        if (touchRightHeld)
        {
            horizontalInput += 1f;
        }

        if (touchUpHeld)
        {
            verticalInput += 1f;
        }

        if (touchDownHeld)
        {
            verticalInput -= 1f;
        }

        // Opposing directions cancel; combined keyboard/touch input stays within [-1, 1].
        horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);
    }

    private void FixedUpdate()
    {
        UpdateBubbleHandoffCollisions();
        if (currentBody == null || restartRequested || isDead)
        {
            return;
        }

        Bounds bodyBounds = currentBody.BodyCollider.bounds;

        if (useHeightLimit)
        {
            float activeMaximumHeight =
                currentBody.Form == BodyForm.BouncyBall
                    ? ballMaximumHeight
                    : maximumHeight;

            if (bodyBounds.max.y < minimumHeight)
            {
                KillControlledBody(
                    currentBody,
                    AnalyticsDeathCause.OutOfBoundsLow
                );
                return;
            }

            if (bodyBounds.min.y > activeMaximumHeight)
            {
                KillControlledBody(
                    currentBody,
                    AnalyticsDeathCause.OutOfBoundsHigh
                );
                return;
            }
        }

        // Keep keyboard exits and hazard pops ahead of pending possession.
        bool shouldPopBubble =
            bubblePopRequested && currentBody.Form == BodyForm.Bubble;
        bubblePopRequested = false;

        bool shouldRelease = releaseRequested;
        releaseRequested = false;

        // Bubble and Spider both return to Star through the same release method.
        bool releaseToStar =
            shouldRelease &&
            (currentBody.Form == BodyForm.Bubble ||
             currentBody.Form == BodyForm.Spider);

        if (shouldPopBubble || releaseToStar)
        {
            PopBubble();
            return;
        }

        if (shouldRelease && currentBody.Form == BodyForm.Fan)
        {
            LeaveFan();
            return;
        }

        UpdateContactLocks();

        if (pendingTarget != null)
        {
            PossessableBody source = pendingSource;
            PossessableBody target = pendingTarget;
            pendingSource = null;
            pendingTarget = null;

            if (CanCompletePossession(source, target))
            {
                TransferTo(target);
            }
        }

        if (currentBody.Form == BodyForm.Spider)
        {
            if (currentBody.TryGetComponent(
                out HexagonSurfaceMovement hexagon))
            {
                hexagon.Tick(horizontalInput);
            }

            actionRequested = false;
            return;
        }

        Rigidbody2D body = currentBody.Body;
        Vector2 velocity = body.linearVelocity;
        bool inWater = currentBody.IsInWater;

        switch (currentBody.Form)
        {
            case BodyForm.Star:
                {
                    float speed = currentBody.MoveSpeed;

                    if (inWater)
                    {
                        speed *= starWaterSpeedMultiplier;
                    }

                    velocity.x = horizontalInput * speed;

                    if (actionRequested &&
                        velocity.y <= 0.1f &&
                        currentBody.IsGrounded())
                    {
                        velocity.y = currentBody.JumpSpeed *
                            (inWater ? starWaterJumpMultiplier : 1f);
                    }

                    break;
                }

            case BodyForm.Box:
                {
                    if (currentBody.IsGrounded())
                    {
                        float speed = currentBody.MoveSpeed;

                        if (inWater)
                        {
                            speed *= boxWaterSpeedMultiplier;
                        }

                        velocity.x = horizontalInput * speed;
                    }

                    break;
                }

            case BodyForm.Bubble:
                {
                    if (inWater)
                    {
                        Vector2 swimInput = Vector2.ClampMagnitude(
                            new Vector2(horizontalInput, verticalInput),
                            1f
                        );

                        Vector2 targetVelocity =
                            swimInput *
                            currentBody.MoveSpeed *
                            bubbleWaterSpeedMultiplier;

                        targetVelocity = currentBody.RemoveBubbleWallPush(
                            targetVelocity
                        );

                        float acceleration = swimInput.sqrMagnitude > 0f
                            ? swimAcceleration
                            : swimBraking;

                        velocity = Vector2.MoveTowards(
                            velocity,
                            targetVelocity,
                            acceleration * Time.fixedDeltaTime
                        );
                    }
                    else
                    {
                        velocity.x = horizontalInput * currentBody.MoveSpeed;
                        velocity.y = Mathf.Min(velocity.y, maximumRiseSpeed);
                    }

                    velocity = currentBody.RemoveBubbleWallPush(velocity);
                    break;
                }

            case BodyForm.SpikeWall:
                {
                    bool hasShooter = currentBody.TryGetComponent(
                        out SpikeShooterAbility shooter);

                    // Pressing left or right turns the spikes to that side.
                    if (hasShooter && horizontalInput != 0f)
                    {
                        shooter.SetFacing(horizontalInput > 0f);
                    }

                    if (currentBody.IsGrounded())
                    {
                        velocity.x = horizontalInput * currentBody.MoveSpeed;
                    }

                    if (hasShooter && actionRequested)
                    {
                        shooter.TryFire();
                    }

                    break;
                }

            case BodyForm.gravityBall:
                {
                    // keep the small ball rolling/player don control
                    actionRequested = false;
                    releaseRequested = false;
                    return;
                }

            case BodyForm.Fan:
                {
                    if (currentBody.IsGrounded())
                    {
                        velocity.x = horizontalInput * currentBody.MoveSpeed;
                    }

                    break;
                }

            case BodyForm.BouncyBall:
                {
                    float targetSpeed =
                        horizontalInput *
                        currentBody.MoveSpeed *
                        (inWater ? ballWaterSpeedMultiplier : 1f);

                    float acceleration = Mathf.Abs(horizontalInput) > 0f
                        ? ballAcceleration
                        : ballBraking;

                    velocity.x = Mathf.MoveTowards(
                        velocity.x,
                        targetSpeed,
                        acceleration * Time.fixedDeltaTime
                    );

                    break;
                }
        }

        body.linearVelocity = velocity;
        actionRequested = false;
    }

    public void RequestRestart()
    {
        if (restartRequested)
        {
            return;
        }

        if (gameObject.scene.buildIndex < 0)
        {
            Debug.LogError(
                "Add the current scene to the enabled build scene list before restarting.",
                this
            );
            return;
        }

        restartRequested = true;
        bubblePopRequested = false;
        pendingSource = null;
        pendingTarget = null;
        horizontalInput = 0f;
        actionRequested = false;
        ReleaseAllTouchInput();
    }

    public void KillControlledBody(
        PossessableBody hitBody,
        AnalyticsDeathCause deathCause = AnalyticsDeathCause.Unknown,
        float restartDelayOverride = -1f)
    {
        if (!isActiveAndEnabled ||
            isDead ||
            restartRequested ||
            hitBody == null ||
            hitBody != currentBody ||
            Time.timeScale == 0f)
        {
            return;
        }

        AnalyticsManager.Instance?.TrackDeath(hitBody, deathCause);

        isDead = true;
        bubblePopRequested = false;
        pendingSource = null;
        pendingTarget = null;
        horizontalInput = 0f;
        actionRequested = false;
        ReleaseAllTouchInput();

        float activeDeathDelay = restartDelayOverride >= 0f
            ? restartDelayOverride
            : deathDelay;
        deathRestartTime = Time.unscaledTime + activeDeathDelay;
        Time.timeScale = 0f;
    }

    public void RequestPossession(
        PossessableBody source,
        PossessableBody target)
    {
        if (!isActiveAndEnabled ||
            restartRequested ||
            isDead ||
            source != currentBody ||
            target == currentBody ||
            blockedTargets.Contains(target) ||
            pendingTarget != null ||
            !target.isActiveAndEnabled ||
            !target.IsReady)
        {
            return;
        }

        // Block re-entering a fan right after leaving one.
        if (target.Form == BodyForm.Fan &&
            Time.time < fanRepossessBlockedUntil)
        {
            return;
        }

        pendingSource = source;
        pendingTarget = target;
    }

    private void OnDisable()
    {
        if (restartMenu != null) restartMenu.Close();
        RestoreBubbleHandoffCollisions();
        bubblePopRequested = false;
        ReleaseAllTouchInput();
        ClearPossessionContacts();

        if (isDead)
        {
            Time.timeScale = 1f;
        }
    }

    public void RequestBubblePop(PossessableBody hitBody)
    {
        if (!isActiveAndEnabled ||
            isDead ||
            restartRequested ||
            Time.timeScale == 0f ||
            hitBody == null ||
            hitBody != currentBody ||
            hitBody.Form != BodyForm.Bubble)
        {
            return;
        }

        // E and bubble hazards share this queue, independently of Space actions.
        // FixedUpdate processes it before a pending possession.
        bubblePopRequested = true;
    }

    private void PopBubble()
    {
        actionRequested = false;
        releaseRequested = false;
        bubblePopRequested = false;

        if (initialBody == null ||
            !initialBody.IsReady ||
            initialBody.Form != BodyForm.Star)
        {
            Debug.LogError(
                "Popping a bubble requires a valid initial star body.",
                this
            );

            return;
        }

        PossessableBody bubble = currentBody;
        Vector2 releasePosition = bubble.Body.position;
        Vector2 releaseVelocity = bubble.Body.linearVelocity;

        ClearPossessionContacts();

        // Remove upward momentum so the released star starts falling.
        releaseVelocity.y = Mathf.Min(releaseVelocity.y, 0f);

        pendingTarget = null;

        bubble.SetOwner(null);

        // Existing loaned bodies return first; never also clone the same body.
        bool spawnedReplacement = false;
        if (bubble.TryGetComponent(out ReturnToSpawnOnLeave respawner))
        {
            respawner.ReturnToSpawn();
        }
        else
        {
            spawnedReplacement = bubble.Form == BodyForm.Bubble &&
                bubble.TryGetComponent(out BubbleRespawnOnPop popRespawner) &&
                popRespawner.TrySpawnReplacement();
            bubble.gameObject.SetActive(false);
        }

        // Position the inactive star before restoring its physics and visuals.
        Transform starTransform = initialBody.transform;
        starTransform.position = new Vector3(
            releasePosition.x,
            releasePosition.y,
            starTransform.position.z
        );

        initialBody.gameObject.SetActive(true);
        initialBody.Body.position = releasePosition;
        initialBody.Body.linearVelocity = releaseVelocity;
        initialBody.Body.angularVelocity = 0f;

        currentBody = initialBody;
        currentBody.SetOwner(this);
        AnalyticsManager.Instance?.ChangeControlledForm(
            bubble.Form,
            currentBody.Form,
            currentBody.Body.position,
            bubble.Form == BodyForm.Bubble ? "bubble_pop" : "leave_form"
        );
        BlockCurrentContacts();

        if (spawnedReplacement)
        {
            Destroy(bubble.gameObject);
        }

        Debug.Log("Bubble popped. Returned to star form.", currentBody);
    }

    private void LeaveFan()
    {
        if (initialBody == null ||
            !initialBody.IsReady ||
            initialBody.Form != BodyForm.Star)
        {
            Debug.LogError(
                "Leaving a fan requires a valid initial star body.",
                this
            );

            return;
        }

        PossessableBody fan = currentBody;
        Vector2 releasePosition = fan.Body.position;
        Vector2 releaseVelocity = fan.Body.linearVelocity;

        pendingTarget = null;

        if (fan.TryGetComponent(out HexagonSurfaceMovement hexagon))
        {
            hexagon.Release();
        }

        // Fan stays in the world and keeps blowing its last direction.
        fan.SetOwner(null);

        if (fanRepossessCooldown > 0f)
        {
            fanRepossessBlockedUntil = Time.time + fanRepossessCooldown;
        }

        Transform starTransform = initialBody.transform;
        starTransform.position = new Vector3(
            releasePosition.x,
            releasePosition.y,
            starTransform.position.z
        );

        initialBody.gameObject.SetActive(true);
        initialBody.Body.position = releasePosition;
        initialBody.Body.linearVelocity = releaseVelocity;
        initialBody.Body.angularVelocity = 0f;

        currentBody = initialBody;
        currentBody.SetOwner(this);
        AnalyticsManager.Instance?.ChangeControlledForm(
            fan.Form,
            currentBody.Form,
            currentBody.Body.position,
            "leave_form"
        );
        BlockTarget(fan);
        actionRequested = false;
    }

    private void TransferTo(PossessableBody target)
    {
        bubblePopRequested = false;
        PossessableBody previousBody = currentBody;

        if (previousBody.TryGetComponent(
            out HexagonSurfaceMovement previousHexagon))
        {
            previousHexagon.Release();
        }

        previousBody.SetOwner(null);

        // Transfer ownership successfully first.
        currentBody = target;
        currentBody.SetOwner(this);
        BeginBubbleHandoff(previousBody, currentBody);
        AnalyticsManager.Instance?.ChangeControlledForm(
            previousBody.Form,
            currentBody.Form,
            currentBody.Body.position,
            "possession"
        );
        ActivateBouncyBallCheckpoint(currentBody);

        horizontalInput = 0f;
        verticalInput = 0f;
        actionRequested = false;
        releaseRequested = false;

        bool starEnteredHost = previousBody == initialBody;
        bool destroyedPreviousBody = false;

        if (starEnteredHost)
        {
            // Keep Star available so it can be restored when Bubble pops.
            previousBody.gameObject.SetActive(false);
        }
        else if (previousBody.Form == BodyForm.gravityBall ||
            previousBody.Form == BodyForm.Spider)
        {
            // Loaned spider/ball with ReturnToSpawnOnLeave goes back to spawn.
            if (previousBody.TryGetComponent(out ReturnToSpawnOnLeave respawner))
            {
                respawner.ReturnToSpawn();
            }
            else
            {
                UnblockTarget(previousBody);
                previousBody.gameObject.SetActive(false);
                Destroy(previousBody.gameObject);
                destroyedPreviousBody = true;
            }
        }

        if (!starEnteredHost && !destroyedPreviousBody)
        {
            BlockTarget(previousBody);
        }

        // A newly possessed body can already be touching several forms. Those
        // contacts must end before any of them can start another transfer.
        BlockCurrentContacts();

        // Do not carry a jump press into a newly possessed body.
        actionRequested = false;

        // Non-star hosts remain in the world. Their own ability/physics decides
        // how they behave while unpossessed.

        Debug.Log($"Possessed {currentBody.Form}.", currentBody);
    }

    private void BeginBubbleHandoff(PossessableBody previous, PossessableBody next)
    {
        if (previous.Form != BodyForm.Box || next.Form != BodyForm.Bubble)
        {
            return;
        }

        Collider2D box = previous.BodyCollider;
        Collider2D bubble = next.BodyCollider;
        if (!IsActiveCollider(box) || !IsActiveCollider(bubble) ||
            Physics2D.GetIgnoreCollision(box, bubble))
        {
            // Do not take ownership of an exclusion configured by another system.
            return;
        }

        // Release only the old host's pressure. Terrain and other bodies remain solid.
        Physics2D.IgnoreCollision(box, bubble, true);
        bubbleHandoffCollisions.Add(new BubbleHandoffCollision
        {
            Box = box,
            Bubble = bubble
        });
    }

    private void UpdateBubbleHandoffCollisions()
    {
        for (int i = bubbleHandoffCollisions.Count - 1; i >= 0; i--)
        {
            BubbleHandoffCollision pair = bubbleHandoffCollisions[i];
            if (!IsActiveCollider(pair.Box) || !IsActiveCollider(pair.Bubble))
            {
                // Unity drops pair exclusions when a collider is deactivated/destroyed.
                bubbleHandoffCollisions.RemoveAt(i);
                continue;
            }

            Bounds clearance = pair.Bubble.bounds;
            clearance.Expand(possessionSeparationDistance * 2f);
            if (clearance.Intersects(pair.Box.bounds))
            {
                pair.SeparatedFrames = 0;
                continue;
            }

            pair.SeparatedFrames++;
            if (pair.SeparatedFrames < possessionSeparationFrames)
            {
                continue;
            }

            Physics2D.IgnoreCollision(pair.Box, pair.Bubble, false);
            bubbleHandoffCollisions.RemoveAt(i);
        }
    }

    private void RestoreBubbleHandoffCollisions()
    {
        foreach (BubbleHandoffCollision pair in bubbleHandoffCollisions)
        {
            if (IsActiveCollider(pair.Box) && IsActiveCollider(pair.Bubble))
            {
                Physics2D.IgnoreCollision(pair.Box, pair.Bubble, false);
            }
        }

        bubbleHandoffCollisions.Clear();
    }

    private static bool IsActiveCollider(Collider2D collider)
    {
        return collider != null && collider.enabled &&
            collider.gameObject.activeInHierarchy;
    }

    private void CacheBouncyBallSpawnPositions()
    {
        bouncyBallSpawnPositions.Clear();

        PossessableBody[] bodies = FindObjectsByType<PossessableBody>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (PossessableBody body in bodies)
        {
            if (body != null &&
                body.gameObject.scene == gameObject.scene &&
                body.Form == BodyForm.BouncyBall)
            {
                bouncyBallSpawnPositions[body] = body.transform.position;
                CheckpointFeedback.RegisterMarker(body.transform, body.name, true);
            }
        }
    }

    private void ActivateBouncyBallCheckpoint(PossessableBody ball)
    {
        if (!enableBouncyBallCheckpoints ||
            ball == null ||
            ball.Form != BodyForm.BouncyBall ||
            isDead ||
            restartRequested)
        {
            return;
        }

        if (!bouncyBallSpawnPositions.TryGetValue(
            ball,
            out Vector3 spawnPosition))
        {
            spawnPosition = ball.transform.position;
        }

        checkpointSession = BouncyBallCheckpointSession.GetOrCreate();
        bool isNewCheckpoint = !checkpointSession.HasCheckpoint ||
            checkpointSession.ScenePath != gameObject.scene.path ||
            checkpointSession.BallName != ball.name;
        checkpointSession.Save(
            gameObject.scene.path,
            ball.name,
            spawnPosition
        );

        if (isNewCheckpoint)
        {
            AnalyticsManager.Instance?.TrackCheckpoint(ball.name, ball);
            CheckpointFeedback.ShowSaved();
        }

        Debug.Log($"Bouncy Ball checkpoint activated: {ball.name}.", ball);
    }

    private bool TryRestoreBouncyBallCheckpoint()
    {
        if (!HasBouncyBallCheckpointInCurrentScene ||
            string.IsNullOrWhiteSpace(checkpointSession.BallName))
        {
            return false;
        }

        PossessableBody checkpointBall = null;

        foreach (PossessableBody ball in bouncyBallSpawnPositions.Keys)
        {
            if (ball != null &&
                ball.name == checkpointSession.BallName &&
                ball.isActiveAndEnabled &&
                ball.IsReady)
            {
                checkpointBall = ball;
                break;
            }
        }

        if (checkpointBall == null)
        {
            Debug.LogWarning(
                $"Checkpoint ball '{checkpointSession.BallName}' was not found. " +
                "Restarting from the beginning.",
                this
            );

            BouncyBallCheckpointSession.ClearCurrent();
            return false;
        }

        checkpointBall.transform.position = checkpointSession.Position;
        checkpointBall.Body.position = (Vector2)checkpointSession.Position;
        checkpointBall.Body.linearVelocity = Vector2.zero;
        checkpointBall.Body.angularVelocity = 0f;

        initialBody.SetOwner(null);
        initialBody.gameObject.SetActive(false);

        currentBody = checkpointBall;
        currentBody.SetOwner(this);

        Physics2D.SyncTransforms();
        BlockCurrentContacts();

        Debug.Log(
            $"Respawned as Bouncy Ball at checkpoint: {checkpointBall.name}.",
            checkpointBall
        );

        return true;
    }

    private void BeginAnalyticsForCurrentBody()
    {
        if (currentBody == null)
        {
            return;
        }

        string activeCheckpoint = AnalyticsValueNames.StartCheckpoint;
        if (HasCheckpointInCurrentScene)
        {
            activeCheckpoint = checkpointId;
        }
        else if (HasBouncyBallCheckpointInCurrentScene)
        {
            activeCheckpoint = checkpointSession.BallName;
        }

        AnalyticsManager.Instance?.BeginLevel(currentBody, activeCheckpoint);
    }

    private bool CanCompletePossession(
        PossessableBody source,
        PossessableBody target)
    {
        return source != null &&
            target != null &&
            source == currentBody &&
            source.isActiveAndEnabled &&
            source.IsReady &&
            target.isActiveAndEnabled &&
            target.IsReady &&
            !blockedTargets.Contains(target) &&
            AreCollidersClose(source.BodyCollider, target.BodyCollider);
    }

    // Spider hovers a small gap off every surface, so a strict IsTouching
    // check would never pass for it. Allow a small measured gap as well.
    private bool AreCollidersClose(Collider2D a, Collider2D b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.IsTouching(b))
        {
            return true;
        }

        ColliderDistance2D separation = Physics2D.Distance(a, b);

        return separation.isValid &&
            separation.distance <= possessionTouchTolerance;
    }

    private void BlockCurrentContacts()
    {
        if (currentBody == null ||
            !currentBody.isActiveAndEnabled ||
            currentBody.BodyCollider == null)
        {
            return;
        }

        Bounds contactBounds = currentBody.BodyCollider.bounds;
        contactBounds.Expand(possessionSeparationDistance * 2f);

        Collider2D[] nearbyColliders = Physics2D.OverlapBoxAll(
            contactBounds.center,
            contactBounds.size,
            0f
        );

        foreach (Collider2D contactCollider in nearbyColliders)
        {
            if (contactCollider == null ||
                !contactCollider.TryGetComponent(
                    out PossessableBody touchingBody) ||
                touchingBody == currentBody)
            {
                continue;
            }

            BlockTarget(touchingBody);
        }
    }

    private void BlockTarget(PossessableBody target)
    {
        if (target == null || target == currentBody)
        {
            return;
        }

        blockedTargets.Add(target);
        separationFrames[target] = 0;
    }

    private void UpdateContactLocks()
    {
        if (blockedTargets.Count == 0 || currentBody == null)
        {
            return;
        }

        blockedTargetBuffer.Clear();
        blockedTargetBuffer.AddRange(blockedTargets);

        foreach (PossessableBody target in blockedTargetBuffer)
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                target.BodyCollider == null ||
                !target.BodyCollider.enabled)
            {
                UnblockTarget(target);
                continue;
            }

            Bounds separationBounds = currentBody.BodyCollider.bounds;
            separationBounds.Expand(possessionSeparationDistance * 2f);

            if (separationBounds.Intersects(target.BodyCollider.bounds))
            {
                separationFrames[target] = 0;
                continue;
            }

            int frames = separationFrames[target] + 1;

            if (frames >= possessionSeparationFrames)
            {
                UnblockTarget(target);
            }
            else
            {
                separationFrames[target] = frames;
            }
        }
    }

    private void UnblockTarget(PossessableBody target)
    {
        blockedTargets.Remove(target);
        separationFrames.Remove(target);
    }

    private void ClearPossessionContacts()
    {
        pendingSource = null;
        pendingTarget = null;
        blockedTargets.Clear();
        separationFrames.Clear();
        blockedTargetBuffer.Clear();
    }
}
