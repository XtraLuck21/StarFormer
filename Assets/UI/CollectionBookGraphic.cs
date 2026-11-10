using UnityEngine;
using UnityEngine.UI;

// Vector UI artwork: no copied textures or additional image assets.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CollectionBookGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        Color ink = new Color(.10f, .14f, .18f, 1f);
        Color yellow = new Color(1f, .82f, .15f, 1f);
        Quad(vh, r, .12f, .05f, .88f, .92f, ink);
        Quad(vh, r, .17f, .09f, .84f, .22f, new Color(1f, .97f, .85f));
        Quad(vh, r, .12f, .18f, .88f, .95f, ink);
        Quad(vh, r, .17f, .23f, .83f, .90f, yellow);
        Quad(vh, r, .23f, .23f, .26f, .90f, ink);
        // A five-point star on the front cover.
        Vector2 center = new Vector2(r.xMin + r.width * .55f, r.yMin + r.height * .57f);
        int first = vh.currentVertCount;
        vh.AddVert(center, ink * color, Vector2.zero);
        for (int i = 0; i < 10; i++)
        {
            float angle = (90f + i * 36f) * Mathf.Deg2Rad;
            float radius = r.width * (i % 2 == 0 ? .21f : .095f);
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, ink * color, Vector2.zero);
        }
        for (int i = 0; i < 10; i++) vh.AddTriangle(first, first + i + 1, first + (i + 1) % 10 + 1);
    }

    private void Quad(VertexHelper vh, Rect r, float x0, float y0, float x1, float y1, Color fill)
    {
        int first = vh.currentVertCount;
        Color tint = fill * color;
        vh.AddVert(new Vector3(r.xMin + x0 * r.width, r.yMin + y0 * r.height), tint, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + x0 * r.width, r.yMin + y1 * r.height), tint, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + x1 * r.width, r.yMin + y1 * r.height), tint, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + x1 * r.width, r.yMin + y0 * r.height), tint, Vector2.zero);
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first + 2, first + 3, first);
    }
}
