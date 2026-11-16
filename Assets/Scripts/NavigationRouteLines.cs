using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class NavigationRouteLines : MonoBehaviour
{
    [SerializeField] private Transform[] strokes;
    [SerializeField] private float strokeWidth = .035f;
    private Camera mapCamera;
    private float appliedWidth;

    private void Awake() => mapCamera = GetComponent<Camera>();

    private void LateUpdate()
    {
        int height = mapCamera.scaledPixelHeight;
        if (height <= 0) return;
        float worldUnitsPerPixel = 2f * mapCamera.orthographicSize / height;
        // The stroke meshes are unit rectangles with local Y across the line.
        float width = Mathf.Max(strokeWidth, 2f * worldUnitsPerPixel);
        if (Mathf.Approximately(width, appliedWidth)) return;

        foreach (var stroke in strokes)
        {
            Vector3 scale = stroke.localScale;
            scale.y = width;
            stroke.localScale = scale;
        }
        appliedWidth = width;
    }
}
