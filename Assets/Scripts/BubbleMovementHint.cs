using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Keep this on a scene root so the instruction stays attached to the teaching area.
[ExecuteAlways]
public class BubbleMovementHint : MonoBehaviour
{
    // Append modes to preserve existing scene values: Air=0, Water=1, ExitBubble=2.
    public enum HintMode { Air, Water, ExitBubble, StarMove, StarJump, ContinueRight, BubbleIntroduction, SpiderIntroduction, SpiderBlockedExit, SpiderControls }

    [SerializeField] private HintMode mode;
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody targetBody;
    [SerializeField] private bool previewInEditor = true;
    [Tooltip("Only used by Continue Right: show the arrow after this flag opens its gate.")]
    [SerializeField] private IntroFlagGate flagGate;

    [Header("Spider Q Reminder")]
    [SerializeField, Min(0.1f)] private float blockedKeyHintSeconds = 3f;
    [SerializeField] private Vector2 blockedKeyHintOffset = new Vector2(0f, 1.8f);

    [Header("Spider Introduction")]
    [Tooltip("Show the introduction after the spider first attaches to this structure or a child collider. Leave empty for possession-only visibility.")]
    [SerializeField] private Transform spiderIntroductionSurfaceRoot;
    [SerializeField] private Color spiderTextColor = new Color(0.13725491f, 0.27450982f, 0.3647059f, 1f);
    [SerializeField] private string spiderSubtitle = "No floor, no problem.";
    [SerializeField, Min(0.01f)] private float spiderIntroductionFadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float spiderSubtitleHoldSeconds = 4f;
    [SerializeField, Min(0.01f)] private float spiderSubtitleFadeSeconds = 0.75f;

    [Header("Activation Area")]
    [SerializeField] private Vector2 activationAreaSize = new Vector2(8f, 5f);
    [SerializeField] private Vector2 activationAreaOffset;

    [Header("Appearance")]
    [Tooltip("Show the form label above the movement keys. Disable when a Canvas provides the title.")]
    [SerializeField] private bool showIntroductionText = true;
    [SerializeField] private string waterDescription = "";
    [SerializeField] private string popDescription = "";
    [SerializeField] private Color popDescriptionColor = Color.white;
    [SerializeField, Min(0.1f)] private float iconScale = 1f;
    [Tooltip("Stroke thickness for the Continue Right arrow only.")]
    [SerializeField, Min(0.01f)] private float continueArrowWidth = 0.08f;
    [SerializeField] private Color inkColor = Color.white;
    [SerializeField] private Color keyBackground = new Color(0.08f, 0.12f, 0.18f, 0.95f);
    // Background uses this order; key outlines and letters use the next order.
    [SerializeField] private int sortingOrder = 0;

    private GameObject visuals;
    private Sprite strokeSprite;
    private bool rebuildRequested;
    private float blockedKeyHintRemaining;
    private PossessableBody reminderBody;
    private TextMeshPro introductionTitle;
    private TextMeshPro introductionSubtitle;
    private PossessableBody introductionBody;
    private float introductionSubtitleAge;
    private PossessableBody introductionSurfaceBody;
    private bool hasReachedIntroductionSurface;

    private void OnEnable() => rebuildRequested = true;
    private void OnValidate() => rebuildRequested = true;

