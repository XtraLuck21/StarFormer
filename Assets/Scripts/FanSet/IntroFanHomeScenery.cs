using System.Collections.Generic;
using UnityEngine;

/// <summary>Decorative home scenery using the same vector drawing and pastel palette as Level02.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class IntroFanHomeScenery : MonoBehaviour
{
    [SerializeField] private Vector2 backgroundMin = new Vector2(-28, -26);
    [SerializeField] private Vector2 backgroundMax = new Vector2(42, 20);
    [SerializeField, Range(.2f, 1f)] private float backgroundBrightness = 1f;
    [SerializeField] private bool showKitchen = true;
    [SerializeField] private bool showLivingRoom = true;
    [SerializeField] private bool showPlayCorner = true;

    private static readonly Color WallColor = new Color32(204, 216, 191, 255);
    private static readonly Color Ink = new Color32(49, 66, 73, 255);
    private static readonly Color Cream = new Color32(255, 234, 190, 255);
    private static readonly Color Honey = new Color32(244, 181, 69, 255);
    private static readonly Color Teal = new Color32(87, 151, 149, 255);
    private static readonly Color Glass = new Color32(177, 214, 213, 255);
    private static readonly Color Timber = new Color32(148, 104, 76, 255);
    private static readonly Color SoftInk = new Color32(134, 151, 144, 255);
    private static readonly Color PaleWood = new Color32(211, 183, 139, 255);
    private static readonly Color Peach = new Color32(225, 173, 143, 255);
    private static readonly Color Leaves = new Color32(139, 173, 142, 255);

    private readonly List<GameObject> drawings = new List<GameObject>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private Material material;
    private bool rebuild;

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;
    private void Update()
    {
        if (!rebuild) return;
        rebuild = false;
        Build();
    }

    private void Build()
    {
        Clear();
        material = new Material(Shader.Find("Sprites/Default"))
        {
            name = "IntroFan home scenery",
            mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave
        };
        BuildRoom();
        if (showKitchen) BuildKitchen();
        if (showLivingRoom) BuildLivingRoom();
        if (showPlayCorner) BuildPlayCorner();
    }

    private void Draw(SchoolSceneDrawing art, string label, int order)
    {
        GameObject obj = art.Build(label, transform, material, order, out Mesh mesh);
        // Preserve the warm palette in both Gamma and Linear rendering.
        Color[] colors = mesh.colors;
        float brightness = Mathf.Clamp01(backgroundBrightness);
        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        for (int i = 0; i < colors.Length; i++)
        {
            Color color = colors[i];
            color.r *= brightness;
            color.g *= brightness;
            color.b *= brightness;
            colors[i] = linear ? color.linear : color;
        }
        mesh.colors = colors;
        drawings.Add(obj);
        meshes.Add(mesh);
    }

    private void BuildRoom()
    {
        var wall = new SchoolSceneDrawing();
        wall.ShadedRect(backgroundMin.x, backgroundMin.y, backgroundMax.x, backgroundMax.y,
            WallColor, WallColor);
        Draw(wall, "Home - pale green walls", -160);
        DrawWallPanels(-4.15f, -3.02f, 4.9f, -1.66f, "Living room - green lower wall panels");
        DrawWallPanels(4.9f, -3.14f, 12.65f, -1.66f, "Landing - green lower wall panels", -2.9f);
        DrawWallPanels(5.95f, -7.04f, 25f, -5.32f, "Play corner - green lower wall panels");

        var trim = new SchoolSceneDrawing();
        trim.Rect(.7f, 2.98f, 9.7f, .08f, Cream);
        trim.Rect(.7f, 2.91f, 9.7f, .035f, PaleWood);
        trim.Rect(.7f, -.04f, 9.7f, .055f, PaleWood);
        trim.Rect(.7f, -2.94f, 9.7f, .12f, PaleWood);
        trim.Rect(15.6f, -6.9f, 19.2f, .14f, PaleWood);
        trim.Rect(15.6f, -6.79f, 19.2f, .035f, Cream);
        Draw(trim, "Home - picture rails and skirting", -155);

        var floor = new SchoolSceneDrawing();
        floor.ShadedRect(5.9f, backgroundMin.y, backgroundMax.x, -6.98f,
            new Color32(205, 173, 135, 255), new Color32(231, 208, 172, 255));
        Color joint = new Color32(201, 177, 144, 255);
        foreach (float y in new[] { -7.45f, -8.12f, -9.03f, -10.25f, -11.85f, -14f, -17f, -21f })
            floor.Rect((5.9f + backgroundMax.x) / 2, y, backgroundMax.x - 5.9f, .025f, joint);
        for (int i = 0; i < 10; i++)
        {
            float x = 6.4f + i * 2.5f;
            Vector2 start = new Vector2(x, -7);
            Vector2 end = new Vector2(16 + (x - 16) * 2.7f, backgroundMin.y);
            if (end.x < 5.9f)
                end = Vector2.Lerp(start, end, (5.9f - start.x) / (end.x - start.x));
            floor.Line(start, end, .02f, joint);
        }
        Draw(floor, "Home - warm wooden playroom floor", -154);
    }

    private void DrawWallPanels(float left, float bottom, float right, float top, string label, float? rightBottom = null)
    {
        var panels = new SchoolSceneDrawing();
        Color green = new Color32(174, 190, 158, 255);
        Color seam = new Color32(158, 176, 142, 255);
        float bottomRight = rightBottom ?? bottom;
        panels.Polygon(green, new Vector2(left, bottom), new Vector2(left, top),
            new Vector2(right, top), new Vector2(right, bottomRight));
        for (float x = left + .55f; x < right; x += .75f)
        {
            float baseY = Mathf.Lerp(bottom, bottomRight, (x - left) / (right - left));
            panels.Rect(x, (baseY + top) / 2, .014f, top - baseY, seam);
        }
        panels.Rect((left + right) / 2, top, right - left, .055f, seam);
        panels.Rect((left + right) / 2, top + .04f, right - left, .025f, new Color32(193, 205, 179, 255));
        Draw(panels, label, -157);
    }

    private void BuildKitchen()
    {
        var shelf = new SchoolSceneDrawing();
        shelf.Rect(-1.3f, 2.04f, 1.85f, .09f, Timber);
        shelf.Rect(-2.02f, 1.92f, .05f, .2f, PaleWood);
        shelf.Rect(-.58f, 1.92f, .05f, .2f, PaleWood);
        Mug(shelf, -1.9f, 2.18f, Peach);
        Mug(shelf, -1.47f, 2.18f, Cream);
        PotPlant(shelf, -.82f, 2.09f, .35f, false);
        Draw(shelf, "Kitchen - mugs and herb shelf", -137);

        Window(2.1f, 2.02f, 2.38f, 1.66f, true, "Kitchen - morning window");
        var clock = new SchoolSceneDrawing();
        clock.Circle(new Vector2(4.45f, 2.19f), .43f, PaleWood);
        clock.Circle(new Vector2(4.45f, 2.19f), .37f, Cream);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6;
            Vector2 axis = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            clock.Line(new Vector2(4.45f, 2.19f) + axis * .29f, new Vector2(4.45f, 2.19f) + axis * .32f, .018f, SoftInk);
        }
        clock.Line(new Vector2(4.45f, 2.19f), new Vector2(4.62f, 2.38f), .025f, Ink);
        clock.Line(new Vector2(4.45f, 2.19f), new Vector2(4.31f, 2.25f), .035f, Ink);
        clock.Circle(new Vector2(4.45f, 2.19f), .035f, Honey);
        Draw(clock, "Kitchen - round wall clock", -135);
    }

    private void BuildLivingRoom()
    {
        var lighting = new SchoolSceneDrawing();
        lighting.Rect(.15f, -.24f, .025f, .42f, PaleWood);
        lighting.Polygon(Honey, new Vector2(-.25f, -.65f), new Vector2(.55f, -.65f), new Vector2(.36f, -.41f), new Vector2(-.06f, -.41f));
        lighting.Rect(.15f, -.66f, .72f, .04f, Cream);
        Draw(lighting, "Living room - ceiling pendant", -138);

        var frames = new SchoolSceneDrawing();
        Picture(frames, -1.7f, -.86f, .75f, .83f, true);
        Picture(frames, .22f, -.87f, 1.48f, .71f, false);
        Picture(frames, 2.1f, -.82f, .72f, .87f, true);
        Draw(frames, "Living room - framed geometric family prints", -139);
        Window(8.22f, .65f, 3.6f, 2.85f, true, "Living room - tall sunlit window");
    }

    private void BuildPlayCorner()
    {
        Window(15.8f, -3.45f, 5.1f, 2.8f, true, "Play corner - large garden window");
        var rug = new SchoolSceneDrawing();
        rug.Polygon(new Color32(208, 160, 127, 255), new Vector2(6.15f, -9.7f), new Vector2(21.25f, -9.7f), new Vector2(18.7f, -7.04f), new Vector2(8.1f, -7.04f));
        rug.Polygon(new Color32(233, 202, 160, 255), new Vector2(6.55f, -9.48f), new Vector2(20.81f, -9.48f), new Vector2(18.53f, -7.2f), new Vector2(8.25f, -7.2f));
        rug.Polygon(new Color32(220, 190, 150, 255), new Vector2(7.07f, -9.19f), new Vector2(20.24f, -9.19f), new Vector2(18.32f, -7.4f), new Vector2(8.49f, -7.4f));
        for (int i = 0; i < 17; i++)
        {
            float x = 6.7f + i * .86f;
            rug.Line(new Vector2(x, -9.72f), new Vector2(x - .06f, -9.88f), .035f, Cream);
        }
        for (int i = 0; i < 13; i++)
            rug.Rect(8.8f + i * .76f, -9.04f, .32f, .025f, new Color32(235, 212, 173, 255));
        Draw(rug, "Play corner - woven ochre family rug", -150);

        var accents = new SchoolSceneDrawing();
        Picture(accents, 11.05f, -4.73f, 1.22f, 1.04f, false);
        accents.Line(new Vector2(9.35f, -4.82f), new Vector2(9.5f, -4.55f), .035f, PaleWood);
        accents.Line(new Vector2(9.5f, -4.55f), new Vector2(9.65f, -4.82f), .035f, PaleWood);
        accents.Circle(new Vector2(9.5f, -5.1f), .33f, PaleWood);
        accents.Circle(new Vector2(9.5f, -5.1f), .26f, Cream);
        accents.Star(new Vector2(9.5f, -5.1f), .12f, new Color32(179, 194, 160, 255));
        Draw(accents, "Play corner - framed wall art and hanging star", -138);
    }

    private void Window(float x, float y, float width, float height, bool curtains, string label)
    {
        var a = new SchoolSceneDrawing();
        Rounded(a, x + .06f, y - .07f, width + .18f, height + .2f, .07f, new Color32(206, 210, 184, 255));
        Rounded(a, x, y, width + .12f, height + .14f, .05f, PaleWood);
        a.Rect(x, y, width, height, Cream);
        a.ShadedRect(x - width / 2 + .1f, y - height / 2 + .1f, x + width / 2 - .1f, y + height / 2 - .1f,
            new Color32(208, 226, 205, 255), Glass);
        a.Circle(new Vector2(x + width * .22f, y + height * .2f), height * .11f, new Color32(250, 231, 177, 255));
        Ellipse(a, x - width * .2f, y + height * .24f, width * .22f, height * .07f, new Color32(233, 240, 221, 255));
        Ellipse(a, x - width * .13f, y + height * .28f, width * .14f, height * .07f, new Color32(233, 240, 221, 255));
        a.Rect(x, y - height * .31f, width - .2f, height * .18f, new Color32(161, 190, 166, 255));
        for (int i = 0; i < 5; i++)
        {
            float px = x - width * .38f + i * width * .19f;
            a.Circle(new Vector2(px, y - height * .19f), height * .115f, new Color32(169, 196, 172, 255));
        }
        a.Rect(x, y, .07f, height - .06f, Cream);
        a.Rect(x, y - height * .04f, width - .06f, .07f, Cream);
        a.Rect(x, y - height / 2 - .08f, width + .3f, .13f, Cream);
        if (curtains)
        {
            Color cloth = new Color32(225, 195, 140, 255);
            a.Rect(x, y + height / 2 + .12f, width + .48f, .045f, PaleWood);
            foreach (int side in new[] { -1, 1 })
            {
                float edge = x + side * width * .5f;
                a.Polygon(cloth, new Vector2(edge - .18f, y + height / 2 + .05f), new Vector2(edge + .18f, y + height / 2 + .05f),
                    new Vector2(edge + side * .1f, y - height * .35f), new Vector2(edge - side * .05f, y - height * .38f));
                a.Line(new Vector2(edge, y + height / 2), new Vector2(edge + side * .035f, y - height * .33f), .018f, new Color32(242, 216, 167, 255));
                a.Rect(edge + side * .025f, y - height * .15f, .2f, .05f, PaleWood);
            }
        }
        Draw(a, label, -145);
    }

    private static void Mug(SchoolSceneDrawing a, float x, float y, Color color)
    {
        Rounded(a, x, y, .2f, .24f, .045f, color);
        a.Arc(new Vector2(x + .13f, y + .02f), .09f, -95, 95, .032f, color);
        a.Rect(x, y + .115f, .16f, .018f, PaleWood);
    }

    private static void PotPlant(SchoolSceneDrawing a, float x, float floor, float size, bool tall)
    {
        float potHeight = size * .52f;
        a.Polygon(new Color32(201, 157, 124, 255), new Vector2(x - size * .27f, floor), new Vector2(x + size * .27f, floor),
            new Vector2(x + size * .34f, floor + potHeight), new Vector2(x - size * .34f, floor + potHeight));
        Rounded(a, x, floor + potHeight, size * .76f, size * .1f, size * .03f, PaleWood);
        float reach = tall ? size * 1.8f : size * 1.1f;
        a.Line(new Vector2(x, floor + potHeight), new Vector2(x, floor + reach), size * .035f, Leaves);
        for (int i = 0; i < 5; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            float baseY = floor + potHeight + size * .14f + i * size * .19f;
            float tipX = x + side * size * (.5f - i * .045f);
            float tipY = baseY + size * .46f;
            a.Polygon(Leaves, new Vector2(x, baseY), new Vector2(tipX, tipY - size * .04f),
                new Vector2(tipX - side * size * .05f, tipY + size * .12f), new Vector2(x + side * size * .12f, baseY + size * .25f));
        }
    }

    private static void Picture(SchoolSceneDrawing a, float x, float y, float width, float height, bool flower)
    {
        a.Rect(x + .025f, y - .035f, width + .08f, height + .08f, new Color32(218, 204, 174, 255));
        a.Rect(x, y, width, height, PaleWood);
        a.Rect(x, y, width - .1f, height - .1f, Cream);
        if (flower)
        {
            a.Line(new Vector2(x, y - height * .3f), new Vector2(x, y + height * .12f), .02f, Leaves);
            for (int i = 0; i < 5; i++)
            {
                float angle = (18 + i * 72) * Mathf.Deg2Rad;
                a.Circle(new Vector2(x + Mathf.Cos(angle) * width * .12f, y + height * .12f + Mathf.Sin(angle) * width * .12f), width * .1f, Peach);
            }
            a.Circle(new Vector2(x, y + height * .12f), width * .075f, Honey);
            a.Polygon(Leaves, new Vector2(x, y - height * .2f), new Vector2(x + width * .23f, y - height * .03f), new Vector2(x + width * .04f, y - height * .04f));
        }
        else
        {
            a.Circle(new Vector2(x + width * .21f, y + height * .17f), height * .12f, Honey);
            a.Polygon(new Color32(164, 191, 172, 255), new Vector2(x - width * .43f, y - height * .4f),
                new Vector2(x - width * .14f, y + height * .06f), new Vector2(x + width * .2f, y - height * .4f));
            a.Polygon(new Color32(139, 173, 160, 255), new Vector2(x - width * .1f, y - height * .4f),
                new Vector2(x + width * .16f, y - height * .02f), new Vector2(x + width * .43f, y - height * .4f));
        }
    }

    private static void Ellipse(SchoolSceneDrawing a, float x, float y, float width, float height, Color color)
    {
        var points = new Vector2[40];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / points.Length;
            points[i] = new Vector2(x + Mathf.Cos(angle) * width / 2, y + Mathf.Sin(angle) * height / 2);
        }
        a.Polygon(color, points);
    }

    private static void Rounded(SchoolSceneDrawing a, float x, float y, float width, float height, float radius, Color color)
    {
        radius = Mathf.Min(radius, width / 2, height / 2);
        var points = new Vector2[28];
        for (int corner = 0; corner < 4; corner++)
        {
            float cx = x + (corner == 0 || corner == 3 ? 1 : -1) * (width / 2 - radius);
            float cy = y + (corner < 2 ? 1 : -1) * (height / 2 - radius);
            for (int i = 0; i <= 6; i++)
            {
                float angle = (corner * 90 + i * 15) * Mathf.Deg2Rad;
                points[corner * 7 + i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
            }
        }
        a.Polygon(color, points);
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();
    private void Clear()
    {
        foreach (GameObject obj in drawings) if (obj != null) { obj.SetActive(false); SchoolSceneDrawing.Release(obj); }
        foreach (Mesh mesh in meshes) SchoolSceneDrawing.Release(mesh);
        drawings.Clear();
        meshes.Clear();
        SchoolSceneDrawing.Release(material);
        material = null;
    }
}
