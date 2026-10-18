using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// 踩到之后回到主菜单。
// 挂在终点旗子上，需要一个勾了 Is Trigger 的 Collider2D。
[RequireComponent(typeof(Collider2D))]
public class LevelGoalToMenu : MonoBehaviour
{
    [Header("References")]
    // 留空会自动在场景里找。
    [SerializeField] private PlayerPossessionController player;

    [Header("Required Gate")]
    // 必须先处理掉的门。留空 = 不做任何限制，踩到就通关。
    [SerializeField] private GameObject requiredGate;

    // 勾上：门被销毁或关闭才算通过。
    // 不勾：门还在才算通过（极少用，按需要改）。
    [SerializeField] private bool gateMustBeGone = true;

    [Header("Status Text")]
    [SerializeField] private TMP_Text statusText;

    [SerializeField, TextArea]
    private string clearedMessage = "关卡完成！";

    [SerializeField, TextArea]
    private string blockedMessage = "还有门没打开。";

    // 条件不满足时，提示文字显示几秒后消失。
    [SerializeField, Min(0f)] private float blockedMessageTime = 2f;

    [Header("Return To Menu")]
    // 主菜单场景的名字，必须和 Build Settings 里一致。
    [SerializeField] private string menuSceneName = "MainMenu";

    // 显示完成提示后，等几秒再切场景。
    [SerializeField, Min(0f)] private float returnDelay = 1.5f;

    // 切场景之前执行，可以接音效、存档等等。
    [SerializeField] private UnityEvent onLevelCleared = new UnityEvent();

    private bool cleared;
    private Coroutine blockedRoutine;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void CheckPlayer(Collider2D other)
    {
        if (cleared)
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null ||
            !otherBody.TryGetComponent(out PossessableBody body) ||
            !body.IsControlled)
        {
            return;
        }

        if (!IsGateSatisfied())
        {
            ShowBlockedMessage();
            return;
        }

        Clear();
    }

    // 门的条件是否满足。留空就永远算满足。
    private bool IsGateSatisfied()
    {
        if (requiredGate == null)
        {
            return true;
        }

        // 物体被销毁后，这里的引用会变成 null。
        bool gateGone = !requiredGate.activeInHierarchy;

        return gateMustBeGone ? gateGone : !gateGone;
    }

    private void ShowBlockedMessage()
    {
        if (statusText == null || blockedRoutine != null)
        {
            return;
        }

        blockedRoutine = StartCoroutine(BlockedRoutine());
    }

    private IEnumerator BlockedRoutine()
    {
        statusText.text = blockedMessage;

        yield return new WaitForSeconds(blockedMessageTime);

        if (!cleared)
        {
            statusText.text = string.Empty;
        }

        blockedRoutine = null;
    }

    private void Clear()
    {
        cleared = true;
        AnalyticsManager.Instance?.TrackLevelComplete(
            player != null ? player.CurrentBody : null
        );

        if (blockedRoutine != null)
        {
            StopCoroutine(blockedRoutine);
            blockedRoutine = null;
        }

        if (statusText != null)
        {
            statusText.text = clearedMessage;
        }

        onLevelCleared.Invoke();

        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        // 用 Realtime，万一有脚本把 Time.timeScale 设成 0 也能走完。
        yield return new WaitForSecondsRealtime(returnDelay);

        // 切场景前恢复时间流速，否则主菜单会是静止的。
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("没有填主菜单场景名。", this);
            yield break;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
