using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerPossessionController : MonoBehaviour
{
    [SerializeField] private PossessableBody initialBody;
    [SerializeField] private float minimumHeight = -10f;
    [SerializeField] private float maximumHeight = 8f;
    [SerializeField] private float maximumRiseSpeed = 3f;
    [SerializeField] private float deathDelay = 2f;

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
    private bool restartRequested;

    private readonly Dictionary<PossessableBody, Vector3>
        bouncyBallSpawnPositions =
            new Dictionary<PossessableBody, Vector3>();

    private BouncyBallCheckpointSession checkpointSession;

    private bool HasCheckpointInCurrentScene =>
        checkpointSession != null &&
        checkpointSession.HasCheckpoint &&
        checkpointSession.ScenePath == gameObject.scene.path;

    public static void ResetCheckpointSession()
    {
        BouncyBallCheckpointSession.ClearCurrent();
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

        checkpointSession = BouncyBallCheckpointSession.GetOrCreate();
        CacheBouncyBallSpawnPositions();

        if (TryRestoreBouncyBallCheckpoint())
        {
            return;
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

        bool isBubble = currentBody != null &&
            currentBody.Form == BodyForm.Bubble;

        if ((isBubble && keyboard.eKey.wasPressedThisFrame) ||
            (!isBubble && keyboard.spaceKey.wasPressedThisFrame))
        {
            actionRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (currentBody == null || restartRequested || isDead)
        {
            return;
        }

        Bounds bodyBounds = currentBody.BodyCollider.bounds;

        float activeMaximumHeight = currentBody.Form == BodyForm.BouncyBall
            ? ballMaximumHeight
            : maximumHeight;

        if (bodyBounds.max.y < minimumHeight ||
            bodyBounds.min.y > activeMaximumHeight)
        {
            KillControlledBody(currentBody);
            return;
        }

        // Give the intentional pop action priority over a pending transfer.
        if (actionRequested &&
            (currentBody.Form == BodyForm.Bubble ||
             currentBody.Form == BodyForm.Spider))
        {
            PopBubble();
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
        bubble.gameObject.SetActive(false);

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

    private void TransferTo(PossessableBody target)
    {
        PossessableBody previousBody = currentBody;
        if (previousBody.TryGetComponent(
            out HexagonSurfaceMovement previousHexagon))
        {
            previousHexagon.Release();
        }
        previousBody.SetOwner(null);

        bool starEnteredHost = previousBody == initialBody;
        if (starEnteredHost)
        {
            previousBody.gameObject.SetActive(false);
        }

        currentBody = target;
        currentBody.SetOwner(this);

        if (currentBody.Form == BodyForm.BouncyBall)
        {
            ActivateBouncyBallCheckpoint(currentBody);
        }

        if (!starEnteredHost)
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
            }
        }
    }

    private void ActivateBouncyBallCheckpoint(PossessableBody ball)
    {
        if (ball == null ||
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
        checkpointSession.Save(
            gameObject.scene.path,
            ball.name,
            spawnPosition
        );

        Debug.Log($"Bouncy Ball checkpoint activated: {ball.name}.", ball);
    }

    private bool TryRestoreBouncyBallCheckpoint()
    {
        if (!HasCheckpointInCurrentScene ||
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

            ResetCheckpointSession();
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
            source.BodyCollider.IsTouching(target.BodyCollider);
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
