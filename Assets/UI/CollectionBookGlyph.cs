using UnityEngine;
using UnityEngine.UI;

// Small resolution-independent input symbols. All decoration is non-interactive.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CollectionBookGlyph : MaskableGraphic
{
    public enum Symbol { Key, MouseLeft, MouseRight, Aim, Lock, Check, Dot, Rule }
    public Symbol Kind;
    public Color Ink = new Color32(88, 57, 28, 255);
    private static readonly Color Gold = new Color32(247, 180, 31, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 P(float x, float y) => new Vector2(r.xMin + r.width * x, r.yMin + r.height * y);
        void Line(float x, float y, float a, float b, float width, Color fill)
        {
            Vector2 from = P(x, y), to = P(a, b);
            Vector2 side = Vector2.Perpendicular(to - from).normalized * width * .5f;
            int n = vh.currentVertCount;
            vh.AddVert(from - side, fill, Vector2.zero); vh.AddVert(from + side, fill, Vector2.zero);
            vh.AddVert(to + side, fill, Vector2.zero); vh.AddVert(to - side, fill, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        void Round(float x, float y, float w, float h, float radius, Color fill)
        {
            var box = new Rect(P(x, y), new Vector2(r.width * w, r.height * h));
            radius = Mathf.Min(radius, Mathf.Min(box.width, box.height) * .5f);
            int n = vh.currentVertCount;
            vh.AddVert(box.center, fill, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                float cx = corner == 0 || corner == 3 ? box.xMax - radius : box.xMin + radius;
                float cy = corner < 2 ? box.yMax - radius : box.yMin + radius;
                for (int step = 0; step <= 6; step++)
                {
                    float angle = (corner * 90 + step * 15) * Mathf.Deg2Rad;
                    vh.AddVert(new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius), fill, Vector2.zero);
                }
            }
            for (int i = 0; i < 28; i++) vh.AddTriangle(n, n + 1 + i, n + 1 + (i + 1) % 28);
        }
        float stroke = Mathf.Max(1.2f, Mathf.Min(r.width, r.height) * .055f);
        switch (Kind)
        {
            case Symbol.Key:
                Round(0, 0, 1, .94f, 7, new Color32(166, 127, 75, 255));
                Round(0, .09f, 1, .91f, 7, Ink);
                Round(.045f, .15f, .91f, .80f, 5, new Color32(255, 248, 227, 255));
                break;
            case Symbol.MouseLeft:
            case Symbol.MouseRight:
            case Symbol.Aim:
                Round(.12f, .02f, .76f, .96f, r.width * .36f, Ink);
                Round(.18f, .07f, .64f, .86f, r.width * .30f, new Color32(255, 248, 227, 255));
                if (Kind != Symbol.Aim)
                    Round(Kind == Symbol.MouseLeft ? .20f : .52f, .56f, .28f, .32f, 3, Gold);
                Line(.16f, .53f, .84f, .53f, stroke, Ink);
                Line(.50f, .53f, .50f, .94f, stroke, Ink);
                if (Kind == Symbol.Aim)
                {
                    Line(.0f, .30f, .0f, .7f, stroke, Ink);
                    Line(.0f, .7f, .06f, .6f, stroke, Ink);
                    Line(1, .3f, 1, .7f, stroke, Ink);
                    Line(1, .3f, .94f, .4f, stroke, Ink);
                }
                break;
            case Symbol.Lock:
                Round(.25f, .38f, .5f, .58f, r.width * .23f, Ink);
                Round(.34f, .42f, .32f, .42f, r.width * .14f, new Color32(245, 236, 219, 255));
                Round(.10f, .04f, .8f, .58f, 3, Ink);
                Round(.18f, .12f, .64f, .42f, 2, Gold);
                Line(.5f, .25f, .5f, .4f, stroke * 1.5f, Ink);
                break;
            case Symbol.Check:
                Line(.1f, .48f, .38f, .2f, stroke * 2, Gold);
                Line(.38f, .2f, .9f, .85f, stroke * 2, Gold);
                break;
            case Symbol.Dot:
                Round(.02f, .02f, .96f, .96f, r.width, Ink);
                Round(.14f, .14f, .72f, .72f, r.width, color);
                break;
            case Symbol.Rule:
                Line(.0f, .5f, .43f, .62f, 1.4f, color);
                Line(.43f, .62f, 1, .48f, 1.4f, color);
                break;
        }
    }
}
