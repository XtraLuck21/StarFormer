using UnityEngine;

[ExecuteAlways]
public class BouncyBallMovementHint : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerPossessionController playerController;

    [SerializeField]
    private PossessableBody targetBody;

    [SerializeField]
    private bool previewInEditor = true;

    [Header("Activation Area")]
    [SerializeField]
    private Vector2 activationAreaSize = new Vector2(8f, 5f);

    [SerializeField]
    private Vector2 activationAreaOffset;

    [Header("Appearance")]
    [SerializeField, Min(0.1f)]
    private float iconScale = 1f;

    [SerializeField]
    private Color inkColor = Color.white;

    [SerializeField]
    private Color keyBackground =
        new Color(0.08f, 0.12f, 0.18f, 0.95f);

    [SerializeField]
    private int sortingOrder = 0;

    private GameObject visuals;
    private Sprite strokeSprite;
    private bool rebuildRequested;

    private void OnEnable()
    {
        rebuildRequested = true;
    }

    private void OnValidate()
    {
        rebuildRequested = true;
    }

    private void Update()
    {
        if (!rebuildRequested)
        {
            return;
        }

        rebuildRequested = false;
        BuildVisuals();
    }

    private void LateUpdate()
    {
        if (visuals == null)
        {
            return;
        }

        bool visible;

        if (!UnityEngine.Application.IsPlaying(gameObject))
        {
            visible = previewInEditor;
        }
        else
        {
            visible =
                playerController != null &&
                targetBody != null &&
                targetBody.Form == BodyForm.BouncyBall &&
                targetBody.isActiveAndEnabled &&
                !playerController.IsDead &&
                playerController.CurrentBody == targetBody &&
                IsInsideArea();
        }

        if (visuals.activeSelf != visible)
        {
            visuals.SetActive(visible);
        }
    }

    private bool IsInsideArea()
    {
        if (targetBody == null || targetBody.BodyCollider == null)
        {
            return false;
        }

        Vector2 localCenter = transform.InverseTransformPoint(
            targetBody.BodyCollider.bounds.center
        );

        Vector2 distance = localCenter - activationAreaOffset;

        return
            Mathf.Abs(distance.x) <= activationAreaSize.x * 0.5f &&
            Mathf.Abs(distance.y) <= activationAreaSize.y * 0.5f;
    }

    private void BuildVisuals()
    {
        ReleaseVisuals();

        visuals = new GameObject("BouncyBallMovementHintVisuals");
        visuals.hideFlags = HideFlags.HideAndDontSave;
        visuals.transform.SetParent(transform, false);
        visuals.transform.localScale =
            Vector3.one * Mathf.Max(0.1f, iconScale);

        visuals.SetActive(false);

        Texture2D texture = Texture2D.whiteTexture;

        strokeSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width,
            0,
            SpriteMeshType.FullRect
        );

        strokeSprite.hideFlags = HideFlags.HideAndDontSave;

        AddKey('A', new Vector2(-0.5f, 0));
        AddKey('D', new Vector2(0.5f, 0));

        Arrow(
            new Vector2(-0.85f, 0),
            new Vector2(-1.25f, 0)
        );

        Arrow(
            new Vector2(0.85f, 0),
            new Vector2(1.25f, 0)
        );
    }

    private void AddKey(char letter, Vector2 center)
    {
        Vector2 size = Vector2.one * 0.5f;

        Rectangle(center, size, keyBackground, sortingOrder);

        Vector2 half = size * 0.5f;

        Vector2[] corners =
        {
            new Vector2(-half.x, -half.y),
            new Vector2(half.x, -half.y),
            new Vector2(half.x, half.y),
            new Vector2(-half.x, half.y)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Stroke(
                center + corners[i],
                center + corners[(i + 1) % corners.Length]
            );
        }

        AddLetter(letter, center);
    }

    private void AddLetter(char letter, Vector2 center)
    {
        Vector2[] points;

        switch (letter)
        {
            case 'A':
                points = new[]
                {
                    new Vector2(-0.13f, -0.15f),
                    new Vector2(0, 0.15f),
                    new Vector2(0.13f, -0.15f)
                };

                Stroke(
                    center + new Vector2(-0.08f, -0.03f),
                    center + new Vector2(0.08f, -0.03f)
                );
                break;

            case 'D':
                points = new[]
                {
                    new Vector2(-0.12f, -0.15f),
                    new Vector2(-0.12f, 0.15f),
                    new Vector2(0.04f, 0.15f),
                    new Vector2(0.13f, 0.07f),
                    new Vector2(0.13f, -0.07f),
                    new Vector2(0.04f, -0.15f),
                    new Vector2(-0.12f, -0.15f)
                };
                break;

            default:
                return;
        }

        for (int i = 1; i < points.Length; i++)
        {
            Stroke(
                center + points[i - 1],
                center + points[i]
            );
        }
    }

    private void Arrow(Vector2 start, Vector2 tip)
    {
        Stroke(start, tip);

        Vector2 direction = (tip - start).normalized;
        Vector2 side = new Vector2(-direction.y, direction.x);

        Stroke(
            tip,
            tip - direction * 0.1f + side * 0.075f
        );

        Stroke(
            tip,
            tip - direction * 0.1f - side * 0.075f
        );
    }

    private void Stroke(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;

        Transform segment = Rectangle(
            (start + end) * 0.5f,
            new Vector2(delta.magnitude, 0.03f),
            inkColor,
            sortingOrder + 1
        );

        segment.localRotation = Quaternion.Euler(
            0,
            0,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
        );
    }

    private Transform Rectangle(
        Vector2 position,
        Vector2 size,
        Color color,
        int order)
    {
        GameObject part = new GameObject("HintStroke");
        part.hideFlags = HideFlags.HideAndDontSave;
        part.transform.SetParent(visuals.transform, false);

        part.transform.localPosition =
            new Vector3(position.x, position.y, 0);

        part.transform.localScale =
            new Vector3(size.x, size.y, 1);

        SpriteRenderer spriteRenderer =
            part.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = strokeSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = order;

        return part.transform;
    }

    private void OnDisable()
    {
        ReleaseVisuals();
    }

    private void OnDestroy()
    {
        ReleaseVisuals();
    }

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

    private static void Release(UnityEngine.Object item)
    {
        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(item);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(item);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            new Vector3(
                activationAreaOffset.x,
                activationAreaOffset.y,
                0
            ),
            new Vector3(
                activationAreaSize.x,
                activationAreaSize.y,
                0
            )
        );

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}