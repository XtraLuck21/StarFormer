using UnityEngine;
using UnityEngine.Events;

// 只负责倒计时，时间到了就让自己消失。
// 不处理杀伤、不处理碎裂，也不需要 Rigidbody2D。
// 杀伤由父物体的 SpikeWallEnemyChase 统一接管。
public class SpikeLifetime : MonoBehaviour
{
    [Header("Start")]
    // 勾上 = 关卡加载就开始倒计时。
    // 不勾 = 等外部调用 Begin() 才开始，比如追逐开始的那一刻。
    [SerializeField] private bool countFromStart = false;

    // 存活秒数。
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    [Header("Expire")]
    // 勾上 = 只关掉物体（之后还能用 SetActive 恢复）。
    // 不勾 = 直接销毁。
    [SerializeField] private bool disableInsteadOfDestroy = false;

    // 消失前执行，可以在这里接音效等等。
    [SerializeField] private UnityEvent onExpired = new UnityEvent();

    private bool counting;
    private bool expired;
    private float age;

    // 给别的脚本或 UI 读的状态。
    public bool IsCounting => counting;
    public bool IsExpired => expired;

    // 剩余秒数。还没开始计时就返回完整时长。
    public float Remaining =>
        counting ? Mathf.Max(0f, lifetime - age) : lifetime;

    // 0 到 1 的进度，方便接进度条。
    public float Progress =>
        counting ? Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime)) : 0f;

    private void Start()
    {
        if (countFromStart)
        {
            Begin();
        }
    }

    // 从现在开始倒计时，用 Inspector 里填好的秒数。
    public void Begin()
    {
        if (expired)
        {
            return;
        }

        counting = true;
        age = 0f;
    }

    // 从现在开始倒计时，并临时指定秒数。
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

    // 取消倒计时，已经走过的时间作废。
    public void Cancel()
    {
        counting = false;
        age = 0f;
    }

    // 延长剩余时间，传负数就是缩短。
    public void AddTime(float seconds)
    {
        if (expired)
        {
            return;
        }

        age = Mathf.Max(0f, age - seconds);
    }

    // 不等倒计时，立刻消失。
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