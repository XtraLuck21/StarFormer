using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class level2Camera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody spiderBody;

    // Other camera-control scripts; do not assign this component here.
    [SerializeField] private Behaviour[] normalCameraScripts;

    [Header("Player Area")]
    [SerializeField] private Vector2 yRange = new Vector2(10f, 16f);

    [Header("Area 1 - Follow")]
    [SerializeField]
    private Vector2 firstXRange =
        new Vector2(-90f, -75f);
    [SerializeField]
    private Vector2 firstFollowOffset =
    new Vector2(0f, 0.5f);

    [SerializeField, Min(0.01f)] private float followSize = 2f;
    [SerializeField] private float cameraZ = -10f;

    [Header("Area 2 - Fixed")]
    [SerializeField]
    private Vector2 secondXRange =
        new Vector2(-75f, -64f);

    [SerializeField, Min(0.01f)] private float fixedSize = 6f;
    [SerializeField]
    private Vector3 fixedPosition =
        new Vector3(-70f, 13f, -10f);

    [Header("Spider Cutscene - Once")]
    [SerializeField] private bool useDoorChaseTrigger;
    [SerializeField] private float triggerY = 14.5f;
    [SerializeField, Min(0.01f)] private float cutsceneSize = 2f;
    [SerializeField]
    private Vector3 cutscenePosition =
        new Vector3(-90f, 12f, -10f);

    [SerializeField, Min(0f)]
    private float cutsceneDuration = 3.5f;

    [Header("Chase Framing")]
    // Assign the SpikeAnchor at the leading edge of the spike wall.
    [SerializeField] private Transform spikeWallEnemy;

    [SerializeField] private float chaseStartX = -64f;

    // Leave three world units to the left of the wall and to the right of the player.
    [SerializeField, Min(0f)] private float horizontalMargin = 3f;

    // Extra space behind the wall to show the collapsing floor.
    [SerializeField, Min(0f)] private float chaseRearMargin;

    // Fixed camera height during the chase.
    [SerializeField] private float chaseCameraY = 13f;

    [Header("Collapsing Floor Framing")]
    [SerializeField] private bool frameCollapsingFloor;
    [SerializeField] private SpikeWallEnemyChase chaseWall;
    [SerializeField] private Collider2D chaseFloor;
    [Tooltip("World units visible behind the wall's rear edge.")]
    [SerializeField, Min(0f)] private float visibleCrackTrail = 8f;
    [Tooltip("World units visible below the floor for falling fragments.")]
    [SerializeField, Min(0f)] private float visibleDebrisDepth = 6f;

    [Header("Chase Zoom")]
    // Minimum orthographic size when tightening the chase framing.
    [SerializeField, Min(0.01f)] private float minimumChaseSize = 4f;

    // Speed at which the camera zooms inward.
    [SerializeField, Min(0.01f)] private float zoomSpeed = 3f;

    [Header("Focus Target - After Chase")]
    // Target to focus on after the chase, such as Star.
    // Leave empty to follow the player's currently controlled body.
    [SerializeField] private Transform focusTarget;

    [SerializeField, Min(0.01f)] private float focusSize = 6f;

    // Offset from the target; (0, 0) places the target at the center of the view.
    [SerializeField]
    private Vector2 focusFollowOffset = Vector2.zero;

    // Pan speed toward the target in world units per second.
    [SerializeField, Min(0.01f)] private float focusPanSpeed = 20f;

    // Zoom speed toward the target orthographic size.
    [SerializeField, Min(0.01f)] private float focusZoomSpeed = 4f;

    // Hold duration after reaching the target before returning to normal follow.
    [SerializeField, Min(0f)] private float focusHoldTime = 0.8f;

    // Invoked when the camera arrives; can reveal hint text or trigger other actions.
    [SerializeField]
    private UnityEvent onFocusReached = new UnityEvent();

    private bool focusStarted;
    private bool focusReached;
    private float focusHoldTimer;

    [Header("Dropping Scene")]
    [SerializeField]
    private Vector2 droppingXRange =
    new Vector2(-26f, -16f);

    [SerializeField] private float droppingStartY = 10f;
    [SerializeField] private float droppingStopY = -16f;

    [SerializeField, Min(0.01f)] private float droppingSize = 10f;

    [SerializeField]
    private Vector3 droppingFixedPosition =
        new Vector3(-22f, -16f, -10f);

    [Header("Drop Tension")]
    [SerializeField] private bool tightenDropFraming;
    [SerializeField, Min(0.01f)] private float droppingEndSize = 5.5f;
    [SerializeField, Min(0.01f)] private float droppingZoomSpeed = 8f;
    [SerializeField] private float droppingLookAhead = -1.5f;

    [Header("Bottom Passage")]
    [SerializeField] private bool followBottomPassage;
    [SerializeField] private Vector2 bottomCameraXRange = new Vector2(-22f, -9f);
    [SerializeField] private float bottomCameraY = -21.1f;
    [SerializeField, Min(0.01f)] private float bottomFollowSize = 4f;
    [SerializeField] private float bottomLookAhead = 2f;
    private bool bottomPassageStarted;

    [Header("Seesaw Room")]
    [SerializeField, Min(0.01f)] private float seesawMinimumSize = 8f;
    [SerializeField, Min(0f)] private float seesawHorizontalPadding = 2f;
    [SerializeField] private float seesawCameraYOffset = 2f;
    [SerializeField] private SeesawRoomStage seesawStage;
    private Collider2D seesawSurface;
    private bool seesawRoomStarted;

    private bool droppingStarted;
    private bool droppingFinished;


    private bool chaseCameraStarted;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onEnemyShotStarted =
        new UnityEvent();

    [SerializeField]
    private UnityEvent onChaseStarted =
        new UnityEvent();


    [Header("Remaining Area - Follow Player")]
    [SerializeField] private float remainingFollowSize = 6f;

    [SerializeField]
    private Vector2 remainingFollowOffset =
        new Vector2(0f, 1f);

    private bool remainingFollowStarted;
    private Camera cam;
    private bool cutsceneTriggered;
    private bool playingCutscene;
    private float cutsceneTimer;

    private bool controllingCamera;
    private Vector3 savedPosition;
    private float savedSize;
    private bool[] savedScriptStates;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }
    }

    public void BeginRemainingFollow()
    {
        seesawRoomStarted = false;
        bottomPassageStarted = false;
        remainingFollowStarted = true;
        playingCutscene = false;
        focusStarted = false;
        chaseCameraStarted = false;
    }

    // Called at chase end: exit chase framing and pan to Focus Target.
    // Connect the spike wall's On Chase Ended event to this method.
    public void FocusOnTarget()
    {
        chaseCameraStarted = false;
        playingCutscene = false;

        focusStarted = true;
        focusReached = false;
        focusHoldTimer = 0f;
    }

    // Temporarily override the focus target.
    public void FocusOn(Transform target)
    {
        focusTarget = target;
        FocusOnTarget();
    }

    // Exit chase framing only; let the region rules choose the next camera behavior.
    public void EndChaseCamera()
    {
        chaseCameraStarted = false;
    }

    // Door-driven levels keep the player in view while the enemy emerges.
    public void BeginDoorChase()
    {
        if (!useDoorChaseTrigger || cutsceneTriggered || remainingFollowStarted)
        {
            return;
        }

        cutsceneTriggered = true;
        chaseCameraStarted = true;
        onEnemyShotStarted.Invoke();
        onChaseStarted.Invoke();
    }

    public void BeginTrapdoorDrop()
    {
        playingCutscene = false;
        focusStarted = false;
        chaseCameraStarted = false;
        droppingStarted = true;
    }

    public void BeginDarkPassage()
    {
        if (!followBottomPassage)
        {
            return;
        }
        bottomPassageStarted = true;
        playingCutscene = false;
        focusStarted = false;
        chaseCameraStarted = false;
    }

    public void RestoreDarkPassage()
    {
        BeginDarkPassage();
        if (!bottomPassageStarted || player == null || player.CurrentBody == null) return;
        TakeCamera();
        Vector3 position = player.CurrentBody.transform.position;
        SetCamera(new Vector3(Mathf.Clamp(position.x + bottomLookAhead,
            bottomCameraXRange.x, bottomCameraXRange.y), bottomCameraY, cameraZ), bottomFollowSize);
    }

    public void BeginSeesawRoom(Collider2D surface)
    {
        if (surface == null)
        {
            return;
        }
        seesawSurface = surface;
        seesawRoomStarted = true;
        droppingStarted = false;
        droppingFinished = false;
        bottomPassageStarted = false;
        playingCutscene = false;
        focusStarted = false;
        chaseCameraStarted = false;
        remainingFollowStarted = true;
        TakeCamera();
        // The door covers this cut, so the view never pans through the space
        // between the two separate rooms.
        FrameSeesawRoom(surface.bounds.center);
    }

    private void FrameSeesawRoom(Vector3 playerPosition)
    {
        if (seesawStage != null)
        {
            seesawStage.GetCameraFrame(cam, cameraZ, out Vector3 stagePosition, out float stageSize);
            SetCamera(stagePosition, stageSize);
            return;
        }
        Bounds bounds = seesawSurface.bounds;
        Vector3 center = new Vector3(bounds.center.x,
            bounds.center.y + seesawCameraYOffset, cameraZ);
        float size = Mathf.Max(seesawMinimumSize,
            (bounds.size.x + seesawHorizontalPadding * 2f) /
                (2f * Mathf.Max(cam.aspect, 0.01f)));
        size = Mathf.Max(size, Mathf.Abs(playerPosition.y - center.y) + 2f);
        SetCamera(center, size);
    }

    private void LateUpdate()
    {
        if (playingCutscene)
        {
            SetCamera(cutscenePosition, cutsceneSize);

            cutsceneTimer += Time.deltaTime;

            if (cutsceneTimer < cutsceneDuration)
            {
                return;
            }

            playingCutscene = false;
            ApplyPlayerCamera();
            onChaseStarted.Invoke();
            return;
        }

        // Chase ending: pan to Star, with priority just below the cutscene.
        if (focusStarted && !remainingFollowStarted)
        {
            ApplyFocusCamera();
            return;
        }

        ApplyPlayerCamera();
    }

    private void ApplyFocusCamera()
    {
        TakeCamera();

        // Fall back to the controlled body when no target is assigned.
        Vector3 targetPosition;

        if (focusTarget != null)
        {
            targetPosition = new Vector3(
                focusTarget.position.x,
                focusTarget.position.y,
                cameraZ
            );
        }
        else
        {
            PossessableBody currentBody =
                player != null ? player.CurrentBody : null;

            if (currentBody == null)
            {
                return;
            }

            targetPosition = new Vector3(
                currentBody.transform.position.x,
                currentBody.transform.position.y,
                cameraZ
            );
        }

        targetPosition.x += focusFollowOffset.x;
        targetPosition.y += focusFollowOffset.y;

        // Once caught up, stay attached so a faster-moving target cannot leave the camera behind.
        if (focusReached)
        {
            SetCamera(targetPosition, focusSize);

            // Hold briefly, then restore normal camera following.
            // Following continues to work after possession of Fan, Bubble, or Box.
            focusHoldTimer += Time.deltaTime;

            if (focusHoldTimer >= focusHoldTime)
            {
                focusStarted = false;
                remainingFollowStarted = true;
            }

            return;
        }

        // Smoothly pan toward the target before arrival instead of cutting instantly.
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            focusPanSpeed * Time.deltaTime
        );

        float nextSize = Mathf.MoveTowards(
            cam.orthographicSize,
            focusSize,
            focusZoomSpeed * Time.deltaTime
        );

        SetCamera(nextPosition, nextSize);

        // Notify listeners once both position and zoom have reached their targets.
        if (!focusReached &&
            Vector3.Distance(nextPosition, targetPosition) < 0.01f &&
            Mathf.Abs(nextSize - focusSize) < 0.01f)
        {
            focusReached = true;
            focusHoldTimer = 0f;
            onFocusReached.Invoke();
        }
    }

    private void ApplyPlayerCamera()
    {
        PossessableBody currentBody =
            player != null ? player.CurrentBody : null;

        if (currentBody == null)
        {
            ReleaseCamera();
            return;
        }

        Vector3 position = currentBody.transform.position;

        if (seesawRoomStarted && seesawSurface != null)
        {
            Bounds bounds = seesawSurface.bounds;
            bool insideRoom = seesawStage != null ? seesawStage.ContainsPosition(position) :
                position.x >= bounds.min.x - 3f && position.x <= bounds.max.x + 3f &&
                position.y <= bounds.max.y + 12f && position.y >= bounds.min.y - 5f;
            if (insideRoom)
            {
                TakeCamera();
                FrameSeesawRoom(position);
                return;
            }
            seesawRoomStarted = false;
        }

        if (bottomPassageStarted)
        {
            TakeCamera();
            Vector3 target = new Vector3(Mathf.Clamp(position.x + bottomLookAhead,
                bottomCameraXRange.x, bottomCameraXRange.y), bottomCameraY, cameraZ);
            SetCamera(Vector3.Lerp(transform.position, target,
                1f - Mathf.Exp(-8f * Time.deltaTime)),
                Mathf.MoveTowards(cam.orthographicSize, bottomFollowSize,
                    3f * Time.deltaTime));
            return;
        }

        // Falling-shot logic takes priority over the final follow phase,
        // allowing the post-chase drop sequence to play normally.
        // Take camera control when the player enters the drop region.
        // Once triggered, continue the falling shot even outside this X range.
        if (!droppingStarted &&
            InRange(position.x, droppingXRange) &&
            position.y <= droppingStartY)
        {
            droppingStarted = true;
        }

        if (droppingStarted)
        {
            TakeCamera();

            // Lock at the bottom; jumping again must not restore following.
            if (position.y <= droppingStopY)
            {
                droppingFinished = true;
            }

            if (droppingFinished)
            {
                SetCamera(droppingFixedPosition,
                    tightenDropFraming ? droppingEndSize : droppingSize);
            }
            else
            {
                float size = droppingSize;
                if (tightenDropFraming)
                {
                    float progress = Mathf.InverseLerp(
                        droppingStartY, droppingStopY, position.y);
                    float targetSize = Mathf.SmoothStep(
                        droppingSize, droppingEndSize, progress);
                    size = Mathf.MoveTowards(cam.orthographicSize,
                        targetSize, droppingZoomSpeed * Time.deltaTime);
                }

                SetCamera(
                    new Vector3(position.x,
                        position.y + (tightenDropFraming ? droppingLookAhead : 0f),
                        cameraZ),
                    size
                );
            }

            return;
        }

        // Final follow: track the player's currently controlled body,
        // including Fan, Bubble, and Box after possession changes.
        if (remainingFollowStarted)
        {
            TakeCamera();

            SetCamera(
                new Vector3(
                    position.x + remainingFollowOffset.x,
                    position.y + remainingFollowOffset.y,
                    cameraZ
                ),
                remainingFollowSize
            );

            return;
        }

        bool insideY = InRange(position.y, yRange);

        bool insideSecond =
            insideY && InRange(position.x, secondXRange);

        // At X = -75, use the second region.
        bool insideFirst =
            insideY &&
            InRange(position.x, firstXRange) &&
            !insideSecond;

        // Show the enemy cutscene when Spider first reaches the specified height.
        if (!useDoorChaseTrigger && !cutsceneTriggered &&
            spiderBody != null &&
            currentBody == spiderBody &&
            insideSecond &&
            position.y >= triggerY)
        {
            TakeCamera();

            cutsceneTriggered = true;
            playingCutscene = true;
            cutsceneTimer = 0f;

            SetCamera(cutscenePosition, cutsceneSize);
            onEnemyShotStarted.Invoke();
            return;
        }

        // After the cutscene, start chase framing when the player passes X = -64.
        // Do not restart a chase that has already finished.
        if (!useDoorChaseTrigger && cutsceneTriggered &&
            !playingCutscene &&
            !focusStarted &&
            position.x > chaseStartX)
        {
            chaseCameraStarted = true;
        }

        if (chaseCameraStarted)
        {
            TakeCamera();
            FollowDuringChase(position);
        }
        else if (insideSecond)
        {
            TakeCamera();
            SetCamera(fixedPosition, fixedSize);
        }
        else if (insideFirst)
        {
            TakeCamera();

            SetCamera(
                new Vector3(
                    position.x + firstFollowOffset.x,
                    position.y + firstFollowOffset.y,
                    cameraZ
                ),
                followSize
            );
        }
        else
        {
            ReleaseCamera();
        }
    }


    private bool InRange(float value, Vector2 range)
    {
        return value >= Mathf.Min(range.x, range.y) &&
               value <= Mathf.Max(range.x, range.y);
    }

    private void SetCamera(Vector3 position, float size)
    {
        transform.position = position;
        cam.orthographicSize = size;
    }

    private void TakeCamera()
    {
        if (controllingCamera)
        {
            return;
        }

        savedPosition = transform.position;
        savedSize = cam.orthographicSize;

        int count = normalCameraScripts != null
            ? normalCameraScripts.Length
            : 0;

        savedScriptStates = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Behaviour script = normalCameraScripts[i];

            if (script == null || script == this)
            {
                continue;
            }

            savedScriptStates[i] = script.enabled;
            script.enabled = false;
        }

        controllingCamera = true;
    }


    private void FollowDuringChase(Vector3 playerPosition)
    {
        if (frameCollapsingFloor && chaseWall != null && chaseFloor != null)
        {
            FollowCollapsingFloorChase(playerPosition);
            return;
        }

        if (spikeWallEnemy == null)
        {
            SetCamera(
                new Vector3(playerPosition.x, chaseCameraY, cameraZ),
                Mathf.Max(followSize, minimumChaseSize)
            );
            return;
        }

        // spikeWallEnemy references the SpikeAnchor at the wall's leading edge.
        float left = spikeWallEnemy.position.x - horizontalMargin - chaseRearMargin;
        float right = playerPosition.x + horizontalMargin;

        float centerX = (left + right) * 0.5f;
        float width = Mathf.Abs(right - left);

        // Orthographic size is half the view height.
        float requiredSize = width / (2f * Mathf.Max(cam.aspect, 0.01f));

        // Allow vertical space for the player while jumping.
        float requiredHeight =
            Mathf.Abs(playerPosition.y - chaseCameraY) + 2f;

        float targetSize = Mathf.Max(
            minimumChaseSize,
            Mathf.Max(requiredSize, requiredHeight)
        );

        // Zoom out immediately to keep both targets visible; tighten the view smoothly.
        float nextSize = targetSize >= cam.orthographicSize
            ? targetSize
            : Mathf.MoveTowards(
                cam.orthographicSize,
                targetSize,
                zoomSpeed * Time.deltaTime
            );

        SetCamera(
            new Vector3(centerX, chaseCameraY, cameraZ),
            nextSize
        );
    }

    private void FollowCollapsingFloorChase(Vector3 playerPosition)
    {
        // Keep the latest broken slabs inside the view after the wall passes them.
        float left = Mathf.Min(chaseWall.RearEdgeX - visibleCrackTrail,
            playerPosition.x - horizontalMargin);
        float right = Mathf.Max(chaseWall.FrontEdgeX + horizontalMargin,
            playerPosition.x + horizontalMargin);

        // Frame actual floor height rather than centering above it. Include the
        // falling debris as well as the player's full jump and the spike wall.
        float bottom = chaseFloor.bounds.max.y - visibleDebrisDepth;
        float top = Mathf.Max(playerPosition.y + 2.5f, chaseCameraY + 3f);
        float targetSize = Mathf.Max(minimumChaseSize,
            Mathf.Max((top - bottom) * 0.5f,
                (right - left) / (2f * Mathf.Max(cam.aspect, 0.01f))));
        float nextSize = targetSize >= cam.orthographicSize
            ? targetSize
            : Mathf.MoveTowards(cam.orthographicSize, targetSize,
                zoomSpeed * Time.deltaTime);

        SetCamera(new Vector3((left + right) * 0.5f,
            (bottom + top) * 0.5f, cameraZ), nextSize);
    }

    private void ReleaseCamera()
    {
        if (!controllingCamera)
        {
            return;
        }

        SetCamera(savedPosition, savedSize);

        if (normalCameraScripts != null &&
            savedScriptStates != null)
        {
            int count = Mathf.Min(
                normalCameraScripts.Length,
                savedScriptStates.Length
            );

            for (int i = 0; i < count; i++)
            {
                Behaviour script = normalCameraScripts[i];

                if (script != null && script != this)
                {
                    script.enabled = savedScriptStates[i];
                }
            }
        }

        controllingCamera = false;
    }

    private void OnDisable()
    {
        playingCutscene = false;
        ReleaseCamera();
    }
}
