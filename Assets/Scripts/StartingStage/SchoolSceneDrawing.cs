using System.Collections.Generic;
using UnityEngine;

// Small vector shapes keep the school scenery consistent with the game's geometric art.
internal sealed class SchoolSceneDrawing
{
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> triangles = new List<int>();

    public void Polygon(Color color, params Vector2[] points)
    {
        int start = vertices.Count;
        foreach (Vector2 p in points) { vertices.Add(p); colors.Add(color); }
        for (int i = 1; i < points.Length - 1; i++)
        { triangles.Add(start); triangles.Add(start + i); triangles.Add(start + i + 1); }
    }
    public void Rect(float x, float y, float width, float height, Color color)
    {
        Polygon(color, new Vector2(x-width/2,y-height/2), new Vector2(x-width/2,y+height/2),
            new Vector2(x+width/2,y+height/2), new Vector2(x+width/2,y-height/2));
    }
    public void ShadedRect(float left, float bottom, float right, float top, Color lower, Color upper)
    {
        Polygon(lower, new Vector2(left,bottom), new Vector2(left,top),
            new Vector2(right,top), new Vector2(right,bottom));
        colors[colors.Count - 3] = upper;
        colors[colors.Count - 2] = upper;
    }

    public void Line(Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 d = to-from, n = new Vector2(-d.y,d.x).normalized * width/2;
        Polygon(color, from-n, from+n, to+n, to-n);
    }
    public void Circle(Vector2 center, float radius, Color color)
    {
        for (int i=0;i<40;i++)
        {
            float a=i*Mathf.PI/20, b=(i+1)*Mathf.PI/20;
            Polygon(color, center, center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);
        }
    }
    public void Arc(Vector2 center, float radius, float from, float to, float width, Color color)
    {
        for(int i=0;i<32;i++)
        {
            float a=Mathf.Lerp(from,to,i/32f)*Mathf.Deg2Rad, b=Mathf.Lerp(from,to,(i+1)/32f)*Mathf.Deg2Rad;
            Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,color);
        }
    }
    public void Star(Vector2 center, float size, Color color)
    {
        for(int i=0;i<10;i++)
        {
            float a=(90+i*36)*Mathf.Deg2Rad, b=(90+(i+1)*36)*Mathf.Deg2Rad;
            Polygon(color,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*(i%2==0?1f:.45f),
                center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size*(i%2==0?.45f:1f));
        }
    }
    public GameObject Build(string name, Transform parent, Material material, int order, out Mesh mesh)
    {
        var root = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)) { hideFlags=HideFlags.HideAndDontSave };
        root.transform.SetParent(parent,false);
        mesh=new Mesh { name=name, hideFlags=HideFlags.HideAndDontSave };
        mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles,0);
        mesh.uv=new Vector2[vertices.Count]; mesh.RecalculateBounds();
        root.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=root.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.sortingOrder=order;
        return root;
    }
    public static void Release(Object item)
    {
        if(item==null)return;
        if(Application.isPlaying)Object.Destroy(item);else Object.DestroyImmediate(item);
    }
}
