using UnityEngine;

/// <summary>Four underside strikes widen the crack, break off the left half and lower the right into Purple.</summary>
public sealed class SeesawBouncyFinale : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody ball;
    [SerializeField] private SeesawOverloadSequence passage;
    [SerializeField] private SeesawTilt2D seesaw;
    [SerializeField] private BoxCollider2D board;
    [SerializeField] private SpriteRenderer boardVisual;
    [SerializeField] private BoxCollider2D returnBarrier;
    [SerializeField] private BoxCollider2D purpleSensor;
    [SerializeField] private SpriteRenderer purpleSensorVisual;
    [SerializeField] private SeesawColorContact purpleContact;
    [SerializeField] private Color ballColor = new Color32(154, 120, 220, 255);
    [SerializeField] private Rect bounceArea = new Rect(16.7f, -23.4f, 8.4f, 6f);
    [SerializeField, Range(1f, 20f)] private float finalRightDownDegrees = 14f;
    [SerializeField, Min(1)] private int requiredImpacts = 4;
    [SerializeField, Min(0f)] private float fractureGap = .35f;
    [SerializeField] private Collider2D leftCliffEdge;
    [SerializeField] private Collider2D leftCliffLanding;
    [SerializeField, Min(.1f)] private float bridgeSettleSeconds = 1.1f;
    private BouncyBallAbility ability;
    private Color originalColor;
    private float colorProgress;
    private PossessableBody ignoredControlledBody;
    private Rigidbody2D leftBridgeBody;
    private BoxCollider2D leftBridgeCollider;
    private Vector2 bridgeFrom, bridgeTarget;
    private float bridgeFromAngle, bridgeElapsed;
    public bool IsLeftBridgeReady { get; private set; }
    public int BounceCount { get; private set; }
    public bool HasLifted { get; private set; }
    public bool IsComplete => purpleContact.IsPressed;

    private void Awake()
    {
        originalColor = boardVisual.color;
        purpleSensor.enabled = false;
        purpleSensorVisual.enabled = false;
        ability = ball.GetComponent<BouncyBallAbility>();
        ability.CeilingStruck += OnCeilingStruck;
        Rigidbody2D ballBody = ball.GetComponent<Rigidbody2D>();
        foreach (Collider2D other in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (other == returnBarrier || other.gameObject.scene != gameObject.scene) continue;
            if (other.attachedRigidbody != ballBody) Physics2D.IgnoreCollision(returnBarrier, other);
        }
    }

    private bool CanAct => player != null && !player.IsDead && !player.IsRestarting && Time.timeScale > 0f &&
        player.CurrentBody == ball && ball.IsControlled && passage.IsPassageOpen;

    private void FixedUpdate()
    {
        SettleLeftBridge();
        // Reapply after a stored Star is reactivated by Q; disabled colliders can lose ignored pairs.
        PossessableBody current = player.CurrentBody;
        if (current == ball) { ignoredControlledBody = null; return; }
        if (current == null || current == ignoredControlledBody) return;
        foreach (Collider2D collider in current.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(returnBarrier, collider);
        ignoredControlledBody = current;
    }

    private void Update()
    {
        if (player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        colorProgress = Mathf.MoveTowards(colorProgress, BounceCount / (float)Mathf.Max(1, requiredImpacts), Time.deltaTime / .25f);
        boardVisual.color = Color.Lerp(originalColor, ballColor, colorProgress);
    }

    private void OnCeilingStruck(Collision2D collision)
    {
        if (!CanAct || HasLifted || collision.collider != board || !bounceArea.Contains(ball.Body.position)) return;
        BounceCount++;
        passage.SetFinaleImpactProgress(BounceCount / (float)Mathf.Max(1, requiredImpacts));
        if (BounceCount < requiredImpacts) return;
        HasLifted = true;
        colorProgress = 1f;
        boardVisual.color = ballColor;
        BreakLeftHalf();
        purpleSensor.gameObject.SetActive(true);
        purpleSensorVisual.color = ballColor;
        purpleSensorVisual.enabled = true;
        purpleSensor.enabled = true;
        seesaw.LiftLeftFromBall(finalRightDownDegrees);
    }

    private void BreakLeftHalf()
    {
        // Keep the original collider/reference on the supported right half. Children and
        // the purple sensor retain their positions relative to the central pivot.
        float gap = Mathf.Min(board.size.x * .2f,
            fractureGap / Mathf.Max(.001f, Mathf.Abs(board.transform.lossyScale.x)));
        float halfWidth = (board.size.x - gap) * .5f;
        float centerOffset = (board.size.x + gap) * .25f;
        SpriteRenderer left = CreateHalf("Seesaw Broken Left Half", -centerOffset, halfWidth);
        CreateHalf("Seesaw Supported Right Half", centerOffset, halfWidth);
        boardVisual.enabled = false;
        board.offset += Vector2.right * centerOffset;
        board.size = new Vector2(halfWidth, board.size.y);
        passage.HideBoardCrack();

        left.transform.SetParent(null, true);
        leftBridgeBody = left.gameObject.AddComponent<Rigidbody2D>();
        leftBridgeBody.bodyType = RigidbodyType2D.Kinematic;
        leftBridgeBody.gravityScale = 0f;
        leftBridgeBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        leftBridgeCollider = left.gameObject.AddComponent<BoxCollider2D>();
        leftBridgeCollider.size = left.sprite.bounds.size;
        leftBridgeCollider.enabled = false;
        bridgeFrom = leftBridgeBody.position;
        bridgeFromAngle = leftBridgeBody.rotation;
        float width = left.sprite.bounds.size.x * Mathf.Abs(left.transform.lossyScale.x);
        float height = left.sprite.bounds.size.y * Mathf.Abs(left.transform.lossyScale.y);
        // The whole half spans the left cliff with overlap onto both banks.
        bridgeTarget = new Vector2(leftCliffEdge.bounds.max.x - .2f + width * .5f,
            leftCliffLanding.bounds.max.y - height * .5f);
    }

    private void SettleLeftBridge()
    {
        if (leftBridgeBody == null || IsLeftBridgeReady || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        bridgeElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(bridgeElapsed / Mathf.Max(.1f, bridgeSettleSeconds));
        float eased = Mathf.SmoothStep(0f, 1f, t);
        leftBridgeBody.MovePosition(Vector2.Lerp(bridgeFrom, bridgeTarget, eased));
        leftBridgeBody.MoveRotation(Mathf.LerpAngle(bridgeFromAngle, 0f, eased));
        if (t < 1f) return;
        // Snap the final pose before enabling collision, so the bridge never sweeps a rider.
        leftBridgeBody.position = bridgeTarget;
        leftBridgeBody.rotation = 0f;
        leftBridgeBody.linearVelocity = Vector2.zero;
        leftBridgeBody.angularVelocity = 0f;
        leftBridgeCollider.enabled = true;
        IsLeftBridgeReady = true;
    }

    private SpriteRenderer CreateHalf(string name, float x, float width)
    {
        var half = new GameObject(name);
        half.transform.SetParent(board.transform, false);
        half.transform.localPosition = new Vector3(board.offset.x + x, board.offset.y, 0f);
        half.transform.localScale = new Vector3(width / boardVisual.sprite.bounds.size.x,
            board.size.y / boardVisual.sprite.bounds.size.y, 1f);
        var visual = half.AddComponent<SpriteRenderer>();
        visual.sprite = boardVisual.sprite;
        visual.sharedMaterial = boardVisual.sharedMaterial;
        visual.sortingLayerID = boardVisual.sortingLayerID;
        visual.sortingOrder = boardVisual.sortingOrder;
        visual.color = ballColor;
        return visual;
    }

    private void OnDestroy()
    {
        if (ability == null) return;
        ability.CeilingStruck -= OnCeilingStruck;
    }
}
