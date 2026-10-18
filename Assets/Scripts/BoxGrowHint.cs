using UnityEngine;

// Place on a separate scene root so the guide stays fixed as the box moves.
public class BoxGrowHint : MonoBehaviour
{
    public enum HintMode { Grow, Shrink }

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

        // Draw the W as strokes so this icon needs no font or imported artwork.
        Vector2[] letter =
        {
            new Vector2(-0.23f, 0.20f) * unit,
            new Vector2(-0.115f, -0.20f) * unit,
            new Vector2(0f, 0.05f) * unit,
            new Vector2(0.115f, -0.20f) * unit,
            new Vector2(0.23f, 0.20f) * unit
        };
        AddKeyPrompt(letter, unit);
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

        Vector2[] letter =
        {
            new Vector2(0.20f, 0.20f) * unit,
            new Vector2(-0.20f, 0.20f) * unit,
            new Vector2(-0.20f, 0f) * unit,
            new Vector2(0.20f, 0f) * unit,
            new Vector2(0.20f, -0.20f) * unit,
            new Vector2(-0.20f, -0.20f) * unit
        };
        AddKeyPrompt(letter, unit);
    }

    private void AddKeyPrompt(Vector2[] letter, float unit)
    {
        float stroke = 0.055f * unit;
        float keyHalf = 0.38f * unit;
        AddRectangle("KeyBackground", Vector2.zero, Vector2.one * (keyHalf * 2f),
            new Color(0.08f, 0.12f, 0.18f, 0.92f), sortingOrder + 1);
        Vector2 bottomLeft = new Vector2(-keyHalf, -keyHalf);
        Vector2 bottomRight = new Vector2(keyHalf, -keyHalf);
        Vector2 topLeft = new Vector2(-keyHalf, keyHalf);
        Vector2 topRight = new Vector2(keyHalf, keyHalf);
        AddStroke("KeyBorder", bottomLeft, bottomRight, stroke, sortingOrder + 2);
        AddStroke("KeyBorder", bottomRight, topRight, stroke, sortingOrder + 2);
        AddStroke("KeyBorder", topRight, topLeft, stroke, sortingOrder + 2);
        AddStroke("KeyBorder", topLeft, bottomLeft, stroke, sortingOrder + 2);

        for (int i = 1; i < letter.Length; i++)
        {
            AddStroke("KeyLetter", letter[i - 1], letter[i], stroke, sortingOrder + 3);
        }
    }

    private void AddStroke(string objectName, Vector2 start, Vector2 end, float thickness, int order)
    {
        Vector2 delta = end - start;
        Transform segment = AddRectangle(objectName, (start + end) * 0.5f,
            new Vector2(delta.magnitude, thickness), frameColor, order);
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Transform AddRectangle(string objectName, Vector2 center, Vector2 size, Color color, int order)
    {
        GameObject rectangle = new GameObject(objectName);
        rectangle.transform.SetParent(visuals.transform, false);
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
