using UnityEngine;

/// <summary>Plays a short impact sound when an obstacle hits solid scenery.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public sealed class ObstacleImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip impactClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.6f;
    [SerializeField, Min(0.1f)] private float fullVolumeSpeed = 6f;
    [SerializeField, Min(0f)] private float minimumInterval = 0.12f;

    private AudioSource source;
    private Rigidbody2D body;
    private float nextPlayTime;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        body = GetComponent<Rigidbody2D>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        // Warm the sample before the first collision, including when a different clip is assigned later.
        if (impactClip != null && impactClip.loadState == AudioDataLoadState.Unloaded) impactClip.LoadAudioData();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActiveAndEnabled || impactClip == null || Time.time < nextPlayTime) return;

        // The floor, walls and kinematic seesaw count as scenery. Other balls and player forms do not.
        Rigidbody2D other = collision.rigidbody;
        if (other != null && (other.bodyType == RigidbodyType2D.Dynamic ||
            other.TryGetComponent<PossessableBody>(out _))) return;

        float speed = 0f;
        float normalImpulse = 0f;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            speed = Mathf.Max(speed, Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal)));
            normalImpulse += contact.normalImpulse;
        }
        // The solver can already have stopped the ball when the callback arrives.
        // Normal impulse still captures the landing strength without counting sideways sliding.
        speed = Mathf.Max(speed, normalImpulse / Mathf.Max(0.001f, body.mass));
        if (speed < minimumImpactSpeed) return;

        float strength = Mathf.InverseLerp(minimumImpactSpeed, Mathf.Max(minimumImpactSpeed + 0.01f, fullVolumeSpeed), speed);
        source.PlayOneShot(impactClip, volume * Mathf.Lerp(0.3f, 1f, strength));
        nextPlayTime = Time.time + minimumInterval;
    }
}
