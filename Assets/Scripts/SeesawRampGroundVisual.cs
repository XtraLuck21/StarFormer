using UnityEngine;

/// <summary>Fills the inaccessible underside of the exit slope into one clean ground silhouette.</summary>
[ExecuteAlways]
public sealed class SeesawRampGroundVisual : MonoBehaviour
{
    [SerializeField] private BoxCollider2D ramp;
    [SerializeField] private BoxCollider2D passageFloor;
    private GameObject drawing;
    private Mesh mesh;
    private Material material;
    private Matrix4x4 drawnRampMatrix;
    private float drawnBottom;
    private bool rebuildRequested;

    private void OnEnable() => rebuildRequested = true;
    private void OnValidate() => rebuildRequested = true;

    private void Update()
    {
        if (ramp == null || passageFloor == null) return;
        if (!rebuildRequested && drawnRampMatrix == ramp.transform.localToWorldMatrix &&
            Mathf.Approximately(drawnBottom, passageFloor.bounds.min.y)) return;
        rebuildRequested = false;
        Rebuild();
    }

    private void Rebuild()
    {
        Release();
        drawnRampMatrix = ramp.transform.localToWorldMatrix;
        drawnBottom = passageFloor.bounds.min.y;
        Vector2 high = ramp.transform.TransformPoint(ramp.offset + new Vector2(-ramp.size.x, ramp.size.y) * .5f);
        Vector2 low = ramp.transform.TransformPoint(ramp.offset + ramp.size * .5f);
        var vertices = new[] {
            transform.InverseTransformPoint(high), transform.InverseTransformPoint(low),
            transform.InverseTransformPoint(new Vector2(low.x, drawnBottom)),
            transform.InverseTransformPoint(new Vector2(high.x, drawnBottom))
        };
        mesh = new Mesh { name = "Seesaw Solid Ramp Underside", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices;
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        var source = ramp.GetComponent<SpriteRenderer>();
        material = new Material(Shader.Find("Sprites/Default")) {
            color = source.color, hideFlags = HideFlags.HideAndDontSave
        };
        drawing = new GameObject("Clean Solid Ramp Ground (visual only)") { hideFlags = HideFlags.HideAndDontSave };
        drawing.transform.SetParent(transform, false);
        drawing.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = drawing.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = source.sortingLayerID;
        // Keep the solid ground above the translucent return-floor visual so
        // that visual cannot leave a colored sliver in the slope junction.
        renderer.sortingOrder = source.sortingOrder + 1;
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();

    private void Release()
    {
        if (drawing != null) drawing.SetActive(false);
        Dispose(drawing);
        Dispose(mesh);
        Dispose(material);
        drawing = null;
        mesh = null;
        material = null;
    }

    private static void Dispose(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
