using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>One-finger skill wheel, independent of the movement joystick.</summary>
public sealed class SpiderSilkAimControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IDragHandler
{
    private enum InputMode { None, Wheel, Mouse }
    private PlayerPossessionController player;
    private MobileFormControls mobileControls;
    private bool ShowTouchWheel => mobileControls != null && mobileControls.MobileMode;
    private SpiderSilkAbility ability;
    private CanvasGroup visibility;
    private RectTransform safeArea, wheel, knob, cancelZone;
    private MobileControlGraphic buttonGraphic, cancelGraphic;
    private TMP_Text caption, cooldownText;
    private InputMode inputMode;
    private int pointerId;
    private Touchscreen pointerScreen;
    private int touchId;
    private Mouse pointerMouse;
    private bool cancelRelease;
    private const float WheelRadius = 90f;

    public static SpiderSilkAimControl Create(PlayerPossessionController owner)
    {
        var root = Node("Spider Skill Controls", owner.transform);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.75f;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var group = root.gameObject.AddComponent<CanvasGroup>();
        var safe = Node("Safe Area", root);
        var button = Node("Silk Skill", safe);
        button.anchorMin = button.anchorMax = Vector2.right;
        button.anchoredPosition = new Vector2(-260f, 180f);
        button.sizeDelta = Vector2.one * 154f;
        var graphic = button.gameObject.AddComponent<MobileControlGraphic>();
        graphic.Icon = MobileControlGraphic.Symbol.Silk;
        var control = button.gameObject.AddComponent<SpiderSilkAimControl>();
        control.player = owner;
        control.mobileControls = owner.GetComponent<MobileFormControls>();
        control.visibility = group;
        control.safeArea = safe;
        control.buttonGraphic = graphic;
        control.Build();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        return control;
    }

    private void Build()
    {
        wheel = Node("Aim Wheel", transform);
        wheel.sizeDelta = Vector2.one * 250f;
        var disc = wheel.gameObject.AddComponent<MobileControlGraphic>();
        disc.Icon = MobileControlGraphic.Symbol.SkillWheel;
        disc.raycastTarget = false;
        knob = Node("Aim Thumb", wheel);
        knob.sizeDelta = Vector2.one * 70f;
        var thumb = knob.gameObject.AddComponent<MobileControlGraphic>();
        thumb.Icon = MobileControlGraphic.Symbol.Thumb;
        thumb.ShowDirection(true);
        thumb.raycastTarget = false;
        cancelZone = Node("Cancel Cast", transform);
        cancelZone.anchoredPosition = new Vector2(0f, 230f);
        cancelZone.sizeDelta = Vector2.one * 100f;
        cancelGraphic = cancelZone.gameObject.AddComponent<MobileControlGraphic>();
        cancelGraphic.Icon = MobileControlGraphic.Symbol.Cancel;
        cancelGraphic.raycastTarget = false;
        Label(cancelZone, "CANCEL", new Vector2(0f, -68f), 20f);
        caption = Label((RectTransform)transform, "HOLD + DRAG", new Vector2(0f, -150f), 20f);
        cooldownText = Label((RectTransform)transform, "", Vector2.zero, 36f);
        wheel.gameObject.SetActive(false);
        cancelZone.gameObject.SetActive(false);
    }

