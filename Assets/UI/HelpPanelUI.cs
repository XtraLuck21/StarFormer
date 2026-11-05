using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 翻书式帮助面板：一页一个形态，每页显示标题、键位说明图、以及一段循环演示动画。
/// 替代原来的 HelpPanelUI（开关逻辑都保留了，可以直接换掉）。
///
/// 场景结构：
///   Canvas
///   ├── HelpButton
///   └── HelpPanel                （遮罩，默认关闭）
///       ├── BookFrame            （书页背景）
///       │   ├── PageTitle        （TextMeshProUGUI）
///       │   ├── ControlsImage    （Image，显示该形态的键位）
///       │   ├── DemoImage        （Image + SpriteSequencePlayer，演示动画）
///       │   └── PageCaption      （TextMeshProUGUI，一句话说明，选填）
///       ├── PrevButton
///       ├── NextButton
///       ├── PageIndicator        （TextMeshProUGUI，显示 "3 / 8"）
///       └── CloseButton
///
/// 注意：只需要一套页面控件，翻页时换的是内容，不是换 GameObject。
/// </summary>
public class HelpBookUI : MonoBehaviour
{
    /// <summary>
    /// 一页的内容。在 Inspector 的 Pages 列表里逐页填。
    /// </summary>
    [Serializable]
    public class HelpPage
    {
        [Tooltip("页面标题，比如 STAR / BOX / BUBBLE。")]
        public string title = "STAR";

        [Tooltip("该形态的图标。可以用 Sprite Editor 从原来那张八宫格大图里切出来。")]
        public Sprite icon;

        [Tooltip("这一页的按键说明，比如「A D 移动 / 按住 J 缩小 / 按住 K 变大」。\n" +
                 "做成文字而不是图，以后改键位只改这行，不用重新出图。")]
        [TextArea(1, 4)]
        public string keys;

        [Tooltip("演示动画的序列帧，按顺序排。留空则该页不显示演示区。")]
        public Sprite[] demoFrames;

        [Tooltip("一句话补充说明，选填。比如「泡泡在空气中会自动上升」。")]
        [TextArea(1, 3)]
        public string caption;
    }

    [Header("页面内容")]
    [Tooltip("按你希望的阅读顺序排列。建议：Star → Box → Bubble → BouncingBall → Spider → Fan → SpikeWall → Restart。")]
    [SerializeField] private HelpPage[] pages;

    [Header("必填引用")]
    [SerializeField] private Button helpButton;
    [SerializeField] private GameObject helpPanel;

    [Header("页面控件")]
    [Tooltip("页面标题文字，显示形态名。")]
    [SerializeField] private TextMeshProUGUI pageTitleText;

    [Tooltip("形态图标。")]
    [SerializeField] private Image iconImage;

    [Tooltip("按键说明文字。")]
    [SerializeField] private TextMeshProUGUI keysText;

    [Tooltip("演示动画。挂了 SpriteSequencePlayer 的那个 Image。")]
    [SerializeField] private SpriteSequencePlayer demoPlayer;

    [Tooltip("一句话补充说明，选填。")]
    [SerializeField] private TextMeshProUGUI captionText;

    [Header("翻页控件")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [Tooltip("显示「3 / 8」的文本，选填。")]
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    [Header("关闭控件")]
    [SerializeField] private Button closeButton;
    [Tooltip("遮罩上的 Button。点空白处也能关闭。")]
    [SerializeField] private Button backdropButton;

    [Header("行为设置")]
    [Tooltip("按 Esc 关闭弹窗。")]
    [SerializeField] private bool closeWithKeyboard = true;

    [Tooltip("用左右方向键翻页。不要用 A / D，那是游戏里的移动键。")]
    [SerializeField] private bool flipWithArrowKeys = true;

    [Tooltip("翻到最后一页再按下一页，回到第一页。")]
    [SerializeField] private bool wrapAround = true;

    [Tooltip("打开弹窗时暂停游戏（Time.timeScale = 0）。\n" +
             "主菜单：不要勾。\n" +
             "关卡内：确认本场景没有别的系统在改 timeScale 之后再勾。")]
    [SerializeField] private bool pauseWhileOpen;

    [Tooltip("每次重新打开都从第一页开始；不勾则停在上次看到的那页。")]
    [SerializeField] private bool resetToFirstPageOnOpen = true;

    private float timeScaleBeforeOpen = 1f;
    private int currentPageIndex;
    private bool isOpen;

    /// <summary>弹窗当前是否打开。其他脚本可以读这个值来避开输入冲突。</summary>
    public bool IsOpen => isOpen;

    /// <summary>当前页码，从 0 开始。</summary>
    public int CurrentPageIndex => currentPageIndex;

    private void Awake()
    {
        if (helpPanel == null || helpButton == null)
        {
            Debug.LogError(
                "HelpBookUI 缺少引用：请在 Inspector 里把 Help Button 和 Help Panel 拖进来。",
                this
            );

            enabled = false;
            return;
        }

        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("HelpBookUI 的 Pages 列表是空的，弹窗会显示空白页。", this);
        }

        helpButton.onClick.AddListener(Open);

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);

