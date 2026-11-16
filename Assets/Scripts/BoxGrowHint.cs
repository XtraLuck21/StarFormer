using TMPro;
using UnityEngine;

// Place on a separate scene root so the guide stays fixed as the box moves.
public class BoxGrowHint : MonoBehaviour
{
    public enum HintMode { Grow, Shrink }

    [SerializeField] private bool showHoldLabel;
    [SerializeField] private HintMode mode = HintMode.Grow;
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody targetBody;
    [Header("Activation Area")]
    [Tooltip("Hint appears only while the controlled target box's center is inside this area. Independent of Frame Size.")]
    [SerializeField] private Vector2 activationAreaSize = new Vector2(3f, 4f);
    [SerializeField] private Vector2 activationAreaOffset = Vector2.zero;

    [Header("Appearance")]
    [SerializeField] private Vector2 frameSize = new Vector2(3.2f, 3.2f);
    [SerializeField, Min(0.01f)] private float dashLength = 0.18f;
    [SerializeField, Min(0.01f)] private float gapLength = 0.12f;
    [SerializeField, Min(0.001f)] private float lineThickness = 0.04f;
    [SerializeField] private Color frameColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private int sortingOrder = 20;

    private GameObject visuals;
    private Sprite dashSprite;
    private MobileFormControls mobileControls;
    private GameObject desktopPrompt;
    private RectTransform touchPrompt;
    private float holdDemoStart;

    private void Awake()
    {
        if (playerController == null || targetBody == null ||
            targetBody.Form != BodyForm.Box)
        {
            Debug.LogError("BoxGrowHint requires a player controller and a Box target.", this);
            enabled = false;
            return;
        }

        visuals = new GameObject("GrowHintVisuals");
        visuals.transform.SetParent(transform, false);
        visuals.SetActive(false);

        Texture2D texture = Texture2D.whiteTexture;
        dashSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width,
            0,
            SpriteMeshType.FullRect
        );
        dashSprite.name = "BoxGrowHintDash";

        Vector2 half = frameSize * 0.5f;
        AddEdge(new Vector2(-half.x, -half.y), Vector2.right, frameSize.x);
        AddEdge(new Vector2(-half.x, half.y), Vector2.right, frameSize.x);
        AddEdge(new Vector2(-half.x, -half.y), Vector2.up, frameSize.y);
        AddEdge(new Vector2(half.x, -half.y), Vector2.up, frameSize.y);
        if (mode == HintMode.Shrink)
        {
            AddShrinkPrompt(half);
        }
        else
        {
            AddGrowthPrompt(half);
        }

