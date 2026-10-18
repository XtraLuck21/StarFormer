using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Show the buttons appropriate for the currently possessed form.
/// Attach to an empty object under Canvas and assign the button references.
///
/// Controls follow the form-specific logic in PlayerPossessionController.FixedUpdate:
///   Star        Left/Right + Jump
///   Box         Left/Right + Grow/Shrink (BoxResizeAbility)
///   Bubble      Left/Right + Pop; also Up/Down while in water
///   SpikeWall   Left/Right + Fire
///   Spider      Left/Right + Leave
///   Fan         Left/Right + Blow/Leave
///   BouncyBall  Left/Right
///   gravityBall All hidden (no player control)
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    public enum ShowMode
    {
        AutoDetect,   // Show only on detected mobile/touch devices; use for release builds.
        AlwaysShow,   // Always show; useful for mouse testing in the Editor.
        NeverShow     // Always hide; useful for keyboard-only testing.
    }

    [Header("Control Visibility")]
    [Tooltip("AutoDetect: show on detected mobile/touch devices; hide on desktop.\n" +
             "AlwaysShow: use for mouse testing in the Editor.\n" +
             "Restore AutoDetect before building for release.")]
    [SerializeField] private ShowMode showMode = ShowMode.AutoDetect;

    [SerializeField] private PlayerPossessionController controller;

    [Header("Direction Buttons")]
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    [SerializeField] private GameObject upButton;
    [SerializeField] private GameObject downButton;

    [Header("Form-Specific Action Buttons")]
    [SerializeField] private GameObject jumpButton;   // Star, Kind = Action.
    [SerializeField] private GameObject fireButton;   // SpikeWall, Kind = Action.
    [SerializeField] private GameObject leaveButton;  // Fan / Spider, Kind = Release.
    [SerializeField] private GameObject popButton;    // Bubble, Kind = Release.
    [SerializeField] private GameObject blowButton;   // Fan wind, Kind = Blow.

    [Header("Box Resizing (Use Up / Down for Kind)")]
    [Tooltip("Grow = Up, Shrink = Down. These share VerticalInput with the direction buttons, " +
             "but appear on the right as dedicated resizing controls.")]
    [SerializeField] private GameObject growButton;
    [SerializeField] private GameObject shrinkButton;

    private PossessableBody lastBody;
    private BodyForm lastForm;
    private bool lastInWater;
    private bool lastDead;
    private bool hasCachedState;
    private bool touchControlsEnabled;

    private void Awake()
    {
        touchControlsEnabled = ResolveShouldShow();

        if (!touchControlsEnabled)
        {
            // Hide all controls before the first frame on desktop; Update then exits early.
            HideAll();
        }
    }

    private bool ResolveShouldShow()
    {
        switch (showMode)
        {
            case ShowMode.AlwaysShow:
                return true;

            case ShowMode.NeverShow:
                return false;
        }

        // Use Unity's mobile-platform detection for native mobile players
        // and supported mobile WebGL environments.
        if (Application.isMobilePlatform)
        {
            return true;
        }

        // Also show on touch-only computers or tablets when no mouse is present.
        return Touchscreen.current != null && Mouse.current == null;
    }

    private void Reset()
    {
        controller = FindFirstObjectByType<PlayerPossessionController>();
    }

    private void Update()
    {
        if (!touchControlsEnabled || controller == null)
        {
            return;
        }

        PossessableBody body = controller.CurrentBody;
        bool isDead = controller.IsDead;

        if (body == null)
        {
            if (!hasCachedState || lastBody != null)
            {
                HideAll();
                lastBody = null;
                hasCachedState = true;
            }

            return;
        }

        BodyForm form = body.Form;
        bool inWater = body.IsInWater;

        // Avoid repeated SetActive calls when the relevant state has not changed.
        if (hasCachedState &&
            lastBody == body &&
            lastForm == form &&
            lastInWater == inWater &&
            lastDead == isDead)
        {
            return;
        }

        lastBody = body;
        lastForm = form;
        lastInWater = inWater;
        lastDead = isDead;
        hasCachedState = true;

        if (isDead)
        {
            HideAll();
            return;
        }

        Apply(form, inWater);
    }

    private void Apply(BodyForm form, bool inWater)
    {
        // gravityBall is a scripted sequence without player controls.
        bool canControl = form != BodyForm.gravityBall;

        // Only swimming Bubble needs directional Up/Down; airborne Bubble rises automatically.
        bool needsVertical = canControl &&
            form == BodyForm.Bubble &&
            inWater;

        SetActive(leftButton, canControl);
        SetActive(rightButton, canControl);
        SetActive(upButton, needsVertical);
        SetActive(downButton, needsVertical);

        // Box Grow/Shrink controls.
        bool canResize = form == BodyForm.Box;
        SetActive(growButton, canResize);
        SetActive(shrinkButton, canResize);

        SetActive(jumpButton, form == BodyForm.Star);
        SetActive(fireButton, form == BodyForm.SpikeWall);
        SetActive(leaveButton,
            form == BodyForm.Fan || form == BodyForm.Spider);
        SetActive(popButton, form == BodyForm.Bubble);
        SetActive(blowButton, form == BodyForm.Fan);
    }

    private void HideAll()
    {
        SetActive(leftButton, false);
        SetActive(rightButton, false);
        SetActive(upButton, false);
        SetActive(downButton, false);
        SetActive(growButton, false);
        SetActive(shrinkButton, false);
        SetActive(jumpButton, false);
        SetActive(fireButton, false);
        SetActive(leaveButton, false);
        SetActive(popButton, false);
        SetActive(blowButton, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}