        helpPanel.SetActive(false);
        isOpen = false;
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (closeWithKeyboard && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (!flipWithArrowKeys)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            PreviousPage();
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            NextPage();
        }
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        helpPanel.SetActive(true);

        // Canvas 的渲染顺序由 Hierarchy 顺序决定，排最后的画在最上层。
        // 每次打开都移到最后，这样不管场景里 UI 怎么排，弹窗永远盖在最上面。
        helpPanel.transform.SetAsLastSibling();

        if (resetToFirstPageOnOpen)
        {
            currentPageIndex = 0;
        }

        ShowPage(currentPageIndex);

        if (pauseWhileOpen)
        {
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (demoPlayer != null)
        {
            demoPlayer.Stop();
        }

        helpPanel.SetActive(false);

        if (pauseWhileOpen)
        {
            // 还原打开前的值，而不是硬写 1f：
            // 硬写会在「别的系统已经暂停了游戏」时把它意外恢复。
            Time.timeScale = timeScaleBeforeOpen;
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    /// <summary>翻到下一页。</summary>
    public void NextPage()
    {
        GoToPage(currentPageIndex + 1);
    }

    /// <summary>翻到上一页。</summary>
    public void PreviousPage()
    {
        GoToPage(currentPageIndex - 1);
    }

    /// <summary>跳到指定页。页码从 0 开始。</summary>
    public void GoToPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        if (wrapAround)
        {
            // 取模实现循环翻页。加 pages.Length 是为了处理 -1 的情况：
            // C# 里 -1 % 8 等于 -1 而不是 7，所以要先加一轮再取模。
            index = (index % pages.Length + pages.Length) % pages.Length;
        }
        else
        {
            index = Mathf.Clamp(index, 0, pages.Length - 1);
        }

        if (index == currentPageIndex && isOpen)
        {
            return;
        }

        currentPageIndex = index;
        ShowPage(currentPageIndex);
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, pages.Length - 1);
        HelpPage page = pages[index];

        if (pageTitleText != null)
        {
            pageTitleText.text = page.title;
        }

        if (iconImage != null)
        {
            bool hasIcon = page.icon != null;
            // 用 enabled 而不是 SetActive：没有图标时隐藏这块 Image，
            // 但物体还在，布局占位不变，其他元素不会跳位置。
            iconImage.enabled = hasIcon;

            if (hasIcon)
            {
                iconImage.sprite = page.icon;
            }
        }

        if (keysText != null)
        {
            keysText.text = page.keys;
        }

        if (captionText != null)
        {
            captionText.text = page.caption;
            // 没有说明文字时整行隐藏，避免留下一块空白。
            captionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(page.caption));
        }

        if (demoPlayer != null)
        {
            bool hasDemo = page.demoFrames != null && page.demoFrames.Length > 0;
            demoPlayer.gameObject.SetActive(hasDemo);

            if (hasDemo)
            {
                demoPlayer.SetFrames(page.demoFrames);
            }
        }

        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPageIndex + 1} / {pages.Length}";
        }

        // 不循环时，到头了就把按钮置灰，告诉玩家没有更多页了。
        if (!wrapAround)
        {
            if (prevButton != null)
            {
                prevButton.interactable = currentPageIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentPageIndex < pages.Length - 1;
            }
        }
    }

    private void OnDisable()
    {
        // 场景切换时弹窗还开着并且暂停了游戏，必须还原 timeScale，
        // 否则下一个场景会以 timeScale = 0 开局，表现为「游戏卡死」。
        if (isOpen && pauseWhileOpen)
        {
            Time.timeScale = timeScaleBeforeOpen;
        }

        isOpen = false;
    }

    private void OnDestroy()
    {
        if (helpButton != null) helpButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
        if (prevButton != null) prevButton.onClick.RemoveListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
    }
}