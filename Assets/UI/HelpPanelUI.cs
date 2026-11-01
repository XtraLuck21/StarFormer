using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 右上角 Help 按钮 + 键位说明面板。
///
/// 用法：
///   1. 把本脚本挂在 Canvas 上（Add Component -> Help Panel UI）。
///   2. 把 HelpButton / HelpPanel / CloseButton 拖进下面对应的槽。
///   3. HelpPanel 在场景里保持关闭或打开都可以，Awake 会强制关掉。
///
/// 主菜单：Pause While Open 保持关闭（主菜单没有在跑的游戏，不需要暂停）。
/// 关卡内：只有在确认本场景没有别的系统在改 Time.timeScale 时，才打开 Pause While Open。
///         StarFormer 的 RestartMenuUI 会自己操作 timeScale，两个系统同时改会互相覆盖。
///         详见随附的《HelpPanel 搭建步骤.md》第 7 节。
/// </summary>
public class HelpPanelUI : MonoBehaviour
{
    [Header("必填引用")]
    [Tooltip("右上角的 ? / Help 按钮。")]
    [SerializeField] private Button helpButton;

    [Tooltip("整个弹窗的根物体（包含遮罩和键位图）。Awake 时会被强制关闭。")]
    [SerializeField] private GameObject helpPanel;

    [Header("选填引用")]
    [Tooltip("弹窗里的 × 关闭按钮。")]
    [SerializeField] private Button closeButton;

    [Tooltip("遮罩上的 Button 组件。点空白处也能关闭弹窗，玩家会下意识这么做。")]
    [SerializeField] private Button backdropButton;

    [Header("行为设置")]
    [Tooltip("按 Esc 或 H 关闭弹窗。")]
    [SerializeField] private bool closeWithKeyboard = true;

    [Tooltip("打开弹窗时把游戏暂停（Time.timeScale = 0）。\n" +
             "主菜单：保持关闭。\n" +
             "关卡内：确认本场景没有别的系统在改 timeScale 之后再打开。")]
    [SerializeField] private bool pauseWhileOpen;

    // 记录打开前的时间流速，关闭时原样还回去，而不是硬写成 1f。
    // 硬写 1f 会在"别的系统已经暂停了游戏"时把它意外恢复。
    private float timeScaleBeforeOpen = 1f;

    private bool isOpen;

    /// <summary>弹窗当前是否打开。其他脚本可以读这个值来避开输入冲突。</summary>
    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (helpPanel == null || helpButton == null)
        {
            Debug.LogError(
                "HelpPanelUI 缺少引用：请在 Inspector 里把 Help Button 和 Help Panel 拖进来。",
                this
            );

            enabled = false;
            return;
        }

        // 用代码绑定，而不是在 Inspector 的 OnClick 列表里拖。
        // 好处：重命名方法时编译器会报错，不会像 Inspector 绑定那样静默失效。
        helpButton.onClick.AddListener(Open);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (backdropButton != null)
        {
            backdropButton.onClick.AddListener(Close);
        }

        // 不依赖场景里有没有记得取消勾选，开局一律关闭。
        helpPanel.SetActive(false);
        isOpen = false;
    }

    private void Update()
    {
        // 弹窗没开就什么都不做，省掉每帧的键盘查询。
        if (!isOpen || !closeWithKeyboard)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    /// <summary>打开键位说明弹窗。可以直接挂到别的按钮的 OnClick 上。</summary>
    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        helpPanel.SetActive(true);

        if (pauseWhileOpen)
        {
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    /// <summary>关闭键位说明弹窗。</summary>
    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        helpPanel.SetActive(false);

        if (pauseWhileOpen)
        {
            Time.timeScale = timeScaleBeforeOpen;
        }
    }

    /// <summary>开关弹窗。想做成"按 H 开、再按 H 关"时用这个。</summary>
    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void OnDisable()
    {
        // 场景切换时如果弹窗还开着并且暂停了游戏，必须把 timeScale 还回去，
        // 否则下一个场景会以 timeScale = 0 开局，表现为"游戏卡死、什么都不动"。
        if (isOpen && pauseWhileOpen)
        {
            Time.timeScale = timeScaleBeforeOpen;
        }

        isOpen = false;
    }

    private void OnDestroy()
    {
        if (helpButton != null)
        {
            helpButton.onClick.RemoveListener(Open);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        if (backdropButton != null)
        {
            backdropButton.onClick.RemoveListener(Close);
        }
    }
}