        mobileControls = playerController.GetComponent<MobileFormControls>();
        desktopPrompt = new GameObject("Desktop Input");
        desktopPrompt.transform.SetParent(visuals.transform, false);
        AddDesktopPrompt(half);
        if (mobileControls != null)
        {
            bool shrink = mode == HintMode.Shrink;
            var icon = shrink ? MobileControlGraphic.Symbol.Shrink : MobileControlGraphic.Symbol.Grow;
            Vector2 center = shrink ? Vector2.up * (half.y + 0.52f) : Vector2.zero;
            touchPrompt = MobileControlGraphic.CreateWorldHint(visuals.transform, icon, center, 0.95f, sortingOrder + 3);
            touchPrompt.gameObject.SetActive(false);
        }
    }

    private void AddGrowthPrompt(Vector2 half)
    {
        float unit = Mathf.Min(frameSize.x, frameSize.y) / 3.2f;
        float stroke = 0.055f * unit;

        // Four outward arrows communicate expansion without additional text.
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 start = new Vector2(x * half.x * 0.32f, y * half.y * 0.32f);
                Vector2 tip = new Vector2(x * half.x * 0.74f, y * half.y * 0.74f);
                Vector2 direction = (tip - start).normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                Vector2 headBase = tip - direction * (0.23f * unit);
                AddStroke("ArrowShaft", start, tip, stroke, sortingOrder + 1);
                AddStroke("ArrowHead", tip, headBase + perpendicular * (0.17f * unit), stroke, sortingOrder + 1);
                AddStroke("ArrowHead", tip, headBase - perpendicular * (0.17f * unit), stroke, sortingOrder + 1);
            }
        }
    }

    private void AddShrinkPrompt(Vector2 half)
    {
        // Keep the key readable even when the target outline is narrow.
        const float unit = 0.8f;
        const float stroke = 0.055f * unit;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 start = new Vector2(x * (half.x + 0.65f), y * (half.y + 0.65f));
                Vector2 tip = new Vector2(x * (half.x + 0.10f), y * (half.y + 0.10f));
                Vector2 direction = (tip - start).normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                Vector2 headBase = tip - direction * 0.18f;
                AddStroke("InwardArrow", start, tip, stroke, sortingOrder + 1);
                AddStroke("ArrowHead", tip, headBase + perpendicular * 0.13f, stroke, sortingOrder + 1);
                AddStroke("ArrowHead", tip, headBase - perpendicular * 0.13f, stroke, sortingOrder + 1);
            }
        }
    }

    private void AddDesktopPrompt(Vector2 half)
    {
        bool shrink = mode == HintMode.Shrink;
        float unit = shrink ? 0.8f : Mathf.Min(frameSize.x, frameSize.y) / 3.2f;
        Vector2 center = shrink ? Vector2.up * (half.y + 0.48f * unit) : Vector2.zero;
        Color dark = new Color(0.08f, 0.12f, 0.18f, 0.96f);
        AddRectangle("Input strip", center, new Vector2(2.18f, 0.78f) * unit, dark, sortingOrder + 2, desktopPrompt.transform);

        Vector2 key = center + Vector2.left * (0.67f * unit);
        AddRectangle("Key border", key, Vector2.one * (0.65f * unit), frameColor, sortingOrder + 3, desktopPrompt.transform);
        AddRectangle("Key face", key, Vector2.one * (0.55f * unit), dark, sortingOrder + 4, desktopPrompt.transform);
        AddLabel(shrink ? "K" : "J", key, 5.5f * unit);
        AddLabel("or", center, 3.4f * unit);
        MouseButtonHintIcon.Draw(desktopPrompt.transform, dashSprite, center + Vector2.right * (0.67f * unit),
            unit, shrink, frameColor, sortingOrder + 3);

        if (showHoldLabel)
        {
            Vector2 label = center + Vector2.up * (0.54f * unit);
            AddRectangle("Hold background", label, new Vector2(1.02f, 0.36f) * unit, dark, sortingOrder + 2, desktopPrompt.transform);
            AddLabel("HOLD", label, 3.4f * unit);
        }
    }

    private void AddLabel(string text, Vector2 center, float fontSize)
    {
        var label = new GameObject(text, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        label.transform.SetParent(desktopPrompt.transform, false);
        label.transform.localPosition = center;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.sortingOrder = sortingOrder + 5;
    }

    private void AddStroke(string objectName, Vector2 start, Vector2 end, float thickness, int order)
    {
        Vector2 delta = end - start;
        Transform segment = AddRectangle(objectName, (start + end) * 0.5f,
            new Vector2(delta.magnitude, thickness), frameColor, order);
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Transform AddRectangle(string objectName, Vector2 center, Vector2 size, Color color, int order, Transform parent = null)
    {
        GameObject rectangle = new GameObject(objectName);
        rectangle.transform.SetParent(parent != null ? parent : visuals.transform, false);
        rectangle.transform.localPosition = center;
        rectangle.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = rectangle.AddComponent<SpriteRenderer>();
        renderer.sprite = dashSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return rectangle.transform;
    }

    private void LateUpdate()
    {
        bool visible = playerController != null && targetBody != null &&
            targetBody.isActiveAndEnabled && !playerController.IsDead &&
            playerController.CurrentBody == targetBody && IsTargetInsideArea();

        // Once the box is fully below the opening, the shrink instruction is done.
        if (visible && mode == HintMode.Shrink && targetBody.BodyCollider != null)
        {
            float frameBottom = transform.TransformPoint(new Vector3(0f, -frameSize.y * 0.5f, 0f)).y;
            visible = targetBody.BodyCollider.bounds.max.y >= frameBottom;
        }

        if (visible && touchPrompt != null)
        {
            bool mobile = mobileControls != null && mobileControls.MobileMode;
            if (mobile && (!visuals.activeSelf || !touchPrompt.gameObject.activeSelf))
                holdDemoStart = Time.time;
            if (desktopPrompt.activeSelf == mobile) desktopPrompt.SetActive(!mobile);
            if (touchPrompt.gameObject.activeSelf != mobile) touchPrompt.gameObject.SetActive(mobile);
            if (mobile)
            {
                // Press in, hold briefly, then release once when this lesson appears.
                float age = Time.time - holdDemoStart;
                float press = Mathf.SmoothStep(0f, 1f, age / 0.18f) *
                    (1f - Mathf.SmoothStep(0f, 1f, (age - 0.9f) / 0.2f));
                touchPrompt.localScale = Vector3.one * (1f - 0.18f * press);
            }
        }
        if (visuals != null && visuals.activeSelf != visible)
        {
            visuals.SetActive(visible);
        }
    }

    private bool IsTargetInsideArea()
    {
        if (targetBody == null || targetBody.BodyCollider == null)
        {
            return false;
        }

        // Use the box center so a large box does not activate a distant hint early.
        Vector3 localCenter = transform.InverseTransformPoint(targetBody.BodyCollider.bounds.center);
        Vector2 relativeCenter = (Vector2)localCenter - activationAreaOffset;
        Vector2 halfArea = activationAreaSize * 0.5f;
        return Mathf.Abs(relativeCenter.x) <= halfArea.x &&
            Mathf.Abs(relativeCenter.y) <= halfArea.y;
    }

    private void AddEdge(Vector2 start, Vector2 direction, float length)
    {
        float step = Mathf.Max(0.02f, dashLength + gapLength);
        for (float offset = 0f; offset < length; offset += step)
        {
            float segmentLength = Mathf.Min(Mathf.Max(0.01f, dashLength), length - offset);
            GameObject dash = new GameObject("Dash");
            dash.transform.SetParent(visuals.transform, false);
            dash.transform.localPosition = start + direction * (offset + segmentLength * 0.5f);
            bool horizontal = direction.x != 0f;
            dash.transform.localScale = horizontal
                ? new Vector3(segmentLength, lineThickness, 1f)
                : new Vector3(lineThickness, segmentLength, 1f);

            SpriteRenderer renderer = dash.AddComponent<SpriteRenderer>();
            renderer.sprite = dashSprite;
            renderer.color = frameColor;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void OnDisable()
    {
        if (visuals != null)
        {
            visuals.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (visuals != null)
        {
            Destroy(visuals);
        }
        if (dashSprite != null)
        {
            Destroy(dashSprite);
        }
    }

    private void OnValidate()
    {
        activationAreaSize.x = Mathf.Max(0.01f, activationAreaSize.x);
        activationAreaSize.y = Mathf.Max(0.01f, activationAreaSize.y);
        frameSize.x = Mathf.Max(0.01f, frameSize.x);
        frameSize.y = Mathf.Max(0.01f, frameSize.y);
    }

    private void OnDrawGizmosSelected()
    {
        // Editor-only preview; the actual dashed frame is created at runtime.
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = frameColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(frameSize.x, frameSize.y, 0f));
        Gizmos.color = new Color(0f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(new Vector3(activationAreaOffset.x, activationAreaOffset.y, 0f),
            new Vector3(activationAreaSize.x, activationAreaSize.y, 0f));
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