    private void Update()
    {
        if (rebuildRequested)
        {
            rebuildRequested = false;
            BuildVisuals();
        }

        if (Application.IsPlaying(gameObject) && mode == HintMode.SpiderBlockedExit)
        {
            PossessableBody body = playerController != null ? playerController.CurrentBody : null;
            if (body != reminderBody || body == null || body.Form != BodyForm.Spider || playerController.IsDead)
                blockedKeyHintRemaining = 0f;
            reminderBody = body;
            blockedKeyHintRemaining = Mathf.Max(0f, blockedKeyHintRemaining - Time.deltaTime);
            if (body != null && body.Form == BodyForm.Spider && !playerController.IsDead &&
                Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                blockedKeyHintRemaining = blockedKeyHintSeconds;
        }
    }

    private void LateUpdate()
    {
        if (visuals == null) return;
        bool visible = !Application.IsPlaying(gameObject) && previewInEditor;
        if (Application.IsPlaying(gameObject))
        {
            if (mode == HintMode.SpiderControls)
            {
                PossessableBody body = playerController != null ? playerController.CurrentBody : null;
                visible = body != null && body.Form == BodyForm.Spider && body.isActiveAndEnabled &&
                    !playerController.IsDead;
            }
            else if (mode == HintMode.ContinueRight)
            {
                visible = flagGate != null && flagGate.IsOpen;
            }
            else if (mode == HintMode.SpiderIntroduction || mode == HintMode.SpiderBlockedExit)
            {
                PossessableBody body = playerController != null ? playerController.CurrentBody : null;
                visible = body != null && body.Form == BodyForm.Spider && body.isActiveAndEnabled &&
                    !playerController.IsDead &&
                    (mode != HintMode.SpiderBlockedExit || blockedKeyHintRemaining > 0f);
                if (mode == HintMode.SpiderIntroduction)
                {
                    visible = ShouldShowSpiderIntroduction(body);
                    UpdateSpiderSubtitle(body, visible);
                }
                if (visible && mode == HintMode.SpiderBlockedExit)
                {
                    // Keep feedback readable wherever the spider has climbed; never rotate it with the body.
                    Vector3 position = body.transform.position + (Vector3)blockedKeyHintOffset;
                    position.z = transform.position.z;
                    visuals.transform.position = position;
                }
            }
            else
            {
                bool starMode = mode == HintMode.StarMove || mode == HintMode.StarJump;
                // Pop guidance applies to whichever Bubble the player currently possesses.
                PossessableBody body = mode == HintMode.ExitBubble && playerController != null
                    ? playerController.CurrentBody : targetBody;
                visible = playerController != null && body != null &&
                    body.Form == (starMode ? BodyForm.Star : BodyForm.Bubble) &&
                    body.isActiveAndEnabled && !playerController.IsDead &&
                    playerController.CurrentBody == body &&
                    (starMode || body.IsInWater == (mode == HintMode.Water)) && IsInsideArea(body);
            }
        }
        if (visuals.activeSelf != visible) visuals.SetActive(visible);
    }

    private bool ShouldShowSpiderIntroduction(PossessableBody body)
    {
        if (body == null || body.Form != BodyForm.Spider || !body.isActiveAndEnabled ||
            playerController == null || playerController.IsDead)
        {
            introductionSurfaceBody = null;
            hasReachedIntroductionSurface = false;
            return false;
        }

        if (body != introductionSurfaceBody)
        {
            introductionSurfaceBody = body;
            hasReachedIntroductionSurface = false;
        }

        if (!hasReachedIntroductionSurface)
        {
            if (spiderIntroductionSurfaceRoot == null)
            {
                hasReachedIntroductionSurface = true;
            }
            else if (body.TryGetComponent(out HexagonSurfaceMovement movement) &&
                movement.IsAttached && movement.AttachedSurface != null)
            {
                Transform surface = movement.AttachedSurface.transform;
                hasReachedIntroductionSurface = surface == spiderIntroductionSurfaceRoot ||
                    surface.IsChildOf(spiderIntroductionSurfaceRoot);
            }
        }

        // Keep the introduction stable through corners and brief surface gaps.
        return hasReachedIntroductionSurface;
    }

    private void UpdateSpiderSubtitle(PossessableBody body, bool visible)
    {
        if (!visible)
        {
            introductionBody = null;
            introductionSubtitleAge = 0f;
            if (introductionTitle != null) introductionTitle.alpha = 0f;
            if (introductionSubtitle != null) introductionSubtitle.alpha = 0f;
            return;
        }

        if (body != introductionBody)
        {
            introductionBody = body;
            introductionSubtitleAge = 0f;
        }
        else
        {
            introductionSubtitleAge += Time.deltaTime;
        }

        float fadeInDuration = Mathf.Max(0.01f, spiderIntroductionFadeInSeconds);
        float fadeIn = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(introductionSubtitleAge / fadeInDuration));
        if (introductionTitle != null) introductionTitle.alpha = fadeIn;

        if (introductionSubtitle != null)
        {
            introductionSubtitle.alpha = fadeIn * (1f - Mathf.Clamp01(
                (introductionSubtitleAge - fadeInDuration - spiderSubtitleHoldSeconds) /
                Mathf.Max(0.01f, spiderSubtitleFadeSeconds)));
        }
    }

