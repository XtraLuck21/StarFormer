using UnityEngine;

/// <summary>Three real local bounces dye the board; a subsequent underside strike lifts it into Purple.</summary>
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
    private BouncyBallAbility ability;
    private Color originalColor;
    private float colorProgress;
    private PossessableBody ignoredControlledBody;
    public int BounceCount { get; private set; }
    public bool HasLifted { get; private set; }
    public bool IsComplete => purpleContact.IsPressed;

    private void Awake()
    {
        originalColor = boardVisual.color;
        purpleSensor.enabled = false;
        purpleSensorVisual.enabled = false;
        ability = ball.GetComponent<BouncyBallAbility>();
        ability.Bounced += OnBounced;
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
        // Reapply after a stored Star is reactivated by Q; disabled colliders can lose ignored pairs.
        PossessableBody current = player.CurrentBody;
        if (current == ball) { ignoredControlledBody = null; return; }
        if (current == null || current == ignoredControlledBody) return;
        foreach (Collider2D collider in current.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(returnBarrier, collider);
        ignoredControlledBody = current;
    }

    private void OnBounced()
    {
        if (!CanAct || HasLifted || BounceCount >= 3 || !bounceArea.Contains(ball.Body.position)) return;
        BounceCount++;
    }

    private void Update()
    {
        if (player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        colorProgress = Mathf.MoveTowards(colorProgress, BounceCount / 3f, Time.deltaTime / .25f);
        boardVisual.color = Color.Lerp(originalColor, ballColor, colorProgress);
    }

    private void OnCeilingStruck(Collision2D collision)
    {
        if (!CanAct || HasLifted || BounceCount < 3 || collision.collider != board) return;
        HasLifted = true;
        colorProgress = 1f;
        boardVisual.color = ballColor;
        purpleSensor.gameObject.SetActive(true);
        purpleSensorVisual.color = ballColor;
        purpleSensorVisual.enabled = true;
        purpleSensor.enabled = true;
        seesaw.LiftLeftFromBall(finalRightDownDegrees);
    }

    private void OnDestroy()
    {
        if (ability == null) return;
        ability.Bounced -= OnBounced;
        ability.CeilingStruck -= OnCeilingStruck;
    }
}
