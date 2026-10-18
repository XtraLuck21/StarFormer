using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpikeWallEnemyChase))]
public class spikeWallSpawn : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float expressionDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float emergeDuration = 1f;

    [Header("Emergence")]
    [SerializeField, Min(0f)] private float emergeDistance = 2f;

    [Header("Expression")]
    [SerializeField] private SpikeAngryText angryText;

    // Persist across scene reloads; clear when a new play session begins.
    private static bool expressionPlayed;

    private Rigidbody2D body;
    private SpikeWallEnemyChase chase;

    private Vector2 startPosition;
    private float timer;

    private bool sequenceStarted;
    private bool emergenceFinished;
    private bool chaseRequested;
    private bool chaseStarted;

    // 0 = idle, 1 = expression delay, 2 = emerging, 3 = fully emerged.
    private int stage;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        expressionPlayed = false;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        chase = GetComponent<SpikeWallEnemyChase>();
    }

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

        if (!expressionPlayed && angryText != null)
        {
            expressionPlayed = true;
            angryText.ShowAngry();

            stage = expressionDuration > 0f ? 1 : 2;
        }
        else
        {
            // Skip the expression after death or an R-key restart.
            if (angryText != null)
            {
                angryText.HideAngry();
            }

            stage = 2;
        }
    }

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