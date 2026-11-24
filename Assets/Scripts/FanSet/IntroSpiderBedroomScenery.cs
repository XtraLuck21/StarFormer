using System.Collections.Generic;
using UnityEngine;

/// <summary>Decorative children's bedroom, behind IntroSpider's original climbing route.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class IntroSpiderBedroomScenery : MonoBehaviour
{
    [SerializeField] private Vector2 backgroundMin = new Vector2(-40, -35);
    [SerializeField] private Vector2 backgroundMax = new Vector2(40, 30);
    [SerializeField, Range(.2f, 1f)] private float backgroundBrightness = 1f;
    [SerializeField] private bool showStudy = true;
    [SerializeField] private bool showToys = true;

    private static readonly Color WallColor = new Color32(204, 216, 191, 255);
    private static readonly Color Cream = new Color32(255, 234, 190, 255);
    private static readonly Color Mint = new Color32(172, 203, 184, 255);
    private static readonly Color Teal = new Color32(87, 151, 149, 255);
    private static readonly Color Honey = new Color32(244, 181, 69, 255);
    private static readonly Color Wood = new Color32(148, 104, 76, 255);
    private static readonly Color PaleWood = new Color32(211, 183, 139, 255);
    private static readonly Color Peach = new Color32(225, 173, 143, 255);
    private static readonly Color Blue = new Color32(150, 184, 194, 255);
    private static readonly Color Plum = new Color32(179, 164, 183, 255);
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
            name = "IntroSpider bedroom scenery",
            mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave
        };
        BuildRoom();
        BuildWallDecor();
        if (showStudy) BuildStudy();
        if (showToys) BuildToys();
    }

    private void Draw(SchoolSceneDrawing art, string label, int order = -140)
    {
        GameObject obj = art.Build(label, transform, material, order, out Mesh mesh);
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
        Draw(wall, "Bedroom - pale green walls", -165);
        DrawWallPanels(-13.1f, -4.9f, 3.72f, -1.4f, "Bedroom - green lower wall panels");
        DrawWallPanels(3.72f, -.3f, 12.47f, 1.2f, "Study - green lower wall panels");
        DrawWallPanels(3.72f, -4.9f, 12.98f, -3.3f, "Play nook - green lower wall panels");

        var panels = new SchoolSceneDrawing();
        panels.Rect(-4.6f, -4.79f, 16.8f, .15f, PaleWood);
        panels.Rect(-4.6f, -4.68f, 16.8f, .035f, Cream);
        panels.Rect(8.4f, -4.79f, 9f, .15f, PaleWood);
        panels.Rect(8.4f, -.21f, 7.8f, .13f, PaleWood);
        panels.Rect(-4.6f, 3.99f, 16.8f, .12f, Cream);
        Draw(panels, "Bedroom - wallpaper and wooden skirting", -160);

        var floor = new SchoolSceneDrawing();
        floor.ShadedRect(backgroundMin.x, backgroundMin.y, backgroundMax.x, -4.92f,
            new Color32(196, 164, 128, 255), new Color32(227, 203, 166, 255));
        Color joint = new Color32(193, 169, 136, 255);
        foreach (float y in new[] { -5.35f, -6.1f, -7.12f, -8.5f, -10.3f, -12.9f, -16.5f, -21.4f, -28f })
            floor.Rect((backgroundMin.x + backgroundMax.x) / 2, y, backgroundMax.x - backgroundMin.x, .025f, joint);
        for (int i = -6; i <= 6; i++)
        {
            Vector2 start = new Vector2(i * 3, -4.93f);
            Vector2 end = new Vector2(i * 3 * 2.6f, backgroundMin.y);
            if (end.x < backgroundMin.x) end = Vector2.Lerp(start, end, (backgroundMin.x - start.x) / (end.x - start.x));
            if (end.x > backgroundMax.x) end = Vector2.Lerp(start, end, (backgroundMax.x - start.x) / (end.x - start.x));
            floor.Line(start, end, .02f, joint);
        }
        Draw(floor, "Bedroom - warm wood floor", -162);

        var rugs = new SchoolSceneDrawing();
        Ellipse(rugs, -9.25f, -5.33f, 7.7f, 1.2f, new Color32(177, 196, 172, 255));
        Ellipse(rugs, -9.25f, -5.33f, 7.12f, .92f, new Color32(206, 214, 181, 255));
        Ellipse(rugs, 8.5f, -5.36f, 8.1f, 1.18f, new Color32(206, 166, 146, 255));
        Ellipse(rugs, 8.5f, -5.36f, 7.45f, .88f, new Color32(230, 201, 168, 255));
        Draw(rugs, "Bedroom - soft oval rugs", -158);
    }

    private void DrawWallPanels(float left, float bottom, float right, float top, string label)
    {
        var panels = new SchoolSceneDrawing();
        Color green = new Color32(174, 190, 158, 255);
        Color seam = new Color32(158, 176, 142, 255);
        panels.ShadedRect(left, bottom, right, top, green, green);
        for (float x = left + .55f; x < right; x += .75f)
            panels.Rect(x, (bottom + top) / 2, .014f, top - bottom, seam);
        panels.Rect((left + right) / 2, top, right - left, .055f, seam);
        panels.Rect((left + right) / 2, top + .04f, right - left, .025f, new Color32(193, 205, 179, 255));
        Draw(panels, label, -164);
    }

    private void BuildWallDecor()
    {
        Window(-9.5f, .55f, 4.15f, 3.05f, "Bedroom - garden window and curtains");
        Window(7.15f, 2.12f, 3.35f, 1.72f, "Study - daylight window");

        var prints = new SchoolSceneDrawing();
        Frame(prints, -5.66f, 1.66f, 1.35f, 1.75f);
        // A little space explorer poster: a rocket, moon and scattered stars.
        prints.Circle(new Vector2(-5.35f, 2.12f), .18f, Honey);
        Rounded(prints, -5.76f, 1.62f, .27f, .65f, .13f, Blue);
        prints.Polygon(Peach, V(-5.9f, 1.76f), V(-5.76f, 2.08f), V(-5.62f, 1.76f));
        prints.Polygon(Peach, V(-5.89f, 1.53f), V(-6.06f, 1.26f), V(-5.89f, 1.33f));
        prints.Polygon(Peach, V(-5.63f, 1.53f), V(-5.46f, 1.26f), V(-5.63f, 1.33f));
        prints.Circle(V(-5.76f, 1.7f), .075f, Cream);
        prints.Polygon(Honey, V(-5.85f, 1.3f), V(-5.67f, 1.3f), V(-5.76f, 1.07f));
        prints.Star(V(-6.02f, 2.18f), .07f, Honey);
        prints.Star(V(-5.3f, 1.36f), .07f, Mint);
        Frame(prints, -3.48f, 2.36f, 1.32f, 1.1f);
        prints.Circle(V(-3.17f, 2.56f), .13f, Honey);
        prints.Polygon(Mint, V(-4.02f, 1.92f), V(-3.6f, 2.55f), V(-3.08f, 1.92f));
        prints.Polygon(Teal, V(-3.63f, 1.92f), V(-3.23f, 2.38f), V(-2.95f, 1.92f));
        Draw(prints, "Bedroom - rocket and mountain pictures", -148);

        var shelf = new SchoolSceneDrawing();
        shelf.Rect(-3.5f, .63f, 2.35f, .12f, PaleWood);
        shelf.Rect(-4.32f, .44f, .06f, .32f, Wood);
        shelf.Rect(-2.68f, .44f, .06f, .32f, Wood);
        Books(shelf, -4.47f, .7f, 4, .17f, .63f);
        shelf.Circle(V(-2.93f, 1.08f), .24f, Blue);
        shelf.Arc(V(-2.93f, 1.08f), .28f, -90, 90, .025f, PaleWood);
        shelf.Rect(-2.93f, .77f, .04f, .18f, Wood);
        shelf.Rect(-2.93f, .71f, .34f, .045f, PaleWood);
        Draw(shelf, "Bedroom - small bookshelf and globe", -146);

        var garland = new SchoolSceneDrawing();
        Vector2 previous = V(-12.45f, 3.65f);
        for (int i = 1; i <= 30; i++)
        {
            float t = i / 30f;
            Vector2 point = V(Mathf.Lerp(-12.45f, -6.85f, t), 3.65f - Mathf.Sin(t * Mathf.PI) * .45f);
            garland.Line(previous, point, .018f, PaleWood);
            previous = point;
        }
        for (int i = 0; i < 8; i++)
        {
            float t = (i + .5f) / 8;
            float x = Mathf.Lerp(-12.45f, -6.85f, t), y = 3.65f - Mathf.Sin(t * Mathf.PI) * .45f;
            garland.Polygon(i % 3 == 0 ? Peach : i % 3 == 1 ? Mint : Honey,
                V(x - .18f, y), V(x + .18f, y), V(x, y - .32f));
        }
        // Keep stars away from the playable floating island and its aiming indicators.
        foreach (Vector2 p in new[] { V(-1.6f, 3.2f), V(.1f, 2.85f), V(1.65f, 3.35f), V(-7.05f, 1.95f) })
            garland.Star(p, .1f, new Color32(223, 213, 174, 255));
        Draw(garland, "Bedroom - bunting and quiet star decals", -152);
    }

    private void BuildStudy()
    {
        var board = new SchoolSceneDrawing();
        Frame(board, 9.35f, 2.48f, .92f, 1.16f);
        board.Rect(9.22f, 2.67f, .34f, .36f, Blue);
        board.Rect(9.48f, 2.3f, .35f, .39f, Peach);
        board.Circle(V(9.22f, 2.81f), .028f, Honey);
        board.Circle(V(9.48f, 2.45f), .028f, Teal);
        Draw(board, "Study - children's notes and drawings", -145);
    }

    private void BuildToys()
    {
        var storage = new SchoolSceneDrawing();
        Rounded(storage, 10.7f, -3.68f, 3.1f, 2.2f, .09f, PaleWood);
        storage.Rect(10.7f, -3.67f, 2.91f, 1.99f, new Color32(217, 204, 171, 255));
        storage.Rect(10.7f, -3.65f, 2.96f, .09f, PaleWood);
        storage.Rect(10.7f, -3.67f, .09f, 2f, PaleWood);
        Basket(storage, 9.97f, -4.2f, 1.14f, .72f);
        Basket(storage, 11.43f, -4.2f, 1.14f, .72f);
        Books(storage, 9.43f, -3.59f, 5, .18f, .65f);
        Teddy(storage, 11.42f, -3.59f, .85f);
        storage.Rect(10.7f, -2.53f, 3.29f, .15f, PaleWood);
        // A tiny wooden toy house on top of the storage unit.
        storage.Rect(10.05f, -2.12f, .71f, .65f, Peach);
        storage.Polygon(Wood, V(9.56f, -1.8f), V(10.54f, -1.8f), V(10.05f, -1.35f));
        storage.Rect(10.05f, -2.29f, .16f, .3f, Cream);
        storage.Rect(9.85f, -1.99f, .13f, .15f, Cream);
        storage.Rect(10.26f, -1.99f, .13f, .15f, Cream);
        Draw(storage, "Play nook - cubby baskets teddy and dollhouse", -140);

        var play = new SchoolSceneDrawing();
        Ellipse(play, 6.15f, -4.68f, 2.4f, .27f, new Color32(200, 183, 152, 255));
        Ellipse(play, 6.15f, -4.12f, 2.2f, 1.37f, new Color32(198, 155, 143, 255));
        Ellipse(play, 6.19f, -3.96f, 1.91f, 1.23f, Peach);
        play.Arc(V(6.15f, -3.96f), .44f, 200, 340, .025f, new Color32(202, 155, 133, 255));
        Teddy(play, 7.88f, -4.88f, 1.16f);
        // Stacking rings are flat decoration, deliberately darker than the silk targets.
        play.Rect(4.64f, -4.79f, .045f, .96f, Wood);
        foreach (int i in new[] { 0, 1, 2, 3 })
            Ellipse(play, 4.64f, -4.72f + i * .17f, .92f - i * .18f, .2f, i % 2 == 0 ? Mint : Honey);
        play.Circle(V(4.64f, -3.97f), .075f, Peach);
        Draw(play, "Play nook - beanbag teddy and stacking rings", -138);

        var train = new SchoolSceneDrawing();
        Rounded(train, 8.98f, -5.11f, 1.1f, .25f, .05f, Teal);
        train.Rect(8.66f, -4.89f, .32f, .39f, Honey);
        train.Rect(8.66f, -4.83f, .16f, .17f, Cream);
        train.Rect(9.23f, -4.86f, .12f, .22f, Wood);
        train.Rect(9.23f, -4.75f, .2f, .055f, Wood);
        foreach (float x in new[] { 8.65f, 9.29f })
        {
            train.Circle(V(x, -5.27f), .14f, Wood);
            train.Circle(V(x, -5.27f), .065f, PaleWood);
        }
        Draw(train, "Bedroom - wooden toy train on rug", -144);
    }

    private void Window(float x, float y, float width, float height, string label)
    {
        var a = new SchoolSceneDrawing();
        Rounded(a, x + .05f, y - .07f, width + .2f, height + .2f, .06f, new Color32(198, 209, 185, 255));
        Rounded(a, x, y, width + .13f, height + .13f, .045f, PaleWood);
        a.Rect(x, y, width, height, Cream);
        a.ShadedRect(x - width / 2 + .1f, y - height / 2 + .1f, x + width / 2 - .1f, y + height / 2 - .1f,
            new Color32(205, 224, 207, 255), new Color32(177, 214, 213, 255));
        a.Circle(V(x + width * .24f, y + height * .22f), height * .12f, new Color32(250, 230, 174, 255));
        Ellipse(a, x - width * .19f, y + height * .24f, width * .24f, height * .07f, Cream);
        Ellipse(a, x - width * .12f, y + height * .28f, width * .15f, height * .065f, Cream);
        a.Rect(x, y - height * .33f, width - .2f, height * .15f, new Color32(167, 194, 166, 255));
        for (int i = 0; i < 5; i++) a.Circle(V(x - width * .36f + i * width * .18f, y - height * .22f), height * .105f, Mint);
        a.Rect(x, y, .07f, height - .08f, Cream);
        a.Rect(x, y - height * .04f, width - .06f, .07f, Cream);
        a.Rect(x, y - height / 2 - .08f, width + .31f, .13f, Cream);
        a.Rect(x, y + height / 2 + .14f, width + .48f, .045f, Wood);
        foreach (int side in new[] { -1, 1 })
        {
            float edge = x + side * width / 2;
            a.Polygon(Peach, V(edge - .2f, y + height / 2 + .07f), V(edge + .2f, y + height / 2 + .07f),
                V(edge + .15f, y - height * .39f), V(edge - .15f, y - height * .39f));
            a.Line(V(edge, y + height / 2), V(edge, y - height * .35f), .02f, new Color32(241, 200, 163, 255));
            a.Rect(edge, y - height * .14f, .31f, .05f, PaleWood);
        }
        Draw(a, label, -154);
    }

    private static void Teddy(SchoolSceneDrawing a, float x, float floor, float size)
    {
        Color fur = new Color32(195, 159, 117, 255);
        Ellipse(a, x, floor + size * .35f, size * .55f, size * .62f, fur);
        a.Circle(V(x - size * .2f, floor + size * .86f), size * .13f, fur);
        a.Circle(V(x + size * .2f, floor + size * .86f), size * .13f, fur);
        a.Circle(V(x, floor + size * .69f), size * .29f, fur);
        Ellipse(a, x, floor + size * .31f, size * .34f, size * .36f, Cream);
        Ellipse(a, x - size * .28f, floor + size * .11f, size * .3f, size * .21f, fur);
        Ellipse(a, x + size * .28f, floor + size * .11f, size * .3f, size * .21f, fur);
        a.Circle(V(x - size * .1f, floor + size * .72f), size * .025f, Wood);
        a.Circle(V(x + size * .1f, floor + size * .72f), size * .025f, Wood);
        Ellipse(a, x, floor + size * .62f, size * .2f, size * .15f, Cream);
        a.Circle(V(x, floor + size * .65f), size * .025f, Wood);
        a.Polygon(Teal, V(x, floor + size * .47f), V(x - size * .14f, floor + size * .54f), V(x - size * .14f, floor + size * .41f));
        a.Polygon(Teal, V(x, floor + size * .47f), V(x + size * .14f, floor + size * .54f), V(x + size * .14f, floor + size * .41f));
    }

    private static void Books(SchoolSceneDrawing a, float left, float bottom, int count, float width, float height)
    {
        Color[] covers = { Teal, Peach, Cream, Blue, Plum, Honey };
        for (int i = 0; i < count; i++)
        {
            float h = height * (1 - i % 3 * .1f), x = left + i * (width + .035f) + width / 2;
            a.Rect(x, bottom + h / 2, width, h, covers[i % covers.Length]);
            a.Rect(x, bottom + h * .78f, width * .65f, .025f, Cream);
            a.Rect(x, bottom + .065f, width * .65f, .018f, PaleWood);
        }
    }

    private static void Basket(SchoolSceneDrawing a, float x, float y, float width, float height)
    {
        Rounded(a, x, y, width, height, .06f, new Color32(195, 169, 130, 255));
        for (int i = 1; i < 4; i++) a.Rect(x, y - height / 2 + i * height / 4, width - .06f, .018f, Cream);
        for (int i = 1; i < 6; i++) a.Rect(x - width / 2 + i * width / 6, y, .012f, height - .06f, PaleWood);
        Rounded(a, x, y + height * .15f, width * .25f, .065f, .025f, Wood);
    }

    private static void Frame(SchoolSceneDrawing a, float x, float y, float width, float height)
    {
        a.Rect(x + .035f, y - .04f, width + .08f, height + .08f, new Color32(207, 199, 169, 255));
        a.Rect(x, y, width, height, PaleWood);
        a.Rect(x, y, width - .11f, height - .11f, Cream);
    }

    private static Vector2 V(float x, float y) => new Vector2(x, y);

    private static void Ellipse(SchoolSceneDrawing a, float x, float y, float width, float height, Color color)
    {
        var points = new Vector2[40];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / points.Length;
            points[i] = V(x + Mathf.Cos(angle) * width / 2, y + Mathf.Sin(angle) * height / 2);
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
                points[corner * 7 + i] = V(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
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
