using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class MenuUIPlatform : MonoBehaviour
{
    [SerializeField] private RectTransform targetUI;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float thickness = 0.12f;

    private BoxCollider2D platformCollider;
    private readonly Vector3[] corners = new Vector3[4];

    private void Awake()
    {
        platformCollider = GetComponent<BoxCollider2D>();
        platformCollider.isTrigger = false;
        platformCollider.usedByEffector = true;

        PlatformEffector2D effector =
            GetComponent<PlatformEffector2D>();

        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 180f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;

        if (targetUI == null || worldCamera == null)
        {
            Debug.LogError(
                "Assign the UI target and menu camera.",
                this
            );

            platformCollider.enabled = false;
            enabled = false;
        }
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        AlignPlatform();
    }

    private void LateUpdate()
    {
        AlignPlatform();
    }

    private void AlignPlatform()
    {
        targetUI.GetWorldCorners(corners);

        // Convert UI positions using the menu canvas camera.
        Vector2 leftScreen =
            RectTransformUtility.WorldToScreenPoint(worldCamera, corners[1]);

        Vector2 rightScreen =
            RectTransformUtility.WorldToScreenPoint(worldCamera, corners[2]);

        // The menu camera faces the XY plane from Z = -10.
        float depth = -worldCamera.transform.position.z;

        Vector3 left = worldCamera.ScreenToWorldPoint(
            new Vector3(leftScreen.x, leftScreen.y, depth)
        );

        Vector3 right = worldCamera.ScreenToWorldPoint(
            new Vector3(rightScreen.x, rightScreen.y, depth)
        );

        float height = Mathf.Max(0.01f, thickness);

        transform.position = new Vector3(
            (left.x + right.x) * 0.5f,
            left.y - height * 0.5f,
            0f
        );

        platformCollider.offset = Vector2.zero;
        platformCollider.size = new Vector2(
            Mathf.Max(0.01f, right.x - left.x),
            height
        );
    }
}