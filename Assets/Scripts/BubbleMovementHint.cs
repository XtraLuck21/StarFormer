using UnityEngine;
using TMPro;

// Keep this on a scene root so the instruction stays attached to the teaching area.
[ExecuteAlways]
public class BubbleMovementHint : MonoBehaviour
{
    // Append modes to preserve existing scene values: Air=0, Water=1, ExitBubble=2.
    public enum HintMode { Air, Water, ExitBubble, StarMove, StarJump, ContinueRight, BubbleIntroduction }

    [SerializeField] private HintMode mode;
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private PossessableBody targetBody;
    [SerializeField] private bool previewInEditor = true;
    [Tooltip("Only used by Continue Right: show the arrow after this flag opens its gate.")]
    [SerializeField] private IntroFlagGate flagGate;

    [Header("Activation Area")]
    [SerializeField] private Vector2 activationAreaSize = new Vector2(8f, 5f);
    [SerializeField] private Vector2 activationAreaOffset;

    [Header("Appearance")]
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

    private void OnEnable() => rebuildRequested = true;
    private void OnValidate() => rebuildRequested = true;

    private void Update()
    {
        if (rebuildRequested)
        {
            rebuildRequested = false;
            BuildVisuals();
        }
    }

    private void LateUpdate()
    {
        if (visuals == null) return;
        bool visible = !Application.IsPlaying(gameObject) && previewInEditor;
        if (Application.IsPlaying(gameObject))
        {
            if (mode == HintMode.ContinueRight)
            {
                visible = flagGate != null && flagGate.IsOpen;
            }
            else
            {
                bool starMode = mode == HintMode.StarMove || mode == HintMode.StarJump;
                visible = playerController != null && targetBody != null &&
                    targetBody.Form == (starMode ? BodyForm.Star : BodyForm.Bubble) &&
                    targetBody.isActiveAndEnabled && !playerController.IsDead &&
                    playerController.CurrentBody == targetBody &&
                    (starMode || targetBody.IsInWater == (mode == HintMode.Water)) && IsInsideArea();
            }
        }
        if (visuals.activeSelf != visible) visuals.SetActive(visible);
    }

    private bool IsInsideArea()
    {
        if (targetBody.BodyCollider == null) return false;
        Vector2 center = (Vector2)transform.InverseTransformPoint(targetBody.BodyCollider.bounds.center);
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

        if (mode == HintMode.BubbleIntroduction)
        {
            BuildBubbleIntroduction();
        }
        else if (mode == HintMode.ContinueRight)
        {
            Arrow(new Vector2(-0.6f, 0), new Vector2(0.6f, 0));
        }
        else if (mode == HintMode.Air || mode == HintMode.StarMove)
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
            if (!string.IsNullOrWhiteSpace(popDescription))
                AddHintText("PopDescription", popDescription, new Vector2(0, -0.8f), new Vector2(3f, 0.8f), popDescriptionColor);
        }
    }

    private void BuildBubbleIntroduction()
    {
        AddKey('A', new Vector2(-0.5f, 0));
        AddKey('D', new Vector2(0.5f, 0));
        Arrow(new Vector2(-0.85f, 0), new Vector2(-1.25f, 0));
        Arrow(new Vector2(0.85f, 0), new Vector2(1.25f, 0));

        AddHintText("BubbleIntroductionText", "You are a\nBubble", new Vector2(0, 0.8f), new Vector2(2.7f, 0.8f));
    }

    private void AddHintText(string objectName, string message, Vector2 position, Vector2 size, Color? textColor = null)
    {
        GameObject label = new GameObject(objectName, typeof(RectTransform));
        label.hideFlags = HideFlags.HideAndDontSave;
        label.transform.SetParent(visuals.transform, false);
        label.transform.localPosition = position;
        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = message;
        text.fontSize = 3f;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor ?? inkColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = size;
        text.renderer.sortingOrder = sortingOrder + 2;
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

    private void OnDisable() => ReleaseVisuals();
    private void OnDestroy() => ReleaseVisuals();

    private void ReleaseVisuals()
    {
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
        Matrix4x4 previous = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(activationAreaOffset, new Vector3(activationAreaSize.x, activationAreaSize.y, 0));
        Gizmos.matrix = previous;
        Gizmos.color = previousColor;
    }
}
