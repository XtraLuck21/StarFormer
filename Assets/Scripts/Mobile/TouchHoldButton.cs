using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在屏幕上的每一个虚拟按钮上。
/// 方向键（Left/Right/Up/Down）是“按住型”：手指按下算按着，抬起才松开。
/// 动作键（Action/Release/Restart）是“点一下型”：按下的瞬间触发一次。
///
/// 注意：按钮的 Image 组件必须勾上 Raycast Target，否则收不到手指事件。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchHoldButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonKind
    {
        Left,     // 左移
        Right,    // 右移
        Up,       // 上（只有泡泡在水里用得到）
        Down,     // 下（同上）
        Action,   // 空格：星星跳跃 / 尖刺墙发射。只做主动能力
        Release,  // E：脱离当前形态（泡泡破碎、蜘蛛与风扇离开）
        Restart   // R：清除检查点并重开关卡
    }

    [SerializeField] private PlayerPossessionController controller;
    [SerializeField] private ButtonKind kind;

    [Header("可选的按下反馈")]
    [Tooltip("留空也没关系。填了的话，按下时会变半透明。")]
    [SerializeField] private CanvasGroup pressedFeedback;
    [SerializeField, Range(0.1f, 1f)] private float pressedAlpha = 0.6f;

    private bool isHeld;

    private void Reset()
    {
        // 把脚本拖到按钮上时，自动帮你找到场景里的控制器。
        controller = FindFirstObjectByType<PlayerPossessionController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller == null)
        {
            Debug.LogError("TouchHoldButton 没有指定 Controller。", this);
            return;
        }

        switch (kind)
        {
            case ButtonKind.Action:
                controller.PressTouchAction();
                break;

            case ButtonKind.Release:
                controller.RequestRelease();
                break;

            case ButtonKind.Restart:
                controller.PressTouchRestart();
                break;

            default:
                SetHeld(true);
                break;
        }

        ApplyFeedback(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetHeld(false);
        ApplyFeedback(false);
    }

    // 关键：按钮被隐藏（比如泡泡出水面，上下键消失）时强制松开，
    // 否则 OnPointerUp 永远不会来，方向会卡死。
    private void OnDisable()
    {
        SetHeld(false);
        ApplyFeedback(false);
    }

    private void SetHeld(bool held)
    {
        if (controller == null || isHeld == held)
        {
            return;
        }

        isHeld = held;

        switch (kind)
        {
            case ButtonKind.Left:
                controller.SetTouchLeft(held);
                break;

            case ButtonKind.Right:
                controller.SetTouchRight(held);
                break;

            case ButtonKind.Up:
                controller.SetTouchUp(held);
                break;

            case ButtonKind.Down:
                controller.SetTouchDown(held);
                break;
        }
    }

    private void ApplyFeedback(bool pressed)
    {
        if (pressedFeedback == null)
        {
            return;
        }

        pressedFeedback.alpha = pressed ? pressedAlpha : 1f;
    }
}