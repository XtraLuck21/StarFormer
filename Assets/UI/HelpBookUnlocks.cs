using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录帮助书里哪些页已经解锁。
///
/// 只在本次游戏期间有效：数据存在内存里，不写硬盘。
/// 关掉游戏重新打开，所有页都回到锁定状态，玩家要重新发现一遍。
/// 关卡之间切换场景不会清零，因为 static 字段跨场景保留。
///
/// 这是个 static 类（静态类）：不需要挂在任何物体上，任何脚本直接
/// HelpBookUnlocks.Unlock("Box") 就能调用。
/// </summary>
public static class HelpBookUnlocks
{
    // HashSet 是一个「不重复的集合」，查找某个元素在不在里面非常快。
    // 比 List 适合这种「只关心有没有」的场景。
    private static readonly HashSet<string> unlocked = new HashSet<string>();

    /// <summary>
    /// 每次进入 Play 模式前清空。
    ///
    /// static 字段在 Unity 编辑器里不会随着停止播放自动重置，
    /// 不加这个的话，编辑器里测试时上一次的解锁记录会残留，
    /// 表现得像还在存盘。打包后的游戏每次启动本来就是干净的。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLaunch()
    {
        unlocked.Clear();
    }

    /// <summary>这一页解锁了吗。id 为空视为「不需要解锁」，直接返回 true。</summary>
    public static bool IsUnlocked(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return true;
        }

        return unlocked.Contains(id);
    }

    /// <summary>解锁一页。重复调用无害。</summary>
    public static void Unlock(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        // Add 在元素已存在时返回 false，正好用来判断这次是不是新解锁。
        if (unlocked.Add(id))
        {
            Debug.Log($"帮助书解锁了新的一页：{id}");
        }
    }

    /// <summary>手动清空所有解锁记录。可以接到「重新开始」之类的按钮上。</summary>
    public static void ResetAll()
    {
        unlocked.Clear();
    }
}