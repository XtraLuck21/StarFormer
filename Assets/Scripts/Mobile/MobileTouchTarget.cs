using UnityEngine;
using UnityEngine.EventSystems;

// Each target owns one finger, so releasing an action never cancels the stick.
public sealed class MobileTouchTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IDragHandler
{
    private MobileFormControls controls;
    private bool isStick;
    private int? pointer;
    public bool Held => pointer.HasValue;

    internal void Initialize(MobileFormControls owner, bool movement)
    {
        controls = owner;
        isStick = movement;
    }

    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

    public void OnPointerDown(PointerEventData e)
    {
        if (Held || e.button != PointerEventData.InputButton.Left || Time.timeScale <= 0f) return;
        pointer = e.pointerId;
        if (isStick)
            controls.StartStick(LocalPoint(e));
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
        if (isStick) controls.EndStick();
        else transform.localScale = Vector3.one;
    }

    private void OnDisable() => Cancel();
}
