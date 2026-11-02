using System.Collections;
using TMPro;
using UnityEngine;

// Coordinates the optional SAVE/RUIN choices in Level01_Advanced.
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class AdvancedRouteSwitch : MonoBehaviour
{
    public enum RouteState { Normal, Safe, Hard }

    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody targetBox;
    [SerializeField] private PossessableBody targetBall;
    [SerializeField] private GameObject routeA;
    [SerializeField] private GameObject routeB;
    [SerializeField] private GameObject hardFloorSpikes;
    [SerializeField] private GameObject hardCeilingSpikes;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Transform buttonFace;
    [SerializeField] private BoxCollider2D ruinSensor;
    [SerializeField] private RuinButtonGuard ruinGuard;
    [SerializeField] private TMP_Text ruinLabel;
    [SerializeField] private Transform ruinButtonFace;
    [SerializeField] private AdvancedLevelCamera2D levelCamera;
    [SerializeField] private Color idleColor = new Color32(255, 211, 77, 255);
    [SerializeField] private Color activatedColor = new Color32(102, 228, 218, 255);
    [SerializeField] private Color ruinIdleColor = new Color32(238, 100, 105, 255);
    [SerializeField] private Color ruinActivatedColor = new Color32(255, 151, 71, 255);

    [Header("Button Interaction")]
    [SerializeField, Min(0f)] private float rearmHorizontalPadding = .3f;

    [Header("Route Presentation")]
    [SerializeField, Min(.1f)] private float cameraMoveSeconds = .85f;
    [SerializeField, Min(0f)] private float previewHoldSeconds = 1f;
    [SerializeField, Min(.1f)] private float spikeSinkSeconds = 1.6f;
    [SerializeField, Min(.7f)] private float spikeSinkDistance = 1f;
    [SerializeField, Min(0f)] private float buttonPressDistance = .12f;
    [SerializeField, Min(.1f)] private float previewHalfWidth = 1.4f;
    [SerializeField] private float previewMinimumY = 11.25f;
    [SerializeField] private Vector2 previewCenter = new Vector2(8f, 13.7f);
    [SerializeField] private Vector2 overviewCenter = new Vector2(8.3f, 14.25f);
    [SerializeField, Min(1f)] private float previewSize = 3.4f;
    [SerializeField, Min(1f)] private float overviewSize = 4.2f;

    private BoxCollider2D sensor;
    private SpriteRenderer buttonRenderer;
    private SpriteRenderer ruinRenderer;
    private Vector3 faceStart;
    private Vector3 ruinFaceStart;
    private HazardGroup[] hazards;
    private Camera viewCamera;
    private Vector3 returnPosition;
    private float returnSize;
    private float previousTimeScale;
    private bool controllerWasEnabled;
    private bool ownsPresentation;
    private bool changingRoute;
    private bool previewShown;
    private bool safeShown;
    private bool hardShown;
    private bool saveArmed = true;
    private bool ruinArmed = true;
    private float savePressX;
    private float ruinPressX;
    public RouteState CurrentRoute { get; private set; } = RouteState.Normal;
    public bool IsActivated => CurrentRoute == RouteState.Safe;
    public bool IsPresenting => ownsPresentation;
    public bool HasShownPreview => previewShown;
    public bool HasShownSafe => safeShown;
    public bool HasShownHard => hardShown;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        buttonRenderer = buttonFace != null ? buttonFace.GetComponent<SpriteRenderer>() : null;
        ruinRenderer = ruinButtonFace != null ? ruinButtonFace.GetComponent<SpriteRenderer>() : null;
        if (playerController == null || targetBox == null || targetBall == null ||
            routeA == null || routeB == null || hardFloorSpikes == null || hardCeilingSpikes == null ||
            ruinSensor == null || ruinGuard == null || buttonRenderer == null || ruinRenderer == null || routeA == routeB)
        {
            Debug.LogError("Assign both route buttons, Box D, R2, and the separate route/hazard roots.", this);
            enabled = false;
            return;
        }
        sensor.isTrigger = true;
        ruinSensor.isTrigger = true;
        if (levelCamera == null) levelCamera = FindFirstObjectByType<AdvancedLevelCamera2D>();
        if (levelCamera != null) viewCamera = levelCamera.GetComponent<Camera>();
        FitButtonLabel(label);
        FitButtonLabel(ruinLabel);
        faceStart = buttonFace.localPosition;
        ruinFaceStart = ruinButtonFace.localPosition;
        hazards = new[]
        {
            new HazardGroup(routeA, Vector3.down * spikeSinkDistance),
            new HazardGroup(hardFloorSpikes, Vector3.down * spikeSinkDistance),
            new HazardGroup(hardCeilingSpikes, Vector3.up * spikeSinkDistance, growFromAnchor: true)
        };
        FinishRouteChange();
    }

    private void Update()
    {
        if (ownsPresentation || Time.timeScale <= 0f || !playerController.isActiveAndEnabled || playerController.IsDead)
            return;
        PossessableBody controlled = playerController.CurrentBody;
        bool isBox = controlled == targetBox;
        bool isBall = controlled == targetBall;
        bool isStar = controlled != null && controlled.Form == BodyForm.Star;
        if (controlled == null || !controlled.isActiveAndEnabled || !controlled.IsReady || !controlled.IsControlled ||
            !controlled.Body.simulated || controlled.Body.bodyType != RigidbodyType2D.Dynamic || !controlled.BodyCollider.enabled)
            return;
        Bounds body = controlled.BodyCollider.bounds;
        // Vertical bounces and held Box contact never rearm a button. The whole
        // controlled body must leave its horizontal band, with a little margin.
        if (!saveArmed && HasLeftHorizontally(sensor, body, savePressX)) saveArmed = true;
        if (!ruinArmed && HasLeftHorizontally(ruinSensor, body, ruinPressX)) ruinArmed = true;
        if ((!isBox && !isBall && !isStar) || (isBox && !controlled.IsGrounded())) return;

        // Both upper-route entry forms share one preview. The floor bound keeps
        // a Star in the lower room from triggering it through the shared ceiling.
        if ((isBox || isStar) && !previewShown && CurrentRoute == RouteState.Normal &&
            body.center.y >= previewMinimumY && body.center.y < sensor.bounds.min.y &&
            Mathf.Abs(body.center.x - sensor.bounds.center.x) <= previewHalfWidth)
        {
            if (BeginPresentation())
            {
                previewShown = true;
                StartCoroutine(PreviewRoute());
                return;
            }
        }
        if (saveArmed && TouchesFromBelow(sensor, controlled))
        {
            if (SelectRoute(CurrentRoute == RouteState.Safe ? RouteState.Normal : RouteState.Safe))
            {
                savePressX = body.center.x;
                saveArmed = false;
            }
        }
        else if (ruinArmed && isBall && ruinGuard.CanActivate && TouchesFromBelow(ruinSensor, controlled))
        {
            if (SelectRoute(CurrentRoute == RouteState.Hard ? RouteState.Normal : RouteState.Hard))
            {
                ruinPressX = body.center.x;
                ruinArmed = false;
            }
        }
    }

    private bool HasLeftHorizontally(Collider2D button, Bounds body, float pressX)
    {
        // Shrinking at a button's edge must not count as walking away.
        if (Mathf.Abs(body.center.x - pressX) <= rearmHorizontalPadding) return false;
        Bounds area = button.bounds;
        return body.max.x < area.min.x - rearmHorizontalPadding ||
            body.min.x > area.max.x + rearmHorizontalPadding;
    }

    private static bool TouchesFromBelow(Collider2D button, PossessableBody body)
    {
        Bounds form = body.BodyCollider.bounds;
        Bounds target = button.bounds;
        return button.IsTouching(body.BodyCollider) && form.center.y < target.min.y &&
            form.max.y >= target.min.y && form.max.x > target.min.x && form.min.x < target.max.x;
    }

    // Scene-reload recovery applies final geometry without replaying the cinematic.
    public void RestoreCheckpointRoute(RouteState state, bool seenPreview = false,
        bool seenSafe = false, bool seenHard = false)
    {
        CurrentRoute = state;
        previewShown = seenPreview;
        safeShown = seenSafe;
        hardShown = seenHard;
        saveArmed = ruinArmed = true;
        FinishRouteChange();
    }

    private bool SelectRoute(RouteState state)
    {
        // Only one camera presentation may own the pause at a time.
        if (levelCamera != null && levelCamera.IsCinematic) return false;
        bool brief = state == RouteState.Normal || (state == RouteState.Safe ? safeShown : hardShown);
        CurrentRoute = state;
        if (state == RouteState.Safe) safeShown = true;
        else if (state == RouteState.Hard) hardShown = true;
        previewShown = true;
        UpdateButtons();
        routeB.SetActive(false);
        for (int i = 0; i < hazards.Length; i++) hazards[i].PrepareAnimation(ShouldShow(i));
        changingRoute = true;
        if (BeginPresentation()) StartCoroutine(RevealRoute(brief));
        else FinishRouteChange();
        return true;
    }

    private bool BeginPresentation()
    {
        if (levelCamera == null || viewCamera == null || !levelCamera.TryBeginCinematic()) return false;
        returnPosition = viewCamera.transform.position;
        returnSize = viewCamera.orthographicSize;
        previousTimeScale = Time.timeScale;
        controllerWasEnabled = playerController.enabled;
        ownsPresentation = true;
        playerController.ClearInputForRestartMenu();
        // Freeze time without discarding the Ball's velocity or bounce phase.
        playerController.enabled = false;
        Time.timeScale = 0f;
        return true;
    }

    private IEnumerator PreviewRoute()
    {
        yield return MoveCamera(previewCenter, FramedSize(previewSize), cameraMoveSeconds);
        yield return new WaitForSecondsRealtime(previewHoldSeconds);
        yield return MoveCamera(returnPosition, returnSize, cameraMoveSeconds);
        RestorePresentation();
    }

    private IEnumerator RevealRoute(bool brief)
    {
        float moveSeconds = brief ? cameraMoveSeconds * .55f : cameraMoveSeconds;
        yield return MoveCamera(overviewCenter, FramedSize(overviewSize), moveSeconds);
        yield return new WaitForSecondsRealtime(brief ? .1f : .25f);
        Vector3[] starts = new Vector3[hazards.Length];
        for (int i = 0; i < hazards.Length; i++) starts[i] = hazards[i].AnimationPosition;
        float duration = brief ? spikeSinkSeconds * .6f : spikeSinkSeconds;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < hazards.Length; i++)
                hazards[i].MoveDuringAnimation(Vector3.Lerp(starts[i], hazards[i].Position(ShouldShow(i)), progress));
            yield return null;
        }
        FinishRouteChange();
        yield return new WaitForSecondsRealtime(brief ? .2f : .6f);
        yield return MoveCamera(returnPosition, returnSize, moveSeconds);
        RestorePresentation();
    }

    private bool ShouldShow(int index) => index == 0 ? CurrentRoute != RouteState.Safe : CurrentRoute == RouteState.Hard;

    private void FinishRouteChange()
    {
        if (hazards == null) return;
        for (int i = 0; i < hazards.Length; i++) hazards[i].Finish(ShouldShow(i));
        routeB.SetActive(CurrentRoute == RouteState.Safe);
        changingRoute = false;
        UpdateButtons();
    }

    private static void FitButtonLabel(TMP_Text buttonLabel)
    {
        if (buttonLabel == null) return;
        // Keep the longer undo caption inside the existing button face.
        buttonLabel.fontSizeMax = buttonLabel.fontSize;
        buttonLabel.fontSizeMin = buttonLabel.fontSize * .7f;
        buttonLabel.enableAutoSizing = true;
    }

    private void UpdateButtons()
    {
        bool safe = CurrentRoute == RouteState.Safe;
        bool hard = CurrentRoute == RouteState.Hard;
        buttonRenderer.color = safe ? activatedColor : idleColor;
        ruinRenderer.color = hard ? ruinActivatedColor : ruinIdleColor;
        buttonFace.localPosition = faceStart + (safe ? Vector3.up * buttonPressDistance : Vector3.zero);
        ruinButtonFace.localPosition = ruinFaceStart + (hard ? Vector3.up * buttonPressDistance : Vector3.zero);
        if (label != null) label.text = safe ? "BACK TO NORMAL" : "SAVE MY LIFE";
        if (ruinLabel != null) ruinLabel.text = hard ? "BACK TO NORMAL" : "RUIN MY LIFE";
    }

    private float FramedSize(float size) => size * Mathf.Max(1f, (16f / 9f) / Mathf.Max(.1f, viewCamera.aspect));

    private IEnumerator MoveCamera(Vector2 center, float size, float duration)
    {
        Vector3 start = viewCamera.transform.position;
        float startSize = viewCamera.orthographicSize;
        Vector3 end = new Vector3(center.x, center.y, start.z);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            levelCamera.SetCinematicView(Vector3.Lerp(start, end, progress), Mathf.Lerp(startSize, size, progress));
            yield return null;
        }
        levelCamera.SetCinematicView(end, size);
    }

    private void RestorePresentation()
    {
        if (!ownsPresentation) return;
        ownsPresentation = false;
        if (levelCamera != null)
        {
            levelCamera.SetCinematicView(returnPosition, returnSize);
            levelCamera.EndCinematic();
        }
        if (playerController != null) playerController.enabled = controllerWasEnabled;
        Time.timeScale = previousTimeScale;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (changingRoute) FinishRouteChange();
        RestorePresentation();
    }

    private sealed class HazardGroup
    {
        public Transform Root { get; }
        private readonly Vector3 visiblePosition;
        private readonly Vector3 hiddenOffset;
        private readonly Collider2D[] colliders;
        private readonly SpriteRenderer[] renderers;
        private readonly int[] orders;
        private readonly bool[] rendererEnabled;
        private readonly bool growFromAnchor;
        private readonly Vector3 visibleScale;
        public Vector3 AnimationPosition { get; private set; }

        public HazardGroup(GameObject root, Vector3 offset, bool growFromAnchor = false)
        {
            Root = root.transform;
            visiblePosition = Root.localPosition;
            visibleScale = Root.localScale;
            AnimationPosition = visiblePosition;
            this.growFromAnchor = growFromAnchor;
            hiddenOffset = offset;
            colliders = root.GetComponentsInChildren<Collider2D>(true);
            renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            orders = new int[renderers.Length];
            rendererEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                orders[i] = renderers[i].sortingOrder;
                rendererEnabled[i] = renderers[i].enabled;
            }
        }

        public Vector3 Position(bool visible) => visiblePosition + (visible ? Vector3.zero : hiddenOffset);

        public void PrepareAnimation(bool willBeVisible)
        {
            foreach (Collider2D collider in colliders) collider.enabled = false;
            foreach (SpriteRenderer renderer in renderers) renderer.sortingOrder = -1;
            // A hidden group that stays hidden must not appear during the camera pan.
            Root.gameObject.SetActive(Root.gameObject.activeSelf || willBeVisible);
            MoveDuringAnimation(AnimationPosition);
        }

        public void MoveDuringAnimation(Vector3 position)
        {
            AnimationPosition = position;
            if (growFromAnchor)
            {
                // The ceiling root is anchored at the roof. Retract long teeth into
                // that surface without exposing their far ends above the thin roof.
                float amount = 1f - Mathf.Clamp01(Mathf.Abs(position.y - visiblePosition.y) /
                    Mathf.Max(.001f, Mathf.Abs(hiddenOffset.y)));
                Root.localPosition = visiblePosition;
                Root.localScale = new Vector3(visibleScale.x, visibleScale.y * amount, visibleScale.z);
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].enabled = rendererEnabled[i] && amount > .001f;
                return;
            }
            Root.localPosition = position;
            float buriedDistance = Mathf.Abs(position.y - visiblePosition.y);
            for (int i = 0; i < renderers.Length; i++)
            {
                // Stop drawing once fully buried, before emerging through the far side
                // of the shared floor or ceiling. Colliders stay disabled throughout.
                renderers[i].enabled = rendererEnabled[i] && buriedDistance < renderers[i].bounds.size.y;
            }
        }

        public void Finish(bool visible)
        {
            AnimationPosition = Position(visible);
            Root.localPosition = AnimationPosition;
            Root.localScale = visibleScale;
            foreach (Collider2D collider in colliders) collider.enabled = visible;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = orders[i];
                renderers[i].enabled = rendererEnabled[i];
            }
            Root.gameObject.SetActive(visible);
        }
    }
}