    private static RectTransform Node(string label, Transform parent)
    {
        var rect = (RectTransform)new GameObject(label, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        return rect;
    }

    private static TMP_Text Label(RectTransform parent, string value, Vector2 offset, float size)
    {
        var rect = Node("Label", parent);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(280f, 48f);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.color = new Color(0.8f, 0.92f, 1f);
        text.outlineColor = new Color(0.03f, 0.08f, 0.12f, 0.8f);
        text.outlineWidth = 0.18f;
        return text;
    }

    private bool RefreshAbility()
    {
        var body = player != null ? player.CurrentBody : null;
        SpiderSilkAbility next = null;
        bool available = player != null && player.isActiveAndEnabled && !player.IsDead && !player.IsRestarting &&
            Time.timeScale > 0f && body != null && body.isActiveAndEnabled && body.Form == BodyForm.Spider &&
            body.TryGetComponent(out next) && next.isActiveAndEnabled;
        if (!available || ability != next) CancelGesture();
        ability = available ? next : null;
        return available;
    }

    private void LateUpdate()
    {
        bool visible = RefreshAbility() && ShowTouchWheel;
        visibility.alpha = visible ? 1f : 0f;
        visibility.blocksRaycasts = visible;
        if (!visible)
        {
            if (inputMode == InputMode.Wheel) CancelGesture();
            return;
        }
        Rect screen = Screen.safeArea;
        safeArea.anchorMin = new Vector2(screen.xMin / Screen.width, screen.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(screen.xMax / Screen.width, screen.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        if (inputMode != InputMode.None && !ability.IsAiming &&
            !(inputMode == InputMode.Wheel && cancelRelease)) CancelGesture();
        // Lost browser contacts cancel; only a real pointer-up casts the skill.
        if (inputMode == InputMode.Wheel)
        {
            if (pointerScreen != null)
            {
                bool held = false;
                if (pointerScreen.added)
                    foreach (var touch in pointerScreen.touches)
                        if (touch.touchId.ReadValue() == touchId && touch.press.isPressed) { held = true; break; }
                if (!held) CancelGesture();
            }
            else if (pointerMouse != null && (!pointerMouse.added || !pointerMouse.leftButton.isPressed)) CancelGesture();
        }
        float remaining = ability.CooldownRemaining;
        cooldownText.text = remaining > 0f ? remaining.ToString("0.0") : "";
        buttonGraphic.canvasRenderer.SetAlpha(ability.CanAim || ability.IsAiming ? 1f : 0.45f);
        if (inputMode == InputMode.None) caption.text = ability.IsPulling ? "PULLING" : "HOLD + DRAG";
    }

    public void ProcessDesktopInput(Mouse mouse, Keyboard keyboard, bool gameplayMousePress)
    {
        if (!RefreshAbility()) return;
        if (inputMode == InputMode.Wheel) return;
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)) { CancelGesture(); return; }
        if (inputMode == InputMode.None)
        {
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || !gameplayMousePress || !ability.BeginAim()) return;
            inputMode = InputMode.Mouse;
            caption.text = "RELEASE TO CAST";
        }
        if (mouse != null && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            var plane = new Plane(Vector3.forward, ability.transform.position);
            if (plane.Raycast(ray, out float enter)) ability.Aim((Vector2)(ray.GetPoint(enter) - ability.transform.position));
        }
        bool released = mouse != null && mouse.leftButton.wasReleasedThisFrame;
        if (released)
        {
            if (gameplayMousePress) ability.ReleaseAim();
            CancelGesture();
        }
        else if (mouse == null || !mouse.leftButton.isPressed) CancelGesture();
    }

    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

    public void OnPointerDown(PointerEventData e)
    {
        if (!ShowTouchWheel || e.button != PointerEventData.InputButton.Left || inputMode != InputMode.None ||
            !RefreshAbility() || !ability.BeginAim()) return;
        inputMode = InputMode.Wheel;
        pointerId = e.pointerId;
        var extended = e as ExtendedPointerEventData;
        pointerScreen = extended?.device as Touchscreen;
        touchId = extended?.touchId ?? 0;
        pointerMouse = extended?.device as Mouse;
        wheel.gameObject.SetActive(true);
        cancelZone.gameObject.SetActive(true);
        DragAim(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (inputMode == InputMode.Wheel && pointerId == e.pointerId) DragAim(e);
    }

    private void DragAim(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var delta);
        bool outside = e.position.x < 0 || e.position.y < 0 || e.position.x > Screen.width || e.position.y > Screen.height;
        bool overCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelZone, e.position, e.pressEventCamera);
        cancelRelease = outside || overCancel || delta.magnitude < 14f;
        knob.anchoredPosition = Vector2.ClampMagnitude(delta, WheelRadius);
        knob.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        cancelGraphic.HighlightLesson(overCancel);
        caption.text = cancelRelease ? "RELEASE TO CANCEL" : "RELEASE TO CAST";
        if (cancelRelease) ability.CancelAim();
        else
        {
            if (!ability.IsAiming) ability.BeginAim();
            var camera = Camera.main;
            Vector2 worldDirection = camera != null
                ? (Vector2)(camera.transform.right * delta.x + camera.transform.up * delta.y) : delta;
            ability.Aim(worldDirection);
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (inputMode != InputMode.Wheel || pointerId != e.pointerId || e.button != PointerEventData.InputButton.Left) return;
        if (ShowTouchWheel && RefreshAbility())
        {
            DragAim(e);
            if (!cancelRelease) ability.ReleaseAim();
        }
        CancelGesture();
    }

    public void CancelGesture()
    {
        if (ability != null) ability.CancelAim();
        inputMode = InputMode.None;
        pointerScreen = null;
        pointerMouse = null;
        if (wheel != null) wheel.gameObject.SetActive(false);
        if (cancelZone != null) cancelZone.gameObject.SetActive(false);
    }

    private void OnDisable() => CancelGesture();
    private void OnApplicationFocus(bool focused) { if (!focused) CancelGesture(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelGesture(); }
}
