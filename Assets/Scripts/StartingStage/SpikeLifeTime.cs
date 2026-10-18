using UnityEngine;
using UnityEngine.Events;

// Count down and remove this object when its lifetime expires.
// Does not handle damage or shattering and does not require Rigidbody2D.
// The parent SpikeWallEnemyChase handles damage.
public class SpikeLifetime : MonoBehaviour
{
    [Header("Start")]
    // Enabled: start the countdown when the level loads.
    // Disabled: wait for an external Begin() call, such as when the chase starts.
    [SerializeField] private bool countFromStart = false;

    // Lifetime in seconds.
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    [Header("Expire")]
    // Enabled: deactivate the object so SetActive can restore it later.
    // Disabled: destroy the object.
    [SerializeField] private bool disableInsteadOfDestroy = false;

    // Invoked before removal; can trigger audio or other effects.
    [SerializeField] private UnityEvent onExpired = new UnityEvent();

    private bool counting;
    private bool expired;
    private float age;

    // State exposed to other scripts or UI.
    public bool IsCounting => counting;
    public bool IsExpired => expired;

    // Remaining seconds; returns the full duration before the countdown starts.
    public float Remaining =>
        counting ? Mathf.Max(0f, lifetime - age) : lifetime;

    // Progress from 0 to 1, suitable for a progress bar.
    public float Progress =>
        counting ? Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime)) : 0f;

    private void Start()
    {
        if (countFromStart)
        {
            Begin();
        }
    }

    // Start counting down using the duration configured in the Inspector.
    public void Begin()
    {
        if (expired)
        {
            return;
        }

        counting = true;
        age = 0f;
    }

    // Start counting down with a temporary duration override.
    public void Begin(float seconds)
    {
        if (expired)
        {
            return;
        }

        lifetime = Mathf.Max(0.1f, seconds);
        counting = true;
        age = 0f;
    }

    // Cancel the countdown and discard elapsed time.
    public void Cancel()
    {
        counting = false;
        age = 0f;
    }

    // Extend the remaining duration; a negative value shortens it.
    public void AddTime(float seconds)
    {
        if (expired)
        {
            return;
        }

        age = Mathf.Max(0f, age - seconds);
    }

    // Remove immediately without waiting for the countdown.
    public void ExpireNow()
    {
        Expire();
    }

    private void Update()
    {
        if (!counting || expired)
        {
            return;
        }

        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Expire();
        }
    }

    private void Expire()
    {
        if (expired)
        {
            return;
        }

        expired = true;
        counting = false;

        onExpired.Invoke();

        if (disableInsteadOfDestroy)
        {
            gameObject.SetActive(false);
            return;
        }

        Destroy(gameObject);
    }
}