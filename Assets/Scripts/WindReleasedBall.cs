using UnityEngine;

/// <summary>An ordinary, non-possessable ball. Wind releases it once for its entire lifetime.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class WindReleasedBall : MonoBehaviour
{
    private SeesawRoomSupplies supply;
    private Rigidbody2D body;
    private float fallingGravity;
    private RigidbodyConstraints2D fallingConstraints;
    private ulong armedAfterActivation;
    private bool hasTriggeredSeesaw;
    private SeesawOverloadSequence gust;
    public bool HasBeenReleased { get; private set; }
    public bool IsOwnedBy(SeesawRoomSupplies owner) => supply != null && supply == owner;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        fallingGravity = body.gravityScale;
        fallingConstraints = body.constraints;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.gravityScale = 0f;
    }

    public void Bind(SeesawRoomSupplies owner)
    {
        supply = owner;
        if (supply != null) supply.RegisterBallCollider(GetComponent<CircleCollider2D>());
        armedAfterActivation = FanBlower.LatestBlowActivation;
    }

    public void ReleaseForGust(SeesawOverloadSequence sequence)
    {
        gust = sequence;
        HasBeenReleased = true;
        body.constraints = fallingConstraints;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = fallingGravity;
        body.linearVelocity = new Vector2(-2.5f, 0f);
        body.WakeUp();
    }

    private void OnCollisionEnter2D(Collision2D collision) => ReportLoad(collision);
    private void OnCollisionStay2D(Collision2D collision) => ReportLoad(collision);
    private void ReportLoad(Collision2D collision)
    {
        if (gust != null && collision.collider == gust.BoardSurface) gust.RegisterLoad(this);
    }

    public bool TryUseSeesawTrigger(SeesawRoomSupplies owner)
    {
        if (!HasBeenReleased || hasTriggeredSeesaw || supply == null || supply != owner) return false;
        hasTriggeredSeesaw = true;
        return true;
    }

    public bool TryDespawnOnSpikes(SeesawRoomSupplies owner)
        => TryDespawnOnFall(owner);

    public bool TryDespawnOnFall(SeesawRoomSupplies owner)
    {
        if (!gameObject.activeInHierarchy || !HasBeenReleased || supply == null || supply != owner) return false;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    private void FixedUpdate()
    {
        if (supply == null || !supply.IsRoomActive || Time.timeScale <= 0f) return;
        if (!HasBeenReleased && !supply.FanSupplyStopped && supply.HasFanBlowCommandAfter(armedAfterActivation))
        {
            HasBeenReleased = true;
            body.constraints = fallingConstraints;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = fallingGravity;
            body.WakeUp();
        }
        if (gust != null && gust.IsWindActive)
            body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, -4f,
                12f * Time.fixedDeltaTime), body.linearVelocity.y);
        if (HasBeenReleased && supply.HasBallLeftBoard(body.position)) Destroy(gameObject);
    }
}
