using UnityEngine;

internal sealed class SchoolRainStreaks
{
    private static readonly Color Water = WaterColor();

    private struct Drop
    {
        public Vector2 Start, Velocity;
        public float Age, Length, Opacity;
    }

    private readonly SchoolSceneDrawing art = new SchoolSceneDrawing();
    private readonly Mesh mesh;
    private readonly MeshRenderer renderer;
    private readonly Drop[] drops;
    private readonly Rect area;

    public SchoolRainStreaks(SchoolMorningScenery scenery, string name, int order, Rect area, int count, int seed)
    {
        this.area = area;
        drops = new Drop[count];
        renderer = scenery.Draw(art, name, order, out mesh).GetComponent<MeshRenderer>();
        mesh.MarkDynamic();
        var random = new System.Random(seed);
        for (int i = 0; i < drops.Length; i++)
        {
            ref Drop drop = ref drops[i];
            drop.Start = new Vector2(Range(random, area.xMin, area.xMax), area.yMax);
            drop.Velocity = Fall(random) * Range(random, .72f, 1.15f);
            drop.Age = Range(random, 0f, area.height / -drop.Velocity.y);
            drop.Length = Range(random, .25f, .55f);
            drop.Opacity = Range(random, .24f, .48f);
        }
    }

    public void Update(float deltaTime, float opacity = 1f)
    {
        renderer.enabled = opacity > 0f;
        if (!renderer.enabled) return;
        art.Clear();
        for (int i = 0; i < drops.Length; i++)
        {
            ref Drop drop = ref drops[i];
            drop.Age = Mathf.Repeat(drop.Age + deltaTime, area.height / -drop.Velocity.y);
            Vector2 tip = drop.Start + drop.Velocity * drop.Age;
            tip.x = area.xMin + Mathf.Repeat(tip.x - area.xMin, area.width);
            float edgeX = Mathf.Min(tip.x - area.xMin, area.xMax - tip.x);
            float edgeY = Mathf.Min(tip.y - area.yMin, area.yMax - tip.y);
            art.Line(tip, tip - drop.Velocity.normalized * drop.Length, .034f,
                Tint(opacity * drop.Opacity * Mathf.Clamp01(Mathf.Min(edgeX, edgeY) / 2f)));
        }
        art.Upload(mesh);
    }

    public static Vector2 Fall(System.Random random) => new Vector2(Range(random, -2.05f, -.85f), -7.6f);

    public static Color Tint(float opacity) => new Color(Water.r, Water.g, Water.b, opacity);

    private static Color WaterColor()
    {
        Color water = new Color32(187, 215, 224, 255);
        return QualitySettings.activeColorSpace == ColorSpace.Linear ? water.linear : water;
    }

    private static float Range(System.Random random, float min, float max)
        => Mathf.Lerp(min, max, (float)random.NextDouble());
}
