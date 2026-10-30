using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to each on-screen control button.
/// Direction buttons (Left/Right/Up/Down) stay held from pointer down until pointer up.
/// Action/Release/Restart trigger once on pointer down.
///
/// Enable Raycast Target on the button's Image so it receives pointer events.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchHoldButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonKind
    {
        Left,     // Move left.
        Right,    // Move right.
        Up,       // Upward input (Bubble swimming or Box growth).
        Down,     // Downward input (Bubble swimming or Box shrinking).
        Action,   // Space: Star jump / SpikeWall fire; active abilities only.
        Release,  // E: pop Bubble or leave Spider/Fan.
        Restart,  // Open restart choices.
        Blow      // W: toggle Fan wind on or off.
    }

    [SerializeField] private PlayerPossessionController controller;
    [SerializeField] private ButtonKind kind;

    [Header("Optional Press Feedback")]
    [Tooltip("Optional. When assigned, the button becomes semi-transparent while pressed.")]
    [SerializeField] private CanvasGroup pressedFeedback;
    [SerializeField, Range(0.1f, 1f)] private float pressedAlpha = 0.6f;

    private bool isHeld;

    private void Reset()
    {
        // Find the scene's controller automatically when this component is added to a button.
        controller = FindFirstObjectByType<PlayerPossessionController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller == null)
        {
            Debug.LogError("TouchHoldButton has no Controller assigned.", this);
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

            case ButtonKind.Blow:
                controller.PressTouchBlow();
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

    // Release held input when hidden, such as when Bubble leaves water and Up/Down disappear.
    // OnPointerUp may never arrive after hiding, which would otherwise leave movement stuck.
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
