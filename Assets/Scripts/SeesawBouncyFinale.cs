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
    [SerializeField] private SpriteRenderer beamSkin;
    private Vector2 visibleSize, visibleCenter;
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
    private readonly Sprite[] brokenSprites = new Sprite[2];
    private PossessableBody ignoredControlledBody;
    private Rigidbody2D leftBridgeBody;
    private BoxCollider2D leftBridgeCollider;
    private Vector2 bridgeFrom, bridgeTarget;
    private float bridgeFromAngle, bridgeElapsed;
    public bool IsLeftBridgeReady { get; private set; }
    public int BounceCount { get; private set; }
    public bool HasLifted { get; private set; }
    public bool IsComplete => purpleContact.IsPressed;
    public bool IsBoardCracked => passage.IsCracked;

    private void Awake()
    {
        originalColor = boardVisual.color;
        visibleSize = board.size;
        visibleCenter = board.offset;
        if (beamSkin != null)
        {
            // Beam.png has transparent margins on both sides of its painted face.
            Sprite beam = beamSkin.sprite;
            Vector2 padding = new Vector2(9f, 12f);
            Vector2 low = (padding - beam.pivot) / beam.pixelsPerUnit;
            Vector2 high = (beam.rect.size - padding - beam.pivot) / beam.pixelsPerUnit;
            Vector3 min = board.transform.InverseTransformPoint(beamSkin.transform.TransformPoint(low));
            Vector3 max = board.transform.InverseTransformPoint(beamSkin.transform.TransformPoint(high));
            visibleCenter = (min + max) * .5f;
            visibleSize = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            board.size = visibleSize;
            board.offset = visibleCenter;
            var paint = new GameObject("Seesaw impact paint").AddComponent<SpriteRenderer>();
            paint.transform.SetParent(board.transform, false);
            paint.transform.localPosition = visibleCenter;
            paint.sprite = boardVisual.sprite;
            paint.transform.localScale = new Vector3(visibleSize.x / paint.sprite.bounds.size.x,
                visibleSize.y / paint.sprite.bounds.size.y, 1f);
            paint.sortingLayerID = beamSkin.sortingLayerID;
            paint.sortingOrder = beamSkin.sortingOrder + 1;
            paint.enabled = false;
            boardVisual = paint;
        }
        purpleSensor.enabled = false;
        purpleSensorVisual.enabled = false;
        ability = ball.GetComponent<BouncyBallAbility>();
        ability.CeilingStruck += OnCeilingStruck;
        Rigidbody2D ballBody = ball.GetComponent<Rigidbody2D>();
        ball.gameObject.SetActive(false);
        foreach (Collider2D other in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (other == returnBarrier || other.gameObject.scene != gameObject.scene) continue;
            if (other.attachedRigidbody != ballBody) Physics2D.IgnoreCollision(returnBarrier, other);
        }
    }

    public void RevealBall() => ball.gameObject.SetActive(true);

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
        float target = BounceCount / (float)Mathf.Max(1, requiredImpacts);
        if (colorProgress >= target) return;
        colorProgress = Mathf.MoveTowards(colorProgress, target, Time.deltaTime / (.14f * requiredImpacts));
        // The green is baked into the beam sprite. Overlay paint on the visible beam
        // instead of multiplying its texture by purple or tinting its hidden backing.
        Color paint = beamSkin != null ? ballColor : Color.Lerp(originalColor, ballColor, colorProgress);
        if (beamSkin != null) paint.a *= colorProgress;
        boardVisual.color = paint;
        boardVisual.enabled = true;
        if (colorProgress >= 1f) BreakAndLowerBoard();
    }

    private void OnCeilingStruck(Collision2D collision)
    {
        if (!CanAct || BounceCount >= requiredImpacts || collision.collider != board || !bounceArea.Contains(ball.Body.position)) return;
        BounceCount++;
        passage.SetFinaleImpactProgress(BounceCount / (float)Mathf.Max(1, requiredImpacts));
    }

    private void BreakAndLowerBoard()
    {
        HasLifted = true;
        BreakLeftHalf();
        // Only the painted right-hand board can press the matching floor switch.
        Transform sensor = purpleSensor.transform;
        sensor.SetParent(board.transform, false);
        sensor.localPosition = board.offset;
        sensor.localRotation = Quaternion.identity;
        sensor.localScale = Vector3.one;
        purpleSensor.size = board.size;
        purpleSensor.offset = Vector2.zero;
        purpleSensor.gameObject.SetActive(true);
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
        Transform left = CreateHalf(true, centerOffset, halfWidth);
        CreateHalf(false, centerOffset, halfWidth);
        if (beamSkin != null) beamSkin.enabled = false;
        boardVisual.enabled = false;
        board.offset += Vector2.right * centerOffset;
        board.size = new Vector2(halfWidth, board.size.y);
        passage.HideBoardCrack();

        left.SetParent(transform, true);
        leftBridgeBody = left.gameObject.AddComponent<Rigidbody2D>();
        leftBridgeBody.bodyType = RigidbodyType2D.Kinematic;
        leftBridgeBody.gravityScale = 0f;
        leftBridgeBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        leftBridgeCollider = left.gameObject.AddComponent<BoxCollider2D>();
        leftBridgeCollider.size = new Vector2(halfWidth, board.size.y);
        leftBridgeCollider.enabled = false;
        bridgeFrom = leftBridgeBody.position;
        bridgeFromAngle = leftBridgeBody.rotation;
        float width = halfWidth * Mathf.Abs(left.lossyScale.x);
        float height = board.size.y * Mathf.Abs(left.lossyScale.y);
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

    private Transform CreateHalf(bool left, float centerOffset, float width)
    {
        // Keep an ordinary scene transform for the bridge collider.
        // Only the sprite child is scaled to the artwork's dimensions.
        float sign = left ? -1f : 1f;
        var half = new GameObject(left ? "Seesaw Broken Left Half" : "Seesaw Supported Right Half");
        half.transform.SetParent(board.transform, false);
        half.transform.localPosition = board.offset + Vector2.right * (sign * centerOffset);

        const int columns = 512, rows = 64;
        float scaleX = Mathf.Max(.001f, Mathf.Abs(board.transform.localScale.x));
        float maxOffset = 0f;
        for (int i = 0; i < SeesawOverloadSequence.FractureRows; i++)
            maxOffset = Mathf.Max(maxOffset, Mathf.Abs(SeesawOverloadSequence.FractureOffset(i)));
        float extent = width * .5f + maxOffset / scaleX;
        var pixels = new Color32[columns * rows];
        Color32 face = ballColor;
        Color32 grain = Color.Lerp(ballColor, new Color32(66, 45, 98, 255), .2f);
        Color32 edge = Color.Lerp(ballColor, new Color32(74, 49, 126, 255), .7f);
        for (int y = 0; y < rows; y++)
        {
            float row = (1f - (y + .5f) / rows) * (SeesawOverloadSequence.FractureRows - 1);
            int i = Mathf.FloorToInt(row);
            float seam = -sign * width * .5f + Mathf.Lerp(SeesawOverloadSequence.FractureOffset(i),
                SeesawOverloadSequence.FractureOffset(i + 1), row - i) / scaleX;
            for (int x = 0; x < columns; x++)
            {
                float localX = ((x + .5f) / columns * 2f - 1f) * extent;
                float depth = (localX - seam) * sign;
                if (depth >= 0f && localX * sign <= width * .5f)
                {
                    float grainLine = Mathf.Repeat((y + .5f) / rows + .022f * Mathf.Sin(localX * scaleX), .23f);
                    pixels[y * columns + x] = depth * scaleX < .11f ? edge : grainLine < .024f ? grain : face;
                }
            }
        }
        var texture = new Texture2D(columns, rows, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, columns, rows), new Vector2(.5f, .5f),
            rows, 0, SpriteMeshType.FullRect);
        brokenSprites[left ? 0 : 1] = sprite;
        var skin = new GameObject("Fractured beam face").AddComponent<SpriteRenderer>();
        skin.transform.SetParent(half.transform, false);
        skin.transform.localScale = new Vector3(2f * extent / sprite.bounds.size.x, board.size.y / sprite.bounds.size.y, 1f);
        skin.sprite = sprite;
        skin.sortingLayerID = boardVisual.sortingLayerID;
        skin.sortingOrder = boardVisual.sortingOrder;
        return half.transform;
    }

    private void OnDestroy()
    {
        if (leftBridgeBody != null) Destroy(leftBridgeBody.gameObject);
        foreach (Sprite sprite in brokenSprites)
        {
            if (sprite == null) continue;
            Destroy(sprite.texture);
            Destroy(sprite);
        }
        if (ability == null) return;
        ability.CeilingStruck -= OnCeilingStruck;
    }
}
