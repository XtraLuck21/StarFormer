using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(PlayerPossessionController))]
public sealed class MobileFormControls : MonoBehaviour
{
    [Tooltip("Auto Detect uses the device. Always Show previews touch controls with a mouse. Never Show uses the desktop HUD.")]
    [SerializeField] private MobileControlsUI.ShowMode showMode = MobileControlsUI.ShowMode.AutoDetect;

    public bool MobileMode => isActiveAndEnabled && MobileControlsUI.ResolveShouldShow(showMode);
    private PlayerPossessionController player;
    private RectTransform surface;
    private RectTransform safeArea;
    private RectTransform stick;
    private RectTransform thumb;
    private static readonly Vector2 RestPosition = new(165f, 125f);
    private CanvasGroup stickAppearance;
    private MobileControlGraphic thumbIcon;
    private MobileTouchTarget movement;
    private MobileTouchTarget jump;
    private MobileControlGraphic jumpIcon;
    private float jumpHighlightUntil;

    private MobileTouchTarget grow;
    private MobileTouchTarget shrink;
    private MobileTouchTarget pop;
    private PossessableBody displayedBody;
    private Vector2 origin;
    private static bool showedStickDemo;
    private float demoStart = float.NegativeInfinity;

    internal Vector2 Movement { get; private set; }
    internal bool GrowHeld => grow != null && grow.Held && displayedBody == player.CurrentBody;
    internal bool ShrinkHeld => shrink != null && shrink.Held && displayedBody == player.CurrentBody;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLesson() => showedStickDemo = false;

    private void Awake() => player = GetComponent<PlayerPossessionController>();

    private void LateUpdate()
    {
        var body = player.CurrentBody;
        bool visible = MobileMode && player.isActiveAndEnabled && !player.IsDead &&
            !player.IsRestarting && Time.timeScale > 0f && body != null && body.isActiveAndEnabled &&
            (body.Form is BodyForm.Star or BodyForm.Box or BodyForm.Bubble);
        if (!visible)
        {
            Hide();
            return;
        }

        if (surface == null) Build();
        Rect screen = Screen.safeArea;
        var lower = new Vector2(screen.xMin / Screen.width, screen.yMin / Screen.height);
        var upper = new Vector2(screen.xMax / Screen.width, screen.yMax / Screen.height);
        if (safeArea.anchorMin != lower) safeArea.anchorMin = lower;
        if (safeArea.anchorMax != upper) safeArea.anchorMax = upper;
        if (!surface.gameObject.activeSelf) surface.gameObject.SetActive(true);
        if (displayedBody != body)
        {
            jump.Cancel();
            grow.Cancel();
            shrink.Cancel();
            if (body.Form == BodyForm.Bubble && pop == null)
                pop = Button("Pop", MobileControlGraphic.Symbol.Pop, true);
            if (pop != null)
            {
                pop.Cancel();
                pop.gameObject.SetActive(body.Form == BodyForm.Bubble);
            }
            displayedBody = body;
            jump.gameObject.SetActive(body.Form == BodyForm.Star);
            grow.gameObject.SetActive(body.Form == BodyForm.Box);
            shrink.gameObject.SetActive(body.Form == BodyForm.Box);
        }
        jumpIcon.HighlightLesson(Time.time < jumpHighlightUntil);
        if (!movement.Held && !float.IsNegativeInfinity(demoStart))
        {
            // Only the knob moves during the first demonstration. This never drives the player.
            float t = Mathf.Clamp01((Time.time - demoStart - 0.3f) / 0.9f);
            float offset = Mathf.Sin(t * Mathf.PI);
            thumb.anchoredPosition = Vector2.right * (32f * offset * offset);
            if (t >= 1f)
            {
                thumb.anchoredPosition = Vector2.zero;
                demoStart = float.NegativeInfinity;
            }
        }
    }

