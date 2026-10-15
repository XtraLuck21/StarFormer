using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerPossessionController : MonoBehaviour
{
    [SerializeField] private PossessableBody initialBody;

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

    private PossessableBody currentBody;
    private PossessableBody pendingSource;
    private PossessableBody pendingTarget;

    private readonly HashSet<PossessableBody> blockedTargets =
        new HashSet<PossessableBody>();
    private readonly Dictionary<PossessableBody, int> separationFrames =
        new Dictionary<PossessableBody, int>();
    private readonly List<PossessableBody> blockedTargetBuffer =
        new List<PossessableBody>();

    private float horizontalInput;
    private float verticalInput;
    private bool actionRequested;
    private bool leaveRequested;
    private float fanRepossessBlockedUntil;
    private bool restartRequested;
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

    // public：主菜单进入关卡时也要调用，保证是全新的一局。
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCheckpointSession()
    {
        hasCheckpoint = false;
        checkpointScene = null;
        checkpointId = null;
        checkpointPosition = Vector3.zero;
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

        if (HasCheckpointInCurrentScene)
        {
            initialBody.transform.position = checkpointPosition;
            initialBody.Body.position = (Vector2)checkpointPosition;
            initialBody.Body.linearVelocity = Vector2.zero;
            initialBody.Body.angularVelocity = 0f;

            Physics2D.SyncTransforms();
        }

        currentBody = initialBody;
        currentBody.SetOwner(this);
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

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            // R：清除检查点，整个关卡从头开始。
            ResetCheckpointSession();

            RequestRestart();
            return;
        }

        if (isDead)
        {
            actionRequested = false;

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
            leaveRequested = false;
            return;
        }

        if (keyboard == null)
        {
            actionRequested = false;
            return;
        }

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

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            actionRequested = true;
            // Fan form uses Space to leave; other forms use it as action/jump.
            leaveRequested = true;
        }
    }

    private void FixedUpdate()
    {
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

            if (bodyBounds.max.y < minimumHeight ||
                bodyBounds.min.y > activeMaximumHeight)
            {
                KillControlledBody(currentBody);
                return;
            }
        }

        // Give the intentional pop action priority over a pending transfer.
        if (actionRequested &&
            (currentBody.Form == BodyForm.Bubble ||
             currentBody.Form == BodyForm.Spider))
        {
            PopBubble();
            return;
        }

        if (leaveRequested)
        {
            leaveRequested = false;

            if (currentBody.Form == BodyForm.Fan)
            {
                LeaveFan();
                return;
            }
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
                    leaveRequested = false;
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
        pendingSource = null;
        pendingTarget = null;
        horizontalInput = 0f;
        actionRequested = false;
    }

    public void KillControlledBody(PossessableBody hitBody)
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

        isDead = true;
        pendingSource = null;
        pendingTarget = null;
        horizontalInput = 0f;
        actionRequested = false;

        deathRestartTime = Time.unscaledTime + deathDelay;
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

        // Reuse the existing queued pop action.
        // FixedUpdate processes it before a pending possession.
        actionRequested = true;
    }

    private void PopBubble()
    {
        actionRequested = false;

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

        // Loaned spider/bubble returns to its spawn instead of disappearing.
        if (bubble.TryGetComponent(out ReturnToSpawnOnLeave respawner))
        {
            respawner.ReturnToSpawn();
        }
        else
        {
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
        BlockCurrentContacts();

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
        BlockTarget(fan);
        actionRequested = false;
    }

    private void TransferTo(PossessableBody target)
    {
        PossessableBody previousBody = currentBody;

        if (previousBody.TryGetComponent(
            out HexagonSurfaceMovement previousHexagon))
        {
            previousHexagon.Release();
        }

        previousBody.SetOwner(null);

        // 先成功转移控制权。
        currentBody = target;
        currentBody.SetOwner(this);

        horizontalInput = 0f;
        verticalInput = 0f;
        actionRequested = false;
        leaveRequested = false;

        bool starEnteredHost = previousBody == initialBody;
        bool destroyedPreviousBody = false;

        if (starEnteredHost)
        {
            // 星星保留，泡泡爆裂时还能恢复。
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