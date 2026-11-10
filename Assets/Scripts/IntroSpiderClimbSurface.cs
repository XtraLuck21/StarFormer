using UnityEngine;

// A single visual and collision outline avoids exposed seams between the two slopes.
[ExecuteAlways]
public class IntroSpiderClimbSurface : MonoBehaviour
{
    [SerializeField] private Vector2[] outline;
    [SerializeField] private Color surfaceColor = new Color(0.6803133f, 0.9811321f, 0.69119895f, 1f);
    private GameObject generated;
    private Mesh mesh;
    private Material material;
    private bool rebuild;

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;
    private void Update()
    {
        if (!rebuild) return;
        rebuild = false;
        Release();
        if (outline == null || outline.Length != 7) return;

        generated = new GameObject("Continuous Climbing Surface", typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D));
        generated.hideFlags = HideFlags.HideAndDontSave;
        generated.layer = gameObject.layer;
        generated.transform.SetParent(transform, false);
        var vertices = new Vector3[outline.Length];
        var colors = new Color[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            vertices[i] = outline[i];
            colors[i] = surfaceColor;
        }
        mesh = new Mesh { name = "IntroSpider Joined Slopes", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = new Vector2[outline.Length];
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 6, 3, 3, 5, 4, 3, 6, 5 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        generated.GetComponent<MeshFilter>().sharedMesh = mesh;
        material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
        material.mainTexture = Texture2D.whiteTexture;
        generated.GetComponent<MeshRenderer>().sharedMaterial = material;
        generated.GetComponent<PolygonCollider2D>().SetPath(0, outline);
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();
    private void Release()
    {
        if (generated != null)
        {
            generated.SetActive(false);
            DestroyGenerated(generated);
        }
        if (mesh != null) DestroyGenerated(mesh);
        if (material != null) DestroyGenerated(material);
        generated = null;
        mesh = null;
        material = null;
    }

    private static void DestroyGenerated(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
