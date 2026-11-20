using UnityEngine;
using UnityEngine.UI;

// Native UI folds, kept separate so the flap can open without moving the front.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AdmissionEnvelopeGraphic : MaskableGraphic
{
    public bool sealOnly;
    public bool front;
    public bool flapOnly;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (sealOnly)
        {
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float b = Mathf.PI / 2 + (i + 1) * Mathf.PI / 5;
                float ra = i % 2 == 0 ? .48f : .22f;
                float rb = i % 2 == 0 ? .22f : .48f;
                Triangle(vh, new Vector2(.5f, .5f), new Vector2(.5f + Mathf.Cos(a) * ra, .5f + Mathf.Sin(a) * ra),
                    new Vector2(.5f + Mathf.Cos(b) * rb, .5f + Mathf.Sin(b) * rb), new Color(1, .77f, .08f));
            }
            return;
        }
        var paper = new Color(1, .93f, .76f);
        var shade = new Color(.83f, .73f, .52f);
        if (flapOnly) { Triangle(vh, new Vector2(0, 0), new Vector2(.5f, 1), new Vector2(1, 0), shade); return; }
        if (!front)
        {
            Triangle(vh, Vector2.zero, Vector2.up, Vector2.one, shade);
            Triangle(vh, Vector2.zero, Vector2.one, Vector2.right, shade);
            return;
        }
        Triangle(vh, Vector2.zero, Vector2.up, new Vector2(.6f, .35f), paper);
        Triangle(vh, Vector2.right, new Vector2(.4f, .35f), Vector2.one, paper);
        Triangle(vh, Vector2.zero, new Vector2(.5f, .62f), Vector2.right, new Color(1, .96f, .85f));
    }
    void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        var r = rectTransform.rect; int start = vh.currentVertCount;
        foreach (var p in new[] { a, b, c })
            vh.AddVert(new Vector3(r.xMin + p.x * r.width, r.yMin + p.y * r.height), tint * color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }
}