    private bool IsInsideArea(PossessableBody body)
    {
        if (body.BodyCollider == null) return false;
        Vector2 center = (Vector2)transform.InverseTransformPoint(body.BodyCollider.bounds.center);
        Vector2 distance = center - activationAreaOffset;
        return Mathf.Abs(distance.x) <= activationAreaSize.x * 0.5f &&
            Mathf.Abs(distance.y) <= activationAreaSize.y * 0.5f;
    }

    private void BuildVisuals()
    {
        ReleaseVisuals();
        visuals = new GameObject("BubbleMovementHintVisuals");
        visuals.hideFlags = HideFlags.HideAndDontSave;
        visuals.transform.SetParent(transform, false);
        visuals.transform.localScale = Vector3.one * Mathf.Max(0.1f, iconScale);
        visuals.SetActive(false);
        Texture2D texture = Texture2D.whiteTexture;
        strokeSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width, 0, SpriteMeshType.FullRect);
        strokeSprite.hideFlags = HideFlags.HideAndDontSave;

        if (mode == HintMode.BubbleIntroduction || mode == HintMode.SpiderIntroduction)
        {
            BuildBubbleIntroduction();
        }
        else if (mode == HintMode.ContinueRight)
        {
            Arrow(new Vector2(-0.6f, 0), new Vector2(0.6f, 0));
        }
        else if (mode == HintMode.Air || mode == HintMode.StarMove || mode == HintMode.SpiderControls)
        {
            AddKey('A', new Vector2(-0.5f, 0));
            AddKey('D', new Vector2(0.5f, 0));
            Arrow(new Vector2(-0.85f, 0), new Vector2(-1.25f, 0));
            Arrow(new Vector2(0.85f, 0), new Vector2(1.25f, 0));
        }
        else if (mode == HintMode.Water)
        {
            if (!string.IsNullOrWhiteSpace(waterDescription))
                AddHintText("WaterDescription", waterDescription, new Vector2(0, -1.1f), new Vector2(3f, 0.8f));
            AddKey('W', new Vector2(0, 0.65f));
            AddKey('A', new Vector2(-0.65f, 0));
            AddKey('S', Vector2.zero);
            AddKey('D', new Vector2(0.65f, 0));
            Arrow(new Vector2(-0.98f, 0), new Vector2(-1.18f, 0));
            Arrow(new Vector2(0.98f, 0), new Vector2(1.18f, 0));
            Arrow(new Vector2(0, 0.98f), new Vector2(0, 1.18f));
            Arrow(new Vector2(0, -0.33f), new Vector2(0, -0.53f));
        }
        else if (mode == HintMode.StarJump)
        {
            AddKeyFrame(Vector2.zero, new Vector2(1.9f, 0.5f));
            const string label = "SPACE";
            for (int i = 0; i < label.Length; i++)
                AddLetter(label[i], new Vector2((i - 2) * 0.35f, 0));

            // A short jump arc, drawn as strokes rather than font-dependent symbols.
            Vector2 previous = new Vector2(-0.85f, 0.4f);
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f;
                Vector2 next = new Vector2(Mathf.Lerp(-0.85f, 0.85f, t),
                    0.4f + 0.55f * 4f * t * (1f - t));
                if (i == 16) Arrow(previous, next);
                else Stroke(previous, next);
                previous = next;
            }
        }
        else
        {
            AddKey('Q', Vector2.zero);
            if (mode == HintMode.SpiderBlockedExit)
                AddHintText("BlockedKeyDescription", "Q can't be used here.", new Vector2(0, -0.65f), new Vector2(4f, 0.8f));
            else if (!string.IsNullOrWhiteSpace(popDescription))
                AddHintText("PopDescription", popDescription, new Vector2(0, -0.8f), new Vector2(3f, 0.8f), popDescriptionColor);
        }
    }

    private void BuildBubbleIntroduction()
    {
        bool spiderIntroduction = mode == HintMode.SpiderIntroduction;
        if (!spiderIntroduction)
        {
            AddKey('A', new Vector2(-0.5f, 0));
            AddKey('D', new Vector2(0.5f, 0));
            Arrow(new Vector2(-0.85f, 0), new Vector2(-1.25f, 0));
            Arrow(new Vector2(0.85f, 0), new Vector2(1.25f, 0));
        }
        if (showIntroductionText)
        {
            TextMeshPro title = AddHintText("FormIntroductionText", spiderIntroduction ? "You are a spider." : "You are a\nBubble",
                new Vector2(0, spiderIntroduction ? 0f : 0.8f),
                spiderIntroduction ? new Vector2(4.4f, 0.6f) : new Vector2(2.7f, 0.8f),
                spiderIntroduction ? spiderTextColor : (Color?)null);
            if (spiderIntroduction) introductionTitle = title;
            if (spiderIntroduction && !string.IsNullOrWhiteSpace(spiderSubtitle))
            {
                introductionSubtitle = AddHintText("SpiderSubtitle", spiderSubtitle,
                    new Vector2(0, -0.55f), new Vector2(5.5f, 0.6f), spiderTextColor);
                introductionSubtitle.fontSize = 8f / 3f;
            }
        }
    }

    private TextMeshPro AddHintText(string objectName, string message, Vector2 position, Vector2 size, Color? textColor = null)
    {
        GameObject label = new GameObject(objectName, typeof(RectTransform));
        label.hideFlags = HideFlags.HideAndDontSave;
        label.transform.SetParent(visuals.transform, false);
        label.transform.localPosition = position;
        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = message;
        text.fontSize = mode == HintMode.SpiderIntroduction ? 4f : 3f;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor ?? inkColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = size;
        text.renderer.sortingOrder = sortingOrder + 2;
        return text;
    }

    private void AddKey(char letter, Vector2 center)
    {
        AddKeyFrame(center, Vector2.one * 0.5f);
        AddLetter(letter, center);
    }

    private void AddKeyFrame(Vector2 center, Vector2 size)
    {
        Rectangle(center, size, keyBackground, sortingOrder);
        Vector2 half = size * 0.5f;
        Vector2[] corners = { new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y),
            new Vector2(half.x, half.y), new Vector2(-half.x, half.y) };
        for (int i = 0; i < 4; i++) Stroke(center + corners[i], center + corners[(i + 1) % 4]);
    }

    private void AddLetter(char letter, Vector2 center)
    {
        Vector2[] points;
        switch (letter)
        {
            case 'A':
                points = new[] { new Vector2(-0.13f, -0.15f), new Vector2(0, 0.15f), new Vector2(0.13f, -0.15f) };
                Stroke(center + new Vector2(-0.08f, -0.03f), center + new Vector2(0.08f, -0.03f));
                break;
            case 'D':
                points = new[] { new Vector2(-0.12f, -0.15f), new Vector2(-0.12f, 0.15f),
                    new Vector2(0.04f, 0.15f), new Vector2(0.13f, 0.07f), new Vector2(0.13f, -0.07f),
                    new Vector2(0.04f, -0.15f), new Vector2(-0.12f, -0.15f) };
                break;
            case 'Q':
                points = new[] { new Vector2(-0.07f, -0.15f), new Vector2(-0.13f, -0.07f),
                    new Vector2(-0.13f, 0.07f), new Vector2(-0.07f, 0.15f),
                    new Vector2(0.07f, 0.15f), new Vector2(0.13f, 0.07f),
                    new Vector2(0.13f, -0.07f), new Vector2(0.07f, -0.15f),
                    new Vector2(-0.07f, -0.15f) };
                Stroke(center + new Vector2(0.03f, -0.05f), center + new Vector2(0.17f, -0.19f));
                break;
            case 'E':
                points = new[] { new Vector2(0.13f, 0.15f), new Vector2(-0.13f, 0.15f),
                    new Vector2(-0.13f, -0.15f), new Vector2(0.13f, -0.15f) };
                Stroke(center + new Vector2(-0.13f, 0), center + new Vector2(0.1f, 0));
                break;
            case 'P':
                points = new[] { new Vector2(-0.12f, -0.15f), new Vector2(-0.12f, 0.15f),
                    new Vector2(0.1f, 0.15f), new Vector2(0.13f, 0.07f),
                    new Vector2(0.1f, 0), new Vector2(-0.12f, 0) };
                break;
            case 'C':
                points = new[] { new Vector2(0.13f, 0.15f), new Vector2(-0.08f, 0.15f),
                    new Vector2(-0.13f, 0.08f), new Vector2(-0.13f, -0.08f),
                    new Vector2(-0.08f, -0.15f), new Vector2(0.13f, -0.15f) };
                break;
            case 'W':
                points = new[] { new Vector2(-0.16f, 0.15f), new Vector2(-0.08f, -0.15f),
                    new Vector2(0, 0.04f), new Vector2(0.08f, -0.15f), new Vector2(0.16f, 0.15f) };
                break;
            default:
                points = new[] { new Vector2(0.13f, 0.15f), new Vector2(-0.13f, 0.15f),
                    new Vector2(-0.13f, 0), new Vector2(0.13f, 0),
                    new Vector2(0.13f, -0.15f), new Vector2(-0.13f, -0.15f) };
                break;
        }
        for (int i = 1; i < points.Length; i++) Stroke(center + points[i - 1], center + points[i]);
    }

    private void Arrow(Vector2 start, Vector2 tip)
    {
        Stroke(start, tip);
        Vector2 direction = (tip - start).normalized;
        Vector2 side = new Vector2(-direction.y, direction.x);
        float headLength = mode == HintMode.ContinueRight ? 0.3f : 0.1f;
        float headHalfWidth = mode == HintMode.ContinueRight ? 0.22f : 0.075f;
        Stroke(tip, tip - direction * headLength + side * headHalfWidth);
        Stroke(tip, tip - direction * headLength - side * headHalfWidth);
    }

    private void Stroke(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        Transform segment = Rectangle((start + end) * 0.5f,
            new Vector2(delta.magnitude, mode == HintMode.ContinueRight
                ? Mathf.Max(0.01f, continueArrowWidth) : 0.03f), inkColor, sortingOrder + 1);
        segment.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Transform Rectangle(Vector2 position, Vector2 size, Color color, int order)
    {
        GameObject part = new GameObject("HintStroke");
        part.hideFlags = HideFlags.HideAndDontSave;
        part.transform.SetParent(visuals.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = strokeSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return part.transform;
    }

    private void OnDisable()
    {
        blockedKeyHintRemaining = 0f;
        reminderBody = null;
        ReleaseVisuals();
    }
    private void OnDestroy() => ReleaseVisuals();

    private void ReleaseVisuals()
    {
        introductionTitle = null;
        introductionSubtitle = null;
        introductionBody = null;
        introductionSubtitleAge = 0f;
        introductionSurfaceBody = null;
        hasReachedIntroductionSurface = false;
        if (visuals != null)
        {
            visuals.SetActive(false);
            Release(visuals);
            visuals = null;
        }
        if (strokeSprite != null)
        {
            Release(strokeSprite);
            strokeSprite = null;
        }
    }

    private static void Release(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }

    private void OnDrawGizmosSelected()
    {
        // Spider prompts use surface/possession state, not this activation rectangle.
        if (mode == HintMode.SpiderIntroduction || mode == HintMode.SpiderBlockedExit ||
            mode == HintMode.SpiderControls) return;

        Matrix4x4 previous = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(activationAreaOffset, new Vector3(activationAreaSize.x, activationAreaSize.y, 0));
        Gizmos.matrix = previous;
        Gizmos.color = previousColor;
    }
}