    private void Build()
    {
        surface = Node("Touch Controls", transform);
        var canvas = surface.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = surface.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.75f;
        surface.gameObject.AddComponent<GraphicRaycaster>();
        safeArea = Node("Safe Area", surface);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

        var zone = Node("Movement Area", safeArea);
        zone.anchorMin = Vector2.zero;
        zone.anchorMax = new Vector2(0.5f, 0.68f);
        zone.offsetMin = zone.offsetMax = Vector2.zero;
        zone.gameObject.AddComponent<Image>().color = Color.clear;
        movement = zone.gameObject.AddComponent<MobileTouchTarget>();
        movement.Initialize(this, true);

        stick = Symbol("Floating Stick", zone, MobileControlGraphic.Symbol.Base, 160f);
        thumb = Symbol("Stick Thumb", stick, MobileControlGraphic.Symbol.Thumb, 82f);
        stickAppearance = stick.gameObject.AddComponent<CanvasGroup>();
        stickAppearance.blocksRaycasts = false;
        thumbIcon = thumb.GetComponent<MobileControlGraphic>();
        EndStick();
        if (!showedStickDemo)
        {
            showedStickDemo = true;
            demoStart = Time.time;
        }

        jump = Button("Jump", MobileControlGraphic.Symbol.Jump, false);
        jumpIcon = jump.GetComponent<MobileControlGraphic>();
        grow = Button("Grow", MobileControlGraphic.Symbol.Grow, false);
        shrink = Button("Shrink", MobileControlGraphic.Symbol.Shrink, true);
    }

    private MobileTouchTarget Button(string label, MobileControlGraphic.Symbol icon, bool secondary)
    {
        var rect = Symbol(label, safeArea, icon, 154f);
        rect.anchorMin = rect.anchorMax = Vector2.right;
        rect.anchoredPosition = secondary ? new Vector2(-105f, 232f) : new Vector2(-260f, 103f);
        rect.GetComponent<MobileControlGraphic>().raycastTarget = true;
        var target = rect.gameObject.AddComponent<MobileTouchTarget>();
        target.Initialize(this, false);
        return target;
    }

    private static RectTransform Node(string label, Transform parent)
    {
        var rect = (RectTransform)new GameObject(label, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform Symbol(string label, Transform parent, MobileControlGraphic.Symbol icon, float size)
    {
        var rect = Node(label, parent);
        rect.sizeDelta = Vector2.one * size;
        var graphic = rect.gameObject.AddComponent<MobileControlGraphic>();
        graphic.Icon = icon;
        graphic.raycastTarget = false;
        return rect;
    }

    internal void HighlightJumpLesson() => jumpHighlightUntil = Time.time + 0.75f;

    internal void PressAction(MobileTouchTarget target)
    {
        if (displayedBody == null || displayedBody != player.CurrentBody) return;
        if (target == pop && displayedBody.Form == BodyForm.Bubble)
            player.RequestRelease();
        else if (target == jump && displayedBody.Form == BodyForm.Star)
            player.PressTouchAction();
    }

    internal void StartStick(Vector2 point)
    {
        demoStart = float.NegativeInfinity;
        origin = point;
        // Pointer coordinates are relative to the movement area's center pivot.
        stick.anchorMin = stick.anchorMax = Vector2.one * 0.5f;
        stick.anchoredPosition = origin;
        stickAppearance.alpha = 1f;
        MoveStick(point);
    }

    internal void MoveStick(Vector2 point)
    {
        const float radius = 58f;
        const float deadZone = 0.16f;
        Vector2 delta = Vector2.ClampMagnitude((point - origin) / radius, 1f);
        thumb.anchoredPosition = delta * radius;
        thumbIcon.ShowDirection(delta.sqrMagnitude >= deadZone * deadZone);
        if (delta.sqrMagnitude >= deadZone * deadZone)
            thumb.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Movement = new Vector2(
            Mathf.Sign(delta.x) * Mathf.InverseLerp(deadZone, 1f, Mathf.Abs(delta.x)),
            Mathf.Sign(delta.y) * Mathf.InverseLerp(deadZone, 1f, Mathf.Abs(delta.y)));
    }

    internal void EndStick()
    {
        Movement = Vector2.zero;
        if (stick == null || thumb == null) return;
        stick.anchorMin = stick.anchorMax = Vector2.zero;
        stick.anchoredPosition = RestPosition;
        thumb.anchoredPosition = Vector2.zero;
        thumb.localRotation = Quaternion.identity;
        thumbIcon.ShowDirection(false);
        stickAppearance.alpha = 0.72f;
    }

    private void Hide()
    {
        if (surface != null && surface.gameObject.activeSelf) surface.gameObject.SetActive(false);
        displayedBody = null;
    }

    private void OnDisable() => Hide();
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) Hide();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Hide();
    }
}
