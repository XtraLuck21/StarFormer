using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// Each target owns one finger, so releasing an action never cancels the stick.
public sealed class MobileTouchTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IDragHandler
{
    private MobileFormControls controls;
    private bool isStick;
    private int? pointer;
    private Touchscreen stickScreen;
    private int stickTouchId;
    private Mouse stickMouse;
    public bool Held => pointer.HasValue;

    internal void Initialize(MobileFormControls owner, bool movement)
    {
        controls = owner;
        isStick = movement;
    }

    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || Time.timeScale <= 0f) return;
        var input = e as ExtendedPointerEventData;
        var screen = input?.device as Touchscreen;
        // A fresh thumb press can reclaim the stick after an interrupted browser gesture.
        if (Held && (!isStick || (screen == null && pointer != e.pointerId))) return;
        pointer = e.pointerId;
        if (isStick)
        {
            stickScreen = screen;
            stickTouchId = input?.touchId ?? 0;
            stickMouse = input?.device as Mouse;
            controls.StartStick(LocalPoint(e));
        }
        else
        {
            transform.localScale = Vector3.one * 0.94f;
            controls.PressAction(this);
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (isStick && pointer == e.pointerId) controls.MoveStick(LocalPoint(e));
    }

    private void LateUpdate()
    {
        // The browser can cancel a contact without delivering a UI pointer-up to this target.
        if (stickScreen != null)
        {
            if (stickScreen.added)
            {
                foreach (var touch in stickScreen.touches)
                    if (touch.touchId.ReadValue() == stickTouchId && touch.press.isPressed) return;
            }
            Cancel();
        }
        else if (stickMouse != null && (!stickMouse.added || !stickMouse.leftButton.isPressed))
            Cancel();
    }

    private Vector2 LocalPoint(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position,
            e.pressEventCamera, out Vector2 point);
        return point;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (pointer == e.pointerId && e.button == PointerEventData.InputButton.Left) Cancel();
    }

    internal void Cancel()
    {
        if (!Held) return;
        pointer = null;
        stickScreen = null;
        stickMouse = null;
        if (isStick) controls.EndStick();
        else transform.localScale = Vector3.one;
    }

    private void OnDisable() => Cancel();
}
