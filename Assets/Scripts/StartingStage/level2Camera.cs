using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class level2Camera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody spiderBody;

    // 其他控制镜头的脚本，不要填本脚本。
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
    [SerializeField] private float triggerY = 14.5f;
    [SerializeField, Min(0.01f)] private float cutsceneSize = 2f;
    [SerializeField]
    private Vector3 cutscenePosition =
        new Vector3(-90f, 12f, -10f);

    [SerializeField, Min(0f)]
    private float cutsceneDuration = 3.5f;

    [Header("Chase Framing")]
    // 拖入刺墙前沿的 SpikeAnchor。
    [SerializeField] private Transform spikeWallEnemy;

    [SerializeField] private float chaseStartX = -64f;

    // 刺墙左边、玩家右边各留 3 格。
    [SerializeField, Min(0f)] private float horizontalMargin = 3f;

    // 追逐镜头固定高度。
    [SerializeField] private float chaseCameraY = 13f;

    [Header("Chase Zoom")]
    // 镜头收紧时的最低 Size。
    [SerializeField, Min(0.01f)] private float minimumChaseSize = 4f;

    // 镜头缩小的速度。
    [SerializeField, Min(0.01f)] private float zoomSpeed = 3f;

    [Header("Focus Target - After Chase")]
    // 追逐结束后要对准的东西，比如那颗星星。
    // 留空就改为跟随玩家当前控制的身体。
    [SerializeField] private Transform focusTarget;

    [SerializeField, Min(0.01f)] private float focusSize = 6f;

    // 镜头相对目标的偏移。(0, 0) = 目标正好在画面正中。
    [SerializeField]
    private Vector2 focusFollowOffset = Vector2.zero;

    // 镜头平移到目标的速度，单位是米/秒。
    [SerializeField, Min(0.01f)] private float focusPanSpeed = 20f;

    // 镜头缩放到目标 Size 的速度。
    [SerializeField, Min(0.01f)] private float focusZoomSpeed = 4f;

    // 镜头到位后停留几秒，然后把控制权交还给普通跟随。
    [SerializeField, Min(0f)] private float focusHoldTime = 0.8f;

    // 镜头到位后执行，可以接「显示提示文字」之类。
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
        remainingFollowStarted = true;
        playingCutscene = false;
        focusStarted = false;
        chaseCameraStarted = false;
    }

    // 追逐结束时调用：退出追逐镜头，平移到 Focus Target。
    // 把刺墙的 On Chase Ended 事件连到这个方法。
    public void FocusOnTarget()
    {
        chaseCameraStarted = false;
        playingCutscene = false;

        focusStarted = true;
        focusReached = false;
        focusHoldTimer = 0f;
    }

    // 临时指定一个目标。
    public void FocusOn(Transform target)
    {
        focusTarget = target;
        FocusOnTarget();
    }

    // 只退出追逐镜头，不做别的，镜头按区域规则自己决定。
    public void EndChaseCamera()
    {
        chaseCameraStarted = false;
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

        // 追逐收尾：平移到星星，优先级仅次于分镜。
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

        // 没填目标就退化成跟随玩家，避免卡死在原地。
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

        // 已经追上目标后直接贴住，否则目标跑得比镜头快会一直甩在后面。
        if (focusReached)
        {
            SetCamera(targetPosition, focusSize);

            // 停留一会儿，然后把镜头交还给普通跟随。
            // 之后附身到风扇、泡泡、箱子都能正常跟随。
            focusHoldTimer += Time.deltaTime;

            if (focusHoldTimer >= focusHoldTime)
            {
                focusStarted = false;
                remainingFollowStarted = true;
            }

            return;
        }

        // 还没追上时平滑移过去，而不是瞬间切过去。
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

        // 位置和缩放都到位了，通知一次。
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

        // 下落分镜排在收尾跟随之前，
        // 这样追逐结束之后的下落段落仍然能正常播放。
        // 进入下落区域后，接管镜头。
        // 触发后即使玩家离开这个 X 范围，也继续播放下落镜头。
        if (!droppingStarted &&
            InRange(position.x, droppingXRange) &&
            position.y <= droppingStartY)
        {
            droppingStarted = true;
        }

        if (droppingStarted)
        {
            TakeCamera();

            // 到达底部后锁定，不会因玩家再次跳起而恢复跟随。
            if (position.y <= droppingStopY)
            {
                droppingFinished = true;
            }

            if (droppingFinished)
            {
                SetCamera(droppingFixedPosition, droppingSize);
            }
            else
            {
                SetCamera(
                    new Vector3(position.x, position.y, cameraZ),
                    droppingSize
                );
            }

            return;
        }

        // 收尾跟随：跟着玩家当前控制的身体，
        // 附身风扇、泡泡、箱子都适用。
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

        // X = -75 时使用第二个区域。
        bool insideFirst =
            insideY &&
            InRange(position.x, firstXRange) &&
            !insideSecond;

        // Spider 首次达到指定高度，播放敌人分镜。
        if (!cutsceneTriggered &&
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

        // 分镜结束后，玩家超过 -64，启动追逐镜头。
        // 追逐已经收尾过就不再重新启动。
        if (cutsceneTriggered &&
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
        if (spikeWallEnemy == null)
        {
            SetCamera(
                new Vector3(playerPosition.x, chaseCameraY, cameraZ),
                Mathf.Max(followSize, minimumChaseSize)
            );
            return;
        }

        // spikeWallEnemy 引用的是刺墙前沿的 SpikeAnchor。
        float left = spikeWallEnemy.position.x - horizontalMargin;
        float right = playerPosition.x + horizontalMargin;

        float centerX = (left + right) * 0.5f;
        float width = Mathf.Abs(right - left);

        // Orthographic Size 是画面高度的一半。
        float requiredSize = width / (2f * Mathf.Max(cam.aspect, 0.01f));

        // 玩家跳跃时，也为玩家高度留出空间。
        float requiredHeight =
            Mathf.Abs(playerPosition.y - chaseCameraY) + 2f;

        float targetSize = Mathf.Max(
            minimumChaseSize,
            Mathf.Max(requiredSize, requiredHeight)
        );

        // 放大时立即保证两者入镜，缩小时平滑收紧。
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