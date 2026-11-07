using System.Collections.Generic;
using UnityEngine;

/// <summary>The yellow gear's three-ball gust and Spider entrance to the short underpass.</summary>
public sealed class SeesawOverloadSequence : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawRoomStage stage;
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private BoxCollider2D board;
    [SerializeField] private Transform[] gears;
    [SerializeField] private BoxCollider2D entrance;
    [SerializeField] private BoxCollider2D exit;
    [SerializeField] private SeesawGateShatter entranceShatter;
    [SerializeField] private SeesawGateShatter exitShatter;
    [SerializeField, Min(.1f)] private float ballInterval = .75f;
    private readonly HashSet<WindReleasedBall> loaded = new HashSet<WindReleasedBall>();
    private LineRenderer boardCrack, entranceCrack;
    private readonly List<LineRenderer> wind = new List<LineRenderer>();
    private Vector3[] boardCrackPoints;
    private float elapsed, crackProgress, breakProgress;
    private bool breaking;
    public bool HasStarted { get; private set; }
    public bool IsWindActive => HasStarted && (elapsed < 7.5f || LoadedBallCount < 3);
    public int ReleasedBallCount { get; private set; }
    public int LoadedBallCount => loaded.Count;
    public bool IsCracked { get; private set; }
    public bool IsPassageOpen { get; private set; }
    public Collider2D BoardSurface => board;

    public void BeginYellowGust()
    {
        if (HasStarted || !stage.IsUnlocked || stage.IsRevealing || player.IsDead || player.IsRestarting) return;
        HasStarted = true;
        supplies.BeginGustBatch();
        seesaw.HoldLeftForOverload();
        BuildVisuals();
    }

    public void RegisterLoad(WindReleasedBall ball)
    {
        if (!HasStarted || ball == null || !ball.IsOwnedBy(supplies) || !loaded.Add(ball)) return;
        if (LoadedBallCount == 3)
        {
            IsCracked = true;
            seesaw.HoldLeftForOverload(true);
        }
    }

    private void Update()
    {
        if (!HasStarted || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        elapsed += Time.deltaTime;
        if (ReleasedBallCount < 3 && elapsed >= .6f + ReleasedBallCount * ballInterval)
        {
            WindReleasedBall ball = supplies.TakeGustBall(ReleasedBallCount == 0);
            ball.ReleaseForGust(this);
            ReleasedBallCount++;
        }
        AnimateWind();
        if (!IsCracked) return;
        crackProgress = Mathf.Min(1f, crackProgress + Time.deltaTime);
        boardCrack.enabled = true;
        entranceCrack.enabled = !IsPassageOpen;
        int count = Mathf.Clamp(Mathf.CeilToInt(crackProgress * (boardCrackPoints.Length - 1)) + 1, 2, boardCrackPoints.Length);
        boardCrack.positionCount = count;
        for (int i = 0; i < count; i++) boardCrack.SetPosition(i, boardCrackPoints[i]);
        if (IsPassageOpen) return;
        var spider = player.CurrentBody;
        if (!breaking && crackProgress >= 1f && spider != null && spider.IsControlled && spider.Form == BodyForm.Spider &&
            spider.BodyCollider != null && Vector2.Distance(spider.Body.position, entrance.ClosestPoint(spider.Body.position)) <
            spider.BodyCollider.bounds.extents.y + .3f)
            breaking = true;
        if (!breaking) return;
        breakProgress = Mathf.Min(1f, breakProgress + Time.deltaTime / .7f);
        entranceShatter.SetProgress(breakProgress);
        exitShatter.SetProgress(breakProgress);
        if (breakProgress >= 1f)
        {
            entrance.enabled = exit.enabled = false;
            entranceCrack.enabled = false;
            IsPassageOpen = true;
        }
    }

    private LineRenderer Line(string name, Transform parent, Color color, float width, int count)
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
        // Parent to the unscaled pivot so the crack rotates with the board without stretching its width.
        boardCrack = Line("Overloaded Board Crack", board.transform.parent, new Color(.22f,.09f,.04f), .055f, 9);
        boardCrackPoints = new Vector3[9];
        Vector3 center = board.transform.localPosition;
        for (int i = 0; i < boardCrackPoints.Length; i++)
            boardCrackPoints[i] = center + new Vector3((i % 2 == 0 ? -.12f : .14f),
                board.size.y * Mathf.Abs(board.transform.localScale.y) * (.5f - i / 8f), 0f);
        boardCrack.enabled = false;
        entranceCrack = Line("Spider Entrance Crack", transform, new Color(.95f,.7f,.32f), .045f, 9);
        for (int i = 0; i < 9; i++)
            entranceCrack.SetPosition(i, transform.InverseTransformPoint(new Vector3(
                Mathf.Lerp(entrance.bounds.min.x + .08f, entrance.bounds.max.x - .08f, i / 8f),
                entrance.bounds.max.y + .025f + (i % 2 == 0 ? .025f : -.045f), 0f)));
        entranceCrack.enabled = false;
        for (int i = 0; i < 24; i++)
            wind.Add(Line("Leftward Gear Gust", transform, new Color(.65f,.87f,1f,.3f), .035f, 3));
    }

    private void AnimateWind()
    {
        for (int i = 0; i < wind.Count; i++)
        {
            LineRenderer line = wind[i];
            line.enabled = IsWindActive;
            if (!line.enabled) continue;
            Vector3 point;
            if (i < 9)
            {
                var gear = gears[i / 3];
                point = gear.position + new Vector3(-.8f - Mathf.Repeat(elapsed * 4f + i * .65f, 2.5f), (i % 3 - 1) * .35f, 0f);
            }
            else point = new Vector3(42f - Mathf.Repeat(elapsed * 7f + i * 2.3f, 28f),
                -16f - (i % 5) * .7f, 0f);
            for (int j = 0; j < 3; j++)
                line.SetPosition(j, transform.InverseTransformPoint(point + new Vector3(j * .45f,
                    Mathf.Sin(elapsed * 4f + i + j) * .06f, 0f)));
        }
    }
}
