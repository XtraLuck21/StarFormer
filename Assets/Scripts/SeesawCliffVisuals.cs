using System.Collections.Generic;
using UnityEngine;

/// <summary>Decorative rock faces and cliff rims; never adds or modifies collision geometry.</summary>
[ExecuteAlways]
public sealed class SeesawCliffVisuals : MonoBehaviour
{
    [SerializeField] private BoxCollider2D centralIsland;
    private GameObject visuals;
    private Material material;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private bool rebuild;

    private void OnEnable() => Build();
    private void OnValidate() => rebuild = true;
    private void Update() { if (rebuild) Build(); }
    private void OnDisable() => Release();

    private void Build()
    {
        rebuild = false;
        Release();
        if (centralIsland == null) return;
        visuals = new GameObject("Cliff Rock Faces (visual only)") { hideFlags = HideFlags.HideAndDontSave };
        visuals.transform.SetParent(transform, false);
        material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
        Face(centralIsland, 1);
    }

    private void Face(BoxCollider2D surface, int seed)
    {
        Bounds b = surface.bounds;
        float x0 = b.min.x, x1 = b.max.x, top = b.max.y;
        const float depth = 14f;
        float taper = Mathf.Min(.6f, b.size.x * .14f);
        // The profile retreats into solid ground, leaving the actual gap completely open.
        Quad("Upper rock face", x0, x1, top - .08f, top - 2.6f,
            new Color(.23f, .29f, .35f), new Color(.12f, .16f, .22f), 1);
        Polygon("Deep tapered face", new[] {
            new Vector2(x0, top - 2.6f), new Vector2(x1, top - 2.6f),
            new Vector2(x1 - taper * .3f, top - 4f), new Vector2(x1 - taper, top - 5.2f),
            new Vector2(x1 - taper * .6f, top - 6.7f), new Vector2(x1 - taper, top - depth),
            new Vector2(x0 + taper, top - depth), new Vector2(x0 + taper * .6f, top - 6.7f),
            new Vector2(x0 + taper, top - 5.2f), new Vector2(x0 + taper * .3f, top - 4f)
        }, new Color(.1f, .14f, .2f), new Color(.025f, .04f, .07f), top - 2.6f, top - depth, 1);
        // Thin light rims identify the walkable shore; a dark undercut separates it from the rock.
        Quad("Stone rim", x0, x1, top - .015f, top - .13f,
            new Color(.58f, .66f, .7f), new Color(.36f, .44f, .5f), 2);
        Quad("Rim undercut", x0, x1, top - .13f, top - .3f,
            new Color(.045f, .065f, .09f), new Color(.08f, .11f, .15f), 2);
        for (int i = 0; i < 3; i++)
        {
            float y = top - .85f - i * .8f;
            float inset = .14f + i * .1f;
            Quad("Rock strata", x0 + inset, x1 - inset, y, y - .055f,
                new Color(.3f, .37f, .43f), new Color(.16f, .22f, .29f), 2);
        }
        // Narrow inward-facing facets accent the four edges adjoining the two pits.
        if (seed != 0) Edge(x0, top, 1f);
        if (seed != 2) Edge(x1, top, -1f);
    }

    private void Edge(float x, float top, float inward)
    {
        Polygon("Broken cliff edge", new[] {
            new Vector2(x, top - .3f), new Vector2(x + inward * .22f, top - .3f),
            new Vector2(x + inward * .12f, top - 1.1f), new Vector2(x + inward * .32f, top - 1.7f),
            new Vector2(x + inward * .22f, top - 2.7f), new Vector2(x, top - 3.5f)
        }, new Color(.38f, .46f, .52f), new Color(.08f, .12f, .18f), top, top - 3.5f, 2);
    }

    private void Quad(string name, float left, float right, float top, float bottom, Color light, Color dark, int order)
        => Polygon(name, new[] { new Vector2(left, top), new Vector2(right, top),
            new Vector2(right, bottom), new Vector2(left, bottom) }, light, dark, top, bottom, order);

    private void Polygon(string name, Vector2[] points, Color light, Color dark, float top, float bottom, int order)
    {
        var vertices = new Vector3[points.Length + 1];
        var colors = new Color[vertices.Length];
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in points) center += point;
        center /= points.Length;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 p = i == points.Length ? center : points[i];
            vertices[i] = visuals.transform.InverseTransformPoint(new Vector3(p.x, p.y, 0));
            colors[i] = Color.Lerp(dark, light, Mathf.InverseLerp(bottom, top, p.y));
        }
        var triangles = new int[points.Length * 3];
        for (int i = 0; i < points.Length; i++)
        {
            triangles[i * 3] = points.Length;
            triangles[i * 3 + 1] = i;
            triangles[i * 3 + 2] = (i + 1) % points.Length;
        }
        var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        meshes.Add(mesh);
        var part = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        part.transform.SetParent(visuals.transform, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
    }

    private void Release()
    {
        Dispose(visuals);
        foreach (Mesh mesh in meshes) Dispose(mesh);
        meshes.Clear();
        Dispose(material);
        visuals = null;
        material = null;
    }

    private static void Dispose(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
