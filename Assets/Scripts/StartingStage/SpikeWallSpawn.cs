using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpikeWallEnemyChase))]
public class spikeWallSpawn : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float expressionDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float emergeDuration = 1f;

    [Header("Emergence")]
    // 从初始位置向右移动多少距离。
    [SerializeField, Min(0f)] private float emergeDistance = 2f;

    [Header("Expression")]
    [SerializeField] private SpikeAngryText angryText;

    private Rigidbody2D body;
    private SpikeWallEnemyChase chase;

    private Vector2 startPosition;
    private float timer;

    private bool sequenceStarted;
    private bool emergenceFinished;
    private bool chaseRequested;
    private bool chaseStarted;

    // 0 = 静止，1 = 显示表情，2 = 出墙，3 = 出墙完成。
    private int stage;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        chase = GetComponent<SpikeWallEnemyChase>();
    }

    // 连接镜头的 On Enemy Shot Started。
    public void BeginEmergence()
    {
        if (sequenceStarted)
        {
            return;
        }

        sequenceStarted = true;
        chase.StopChase();

        startPosition = body.position;
        timer = 0f;
        stage = 1;

        if (angryText != null)
        {
            angryText.ShowAngry();
        }
    }

    // 连接镜头的 On Chase Started。
    // 如果还没出墙完成，就先记住请求。
    public void RequestChase()
    {
        chaseRequested = true;
        TryStartChase();
    }

    private void FixedUpdate()
    {
        if (stage == 1)
        {
            timer += Time.fixedDeltaTime;

            if (timer >= expressionDuration)
            {
                timer = 0f;
                stage = 2;
            }

            return;
        }

        if (stage == 2)
        {
            timer += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(
                timer / Mathf.Max(0.01f, emergeDuration)
            );

            float easedProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            body.MovePosition(
                startPosition +
                Vector2.right * emergeDistance * easedProgress
            );

            if (progress >= 1f)
            {
                // 下一次物理更新再交给追逐脚本。
                stage = 3;
            }

            return;
        }

        if (stage == 3 && !emergenceFinished)
        {
            emergenceFinished = true;
            TryStartChase();
        }
    }

    private void TryStartChase()
    {
        if (!emergenceFinished ||
            !chaseRequested ||
            chaseStarted)
        {
            return;
        }

        chaseStarted = true;

        if (angryText != null)
        {
            angryText.HideAngry();
        }

        chase.StartChase();
    }
}