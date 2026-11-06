using UnityEngine;

/// <summary>
/// Shape Closet 里的一个格子。根据该形态是否已解锁，在「锁定外观」和
/// 「解锁后的图标」之间切换。
///
/// 用法：
///   1. 把本脚本挂在每个 Slot 物体上（Slot_01_Box、Slot_02_Bubble…）。
///   2. Unlock Id 填形态名，大小写必须和 BodyForm 枚举完全一致：
///      Box / Bubble / BouncingBall / Spider / Fan
///   3. Locked Visuals 里拖入锁定时要显示的子物体
///      （Locked silhouette 和 Question mark）。
///   4. Unlocked Visual 拖入解锁后要显示的图标子物体。
///
/// 解锁状态由 HelpBookUnlocks 统一管理，和帮助书共用同一份记录，
/// 不会出现「书里解锁了、衣橱里还锁着」的不一致。
/// </summary>
public class ShapeClosetSlot : MonoBehaviour
{
    [Header("这个格子代表哪个形态")]
    [Tooltip("形态名，大小写必须和 BodyForm 枚举完全一致：\n" +
             "Box / Bubble / BouncingBall / Spider / Fan")]
    [SerializeField] private string unlockId;

    [Header("外观")]
    [Tooltip("锁定时显示的子物体，比如 Locked silhouette 和 Question mark。")]
    [SerializeField] private GameObject[] lockedVisuals;

    [Tooltip("解锁后显示的图标子物体。")]
    [SerializeField] private GameObject unlockedVisual;

    // 记录上一帧的状态，只有变化时才动 SetActive，避免每帧白做无用功。
    private bool lastUnlocked;
    private bool hasCachedState;

    private void OnEnable()
    {
        // 从关卡回到导航场景时，这里会重新评估一次。
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

    /// <summary>重新检查解锁状态并更新外观。</summary>
    public void Refresh(bool force)
    {
        if (string.IsNullOrWhiteSpace(unlockId))
        {
            return;
        }

        bool unlocked = HelpBookUnlocks.IsUnlocked(unlockId);

        if (!force && hasCachedState && unlocked == lastUnlocked)
        {
            return;
        }

        lastUnlocked = unlocked;
        hasCachedState = true;

        if (lockedVisuals != null)
        {
            foreach (GameObject visual in lockedVisuals)
            {
                SetActive(visual, !unlocked);
            }
        }

        SetActive(unlockedVisual, unlocked);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}