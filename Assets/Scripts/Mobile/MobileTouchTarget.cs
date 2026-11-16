using System.Collections.Generic;
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
    private bool gameplayStick;
    private bool recoverHeldTouch;
    private readonly List<RaycastResult> hits = new();
    public bool Held => pointer.HasValue || stickScreen != null;

    internal void EnableGameplayTouchRecovery(bool atCheckpoint)
    {
        gameplayStick = true;
        recoverHeldTouch = atCheckpoint;
    }

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
            if (screen != null && stickScreen == screen && stickTouchId == input.touchId) return;
            stickScreen = screen;
            stickTouchId = input?.touchId ?? 0;
            stickMouse = input?.device as Mouse;
            var origin = e.position;
            // A short swipe can arrive in the same input update as its initial press.
            if (gameplayStick && screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    if (touch.touchId.ReadValue() != stickTouchId || !touch.press.wasPressedThisFrame) continue;
                    origin = touch.startPosition.ReadValue();
                    break;
                }
            }
            controls.StartStick(LocalPoint(origin, e.pressEventCamera));
            controls.MoveStick(LocalPoint(e));
        }
        else
        {
            transform.localScale = Vector3.one * 0.94f;
            controls.PressAction(this);
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isStick || pointer != e.pointerId) return;
        if (!InsideScreen(e.position)) Cancel();
        else controls.MoveStick(LocalPoint(e));
    }

    private void LateUpdate()
    {
        // The replacement HUD may appear after a thumb went down during a checkpoint reload.
        if (gameplayStick && Time.timeScale > 0f)
        {
            ClaimMissedPress();
            recoverHeldTouch = false;
        }
        // The browser can cancel a contact without delivering a UI pointer-up to this target.
        if (stickScreen != null)
        {
            if (stickScreen.added)
            {
                foreach (var touch in stickScreen.touches)
                    if (touch.touchId.ReadValue() == stickTouchId && touch.press.isPressed &&
                        InsideScreen(touch.position.ReadValue()))
                    {
                        if (gameplayStick) controls.MoveStick(LocalPoint(touch.position.ReadValue(), null));
                        return;
                    }
            }
            Cancel();
        }
        else if (stickMouse != null && (!stickMouse.added || !stickMouse.leftButton.isPressed ||
                 !InsideScreen(stickMouse.position.ReadValue())))
            Cancel();
    }

    private void ClaimMissedPress()
    {
        var screen = Touchscreen.current;
        var events = EventSystem.current;
        if (screen == null || events == null) return;
        foreach (var touch in screen.touches)
        {
            if (!touch.press.isPressed || (!touch.press.wasPressedThisFrame && !(recoverHeldTouch && !Held))) continue;
            int id = touch.touchId.ReadValue();
            if (stickScreen == screen && stickTouchId == id) continue;
            var point = touch.position.ReadValue();
            var zone = (RectTransform)transform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(zone, touch.startPosition.ReadValue()) ||
                !RectTransformUtility.RectangleContainsScreenPoint(zone, point)) continue;
            // Respect the same UI hit order as an ordinary press, including open menus.
            hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = point }, hits);
            if (hits.Count == 0 || hits[0].gameObject != gameObject) continue;
            pointer = null;
            stickMouse = null;
            stickScreen = screen;
            stickTouchId = id;
            // A thumb held through respawn gets a neutral origin at its present position.
            var origin = touch.press.wasPressedThisFrame ? touch.startPosition.ReadValue() : point;
            controls.StartStick(LocalPoint(origin, null));
            return;
        }
    }

    private static bool InsideScreen(Vector2 point) =>
        point.x > 0f && point.y > 0f && point.x < Screen.width && point.y < Screen.height;

    private Vector2 LocalPoint(PointerEventData e) => LocalPoint(e.position, e.pressEventCamera);

    private Vector2 LocalPoint(Vector2 screenPoint, Camera camera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, camera, out Vector2 point);
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
