using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class HintZone : MonoBehaviour
{
    [Header("References")]
    // 要淡入淡出的那段文字。
    [SerializeField] private TMP_Text hintText;

    [Header("Behaviour")]
    // 勾上：离开区域后文字淡出。
    // 不勾：显示出来就一直留着。
    [SerializeField] private bool hideOnExit = true;

    // 勾上：只触发一次，之后再进来也不显示。
    [SerializeField] private bool showOnlyOnce = false;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInTime = 0.4f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.3f;

    private Collider2D zoneCollider;
    private bool isInside;
    private bool hasShown;
    private float alpha;

    private void Start()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;

        if (hintText == null)
        {
            Debug.LogError(
                "请把提示文字拖进 Inspector 的 Hint Text 栏。",
                this
            );

            enabled = false;
            return;
        }

        // 开局先藏起来。
        alpha = 0f;
        hintText.alpha = 0f;
    }

    private void Update()
    {
        bool shouldShow = isInside || (!hideOnExit && hasShown);

        float target = shouldShow ? 1f : 0f;
        float duration = shouldShow ? fadeInTime : fadeOutTime;

        if (Mathf.Approximately(alpha, target))
        {
            return;
        }

        // 用 unscaledDeltaTime，这样死亡定格时文字也能正常淡出。
        float step = duration > 0f
            ? Time.unscaledDeltaTime / duration
            : 1f;

        alpha = Mathf.MoveTowards(alpha, target, step);
        hintText.alpha = alpha;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (showOnlyOnce && hasShown)
        {
            return;
        }

        isInside = true;
        hasShown = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            isInside = false;
        }
    }

    // 只认当前被玩家附身的那个身体。
    private bool IsPlayer(Collider2D other)
    {
        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null)
        {
            return false;
        }

        return otherBody.TryGetComponent(
                   out PossessableBody possessableBody) &&
               possessableBody.IsControlled;
    }
}
