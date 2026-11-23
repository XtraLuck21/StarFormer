using UnityEngine;

/// <summary>Four physical ratchet strikes connect Purple and unfold a separate approach bridge.</summary>
public sealed class SeesawBouncyFinale : MonoBehaviour
{
    [SerializeField] private SeesawMachineRoom machine;
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private PossessableBody ball;
    [SerializeField] private SeesawOverloadSequence passage;
    [SerializeField] private BoxCollider2D board;
    [SerializeField] private SpriteRenderer beamSkin;
    [SerializeField] private BoxCollider2D returnBarrier;
    [SerializeField] private BoxCollider2D purpleSensor;
    [SerializeField] private SpriteRenderer purpleSensorVisual;
    [SerializeField] private SeesawColorContact purpleContact;
    [SerializeField] private Rect bounceArea = new Rect(16.7f, -23.4f, 8.4f, 6f);
    [SerializeField, Min(1)] private int requiredImpacts = 4;
    // Used by the room authoring tool to size the hinged bridge to its two banks.
    [SerializeField] private Collider2D leftCliffEdge;
    [SerializeField] private Collider2D leftCliffLanding;
    private BouncyBallAbility ability;
    private PossessableBody ignoredControlledBody;
    public bool IsLeftBridgeReady => machine != null && machine.IsBridgeReady;
    public int BounceCount { get; private set; }
    public bool HasLifted { get; private set; }
    public bool IsComplete => purpleContact.IsPressed;
    public bool IsYellowActivated => passage.HasStarted;

    private void Awake()
    {
        if (beamSkin != null)
        {
            // Match the original beam art's painted face, excluding transparent margins.
            Sprite beam = beamSkin.sprite;
            Vector2 padding = new Vector2(9f, 12f);
            Vector2 low = (padding - beam.pivot) / beam.pixelsPerUnit;
            Vector2 high = (beam.rect.size - padding - beam.pivot) / beam.pixelsPerUnit;
            Vector3 min = board.transform.InverseTransformPoint(beamSkin.transform.TransformPoint(low));
            Vector3 max = board.transform.InverseTransformPoint(beamSkin.transform.TransformPoint(high));
            board.offset = (min + max) * .5f;
            board.size = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
        }
        purpleSensor.enabled = false;
        purpleSensorVisual.enabled = false;
        ability = ball.GetComponent<BouncyBallAbility>();
        ability.CeilingStruck += OnCeilingStruck;
        Rigidbody2D ballBody = ball.Body;
        foreach (Collider2D other in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (other == returnBarrier || other.gameObject.scene != gameObject.scene) continue;
            if (other.attachedRigidbody != ballBody) Physics2D.IgnoreCollision(returnBarrier, other);
        }
    }

    private void FixedUpdate()
    {
        // Reactivating the stored Star can lose ignored pairs; keep this exception room-local.
        PossessableBody current = player.CurrentBody;
        if (current == ball) { ignoredControlledBody = null; return; }
        if (current == null || current == ignoredControlledBody) return;
        foreach (Collider2D collider in current.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(returnBarrier, collider);
        ignoredControlledBody = current;
    }

    private void OnCeilingStruck(Collision2D collision)
    {
        if (player.IsDead || player.IsRestarting || Time.timeScale <= 0f || player.CurrentBody != ball ||
            !ball.IsControlled || !passage.IsPassageOpen || BounceCount >= requiredImpacts ||
            collision.collider != machine.ImpactSurface || !bounceArea.Contains(ball.Body.position)) return;
        BounceCount++;
    }

    private void Update()
    {
        if (HasLifted || BounceCount < requiredImpacts || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        HasLifted = true;
        machine.BeginBridge();
        // The ratchet closes this drive only after real piston collisions. The seesaw
        // beam and its collider remain whole throughout the final stage.
        purpleContact.gameObject.SetActive(true);
        purpleContact.RestorePressed();
    }

    private void OnDestroy()
    {
        if (ability != null) ability.CeilingStruck -= OnCeilingStruck;
    }
}
