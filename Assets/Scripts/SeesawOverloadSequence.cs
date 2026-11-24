using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The yellow gear's three-ball gust and Spider entrance
/// to the short underpass.
/// </summary>
public sealed class SeesawOverloadSequence : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawRoomStage stage;
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private BoxCollider2D board;
    [SerializeField] private Transform[] gears;
    [SerializeField] private SeesawGear2D yellowGear;
    private bool gustRequested;
    [SerializeField] private BoxCollider2D entrance;
    [SerializeField] private BoxCollider2D exit;
    [SerializeField] private SeesawGateShatter entranceShatter;
    [SerializeField] private SeesawGateShatter exitShatter;

    [SerializeField, Min(.1f)]
    private float ballInterval = .75f;

    [Header("Entrance Reminder")]
    [SerializeField, Min(0f)]
    private float entranceHintDelay = 5f;

    [SerializeField]
    private Vector3 entranceHintOffset =
        new Vector3(0f, 1.5f, 0f);

    [SerializeField, Min(.1f)]
    private float entranceHintFontSize = 16f;


    private readonly HashSet<WindReleasedBall> loaded =
        new HashSet<WindReleasedBall>();

    private LineRenderer boardCrack;
    private LineRenderer entranceCrack;

    private readonly List<(LineRenderer Edge, LineRenderer Highlight)> wind =
        new List<(LineRenderer, LineRenderer)>();

    private Vector3[] boardCrackPoints;
    // Horizontal offsets along the same top-to-bottom seam used by the two broken faces.
    private static readonly float[] FractureOffsets = { -.22f, .21f, -.10f, .36f, -.25f, .12f, -.18f };
    internal static int FractureRows => FractureOffsets.Length;
    internal static float FractureOffset(int row) => FractureOffsets[row];
    private float finaleImpactProgress;
    private bool boardBroken;

    private float elapsed;
    private float crackProgress;
    private float breakProgress;
    private bool breaking;

    private TextMeshPro entranceHint;
    private float entranceHintAge;

    public bool HasStarted { get; private set; }

    public bool IsWindActive =>
        HasStarted && (elapsed < 7.5f || LoadedBallCount < 3);

    public int ReleasedBallCount { get; private set; }
    public int LoadedBallCount => loaded.Count;
    public bool IsCracked { get; private set; }
    public bool IsPassageOpen { get; private set; }
    public Collider2D BoardSurface => board;

    public void SetFinaleImpactProgress(float progress)
    {
        finaleImpactProgress = Mathf.Clamp01(progress);
    }

    public void HideBoardCrack()
    {
        boardBroken = true;
        if (boardCrack != null) boardCrack.enabled = false;
    }

    public void BeginYellowGust()
    {
        if (gustRequested || HasStarted ||
            !stage.IsUnlocked ||
            stage.IsRevealing ||
            player.IsDead ||
            player.IsRestarting)
        {
            return;
        }

        gustRequested = true;
    }

    private void StartGust()
    {
        gustRequested = false;
        HasStarted = true;
        supplies.BeginGustBatch();
        seesaw.HoldLeftForOverload();
        BuildVisuals();
    }

    public void RegisterLoad(WindReleasedBall ball)
    {
        if (!HasStarted ||
            ball == null ||
            !ball.IsOwnedBy(supplies) ||
            !loaded.Add(ball))
        {
            return;
        }

        if (LoadedBallCount == 3)
        {
            IsCracked = true;
            seesaw.HoldLeftForOverload(true);
        }
    }

    private void Update()
    {
        if (player.IsDead ||
            player.IsRestarting ||
            Time.timeScale <= 0f)
        {
            return;
        }

        if (gustRequested && (yellowGear == null || !yellowGear.WaitingForAssemblyTurn)) StartGust();
        if (!HasStarted) return;
        elapsed += Time.deltaTime;

        if (ReleasedBallCount < 3 &&
            elapsed >= .6f + ReleasedBallCount * ballInterval)
        {
            WindReleasedBall ball =
                supplies.TakeGustBall(ReleasedBallCount == 0);

            ball.ReleaseForGust(this);
            ReleasedBallCount++;
        }

        AnimateWind();
        const float purpleRevealSeconds = .8f;
        if (elapsed - Time.deltaTime < purpleRevealSeconds)
            stage.RevealPurpleButton(Mathf.Clamp01(elapsed / purpleRevealSeconds));

        if (!IsCracked)
            return;

        crackProgress = Mathf.Min(
            1f,
            crackProgress + Time.deltaTime
        );

        boardCrack.enabled = !boardBroken;
        boardCrack.widthMultiplier = Mathf.Lerp(.09f, .24f, finaleImpactProgress);
        entranceCrack.enabled = !IsPassageOpen;

        int count = Mathf.Clamp(
            Mathf.CeilToInt(
                crackProgress * (boardCrackPoints.Length - 1)
            ) + 1,
            2,
            boardCrackPoints.Length
        );

        boardCrack.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            Vector3 point = boardCrackPoints[i];
            point.x += FractureOffset(i) * finaleImpactProgress * .4f;
            boardCrack.SetPosition(i, point);
        }

        if (IsPassageOpen)
        {
            HideEntranceHint();
            return;
        }

        // Keep the original Spider entrance condition.
        var spider = player.CurrentBody;

        if (!breaking &&
            crackProgress >= 1f &&
            spider != null &&
            spider.IsControlled &&
            spider.Form == BodyForm.Spider &&
            spider.Body != null &&
            spider.BodyCollider != null &&
            Vector2.Distance(
                spider.Body.position,
                entrance.ClosestPoint(spider.Body.position)
            ) < spider.BodyCollider.bounds.extents.y + .3f)
        {
            breaking = true;
        }

        UpdateEntranceHint();

        if (!breaking)
            return;

        breakProgress = Mathf.Min(
            1f,
            breakProgress + Time.deltaTime / .7f
        );

        entranceShatter.SetProgress(breakProgress);

        if (breakProgress >= 1f)
        {
            entrance.enabled = false;
            entranceCrack.enabled = false;
            IsPassageOpen = true;

            HideEntranceHint();
        }
    }

    public void RevealTunnelExit(float progress)
    {
        if (!exit.enabled) return;
        exitShatter.SetProgress(progress);
        if (progress >= 1f) exit.enabled = false;
    }

    private void CreateEntranceHint()
    {
        if (entranceHint != null)
            return;

        GameObject hintObject =
            new GameObject("Spider Entrance Reminder");

        hintObject.transform.SetParent(transform, false);

        entranceHint = hintObject.AddComponent<TextMeshPro>();
        entranceHint.fontStyle = FontStyles.Bold;
        entranceHint.outlineColor =
            new Color32(0, 0, 0, 255);
        entranceHint.outlineWidth = .15f;
        entranceHint.raycastTarget = false;

        ApplyEntranceArrow();

        MeshRenderer hintRenderer =
            hintObject.GetComponent<MeshRenderer>();

        SpriteRenderer boardRenderer =
            board.GetComponent<SpriteRenderer>();

        if (boardRenderer != null)
        {
            hintRenderer.sortingLayerID =
                boardRenderer.sortingLayerID;
        }

        hintRenderer.sortingOrder = 30;

        entranceHint.enabled = false;
        entranceHintAge = 0f;
    }

    private void ApplyEntranceArrow()
    {
        entranceHint.text = "↓";
        entranceHint.color = Color.yellow;
        entranceHint.fontSize = Mathf.Max(16f, entranceHintFontSize);
        entranceHint.alignment = TextAlignmentOptions.Center;
        entranceHint.rectTransform.sizeDelta = new Vector2(3f, 3f);
    }

    private void OnEnable()
    {
        // Script reloads can retain the old runtime text and Inspector values.
        if (entranceHint == null)
        {
            Transform oldHint = transform.Find("Spider Entrance Reminder");
            if (oldHint != null) entranceHint = oldHint.GetComponent<TextMeshPro>();
        }
        if (entranceHint != null) ApplyEntranceArrow();
    }

    private void UpdateEntranceHint()
    {
        if (entranceHint == null)
            return;

        if (breaking ||
            IsPassageOpen ||
            entranceCrack == null ||
            !entranceCrack.enabled ||
            !entranceCrack.gameObject.activeInHierarchy)
        {
            HideEntranceHint();
            return;
        }

        entranceHintAge += Time.deltaTime;

        // Follow the actual entrance's position.
        Vector3 hintPosition = new Vector3(
            entrance.bounds.center.x,
            entrance.bounds.max.y,
            entrance.transform.position.z
        );

        Vector3 floatingOffset = entranceHintOffset;
        float visibleAge = Mathf.Max(0f, entranceHintAge - entranceHintDelay);
        floatingOffset.y = Mathf.Max(1.5f, floatingOffset.y) + Mathf.Sin(visibleAge * Mathf.PI) * .15f;
        entranceHint.transform.position = hintPosition + floatingOffset;

        entranceHint.enabled =
            entranceHintAge >= entranceHintDelay;
    }

    private void HideEntranceHint()
    {
        if (entranceHint != null)
            entranceHint.enabled = false;
    }

    private void OnDisable()
    {
        HideEntranceHint();
    }

    private LineRenderer Line(
        string name,
        Transform parent,
        Color color,
        float width,
        int count)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var line = go.AddComponent<LineRenderer>();
        var renderer = board.GetComponent<SpriteRenderer>();

        line.sharedMaterial = renderer.sharedMaterial;
        line.sortingLayerID = renderer.sortingLayerID;
        line.sortingOrder = 12;
        line.useWorldSpace = false;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.positionCount = count;

        return line;
    }

    private void BuildVisuals()
    {
        // Keep the crack rotating with the board.
        boardCrack = Line(
            "Overloaded Board Crack",
            board.transform.parent,
            new Color(.22f, .09f, .04f),
            .09f,
            9
        );

        boardCrackPoints = new Vector3[FractureOffsets.Length];
        Vector3 center = board.transform.localPosition;

        for (int i = 0; i < boardCrackPoints.Length; i++)
        {
            boardCrackPoints[i] = center + new Vector3(
                FractureOffset(i) * .6f,
                board.size.y *
                Mathf.Abs(board.transform.localScale.y) *
                (.5f - i / (float)(boardCrackPoints.Length - 1)),
                0f
            );
        }

        boardCrack.enabled = false;

        entranceCrack = Line(
            "Spider Entrance Crack",
            transform,
            new Color(.95f, .7f, .32f),
            .045f,
            9
        );

        for (int i = 0; i < 9; i++)
        {
            entranceCrack.SetPosition(
                i,
                transform.InverseTransformPoint(
                    new Vector3(
                        Mathf.Lerp(
                            entrance.bounds.min.x + .08f,
                            entrance.bounds.max.x - .08f,
                            i / 8f
                        ),
                        entrance.bounds.max.y + .025f +
                        (i % 2 == 0 ? .025f : -.045f),
                        0f
                    )
                )
            );
        }

        entranceCrack.enabled = false;

        CreateEntranceHint();

        var taper = new AnimationCurve(
            new Keyframe(0f, .2f),
            new Keyframe(.32f, 1f),
            new Keyframe(1f, 0f)
        );
        for (int i = 0; i < 24; i++)
        {
            // A dark edge reads over the sky; the light center stays visible over the beam.
            var edge = Line("Leftward Gear Gust Edge", transform, new Color(.16f, .36f, .43f, .72f), .13f, 3);
            var highlight = Line("Leftward Gear Gust Highlight", transform, new Color(.83f, .96f, 1f, .92f), .065f, 3);
            edge.widthCurve = highlight.widthCurve = taper;
            highlight.sortingOrder = edge.sortingOrder + 1;
            wind.Add((edge, highlight));
        }
    }

    private void AnimateWind()
    {
        bool active = IsWindActive;
        for (int i = 0; i < wind.Count; i++)
        {
            var (edge, highlight) = wind[i];
            edge.enabled = highlight.enabled = active;

            if (!active)
                continue;

            Vector3 point;

            if (i < 9)
            {
                var gear = gears[i / 3];

                point = gear.position + new Vector3(
                    -.8f - Mathf.Repeat(
                        elapsed * 4f + i * .65f,
                        2.5f
                    ),
                    (i % 3 - 1) * .35f,
                    0f
                );
            }
            else
            {
                point = new Vector3(
                    42f - Mathf.Repeat(
                        elapsed * 7f + i * 2.3f,
                        28f
                    ),
                    -16f - (i % 5) * .7f,
                    0f
                );
            }

            for (int j = 0; j < 3; j++)
            {
                Vector3 position = transform.InverseTransformPoint(
                    point + new Vector3(
                        j * .45f,
                        Mathf.Sin(elapsed * 4f + i + j) * .06f,
                        0f
                    )
                );
                edge.SetPosition(j, position);
                highlight.SetPosition(j, position);
            }
        }
    }
}
