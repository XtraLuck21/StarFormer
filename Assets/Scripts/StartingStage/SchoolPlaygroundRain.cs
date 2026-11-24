using UnityEngine;

// Two visual batches keep the rain behind the puzzle and the splashes on the exterior surfaces.
internal sealed class SchoolPlaygroundRain
{
    private const float SplashTime = .26f;
    private readonly System.Random random = new System.Random(720);
    private readonly SchoolRainStreaks background;
    private readonly SchoolSceneDrawing surfaces = new SchoolSceneDrawing();
    private readonly Mesh surfaceMesh;
    private readonly SeesawRoomExit exit;
    private readonly BoxCollider2D leftWall;
    private readonly BoxCollider2D rightWall;
    private readonly Bounds originalRoof;
    private readonly SurfaceDrop[] roofDrops = new SurfaceDrop[11];
    private readonly SurfaceDrop[] rightDrops = new SurfaceDrop[3];
    private SurfaceDrop leftCapDrop;

    private struct SurfaceDrop
    {
        public Vector2 Fall;
        public float Age, TravelTime, Contact, Size;
    }

    public SchoolPlaygroundRain(SchoolMorningScenery scenery, SeesawRoomExit exit,
        BoxCollider2D leftWall, BoxCollider2D rightWall)
    {
        this.exit = exit;
        this.leftWall = leftWall;
        this.rightWall = rightWall;
        originalRoof = exit.Ceiling.bounds;
        if (!exit.Ceiling.enabled && exit.LeftCeiling != null && exit.RightCeiling != null)
        {
            originalRoof = exit.LeftCeiling.bounds;
            originalRoof.Encapsulate(exit.RightCeiling.bounds);
        }
        Bounds left = leftWall.bounds;
        Bounds right = rightWall.bounds;
        Rect area = Rect.MinMaxRect(left.min.x - 5f, Mathf.Min(left.min.y, right.min.y) - 4f,
            right.max.x + 7f, Mathf.Max(left.max.y, right.max.y) + 10f);

        background = new SchoolRainStreaks(scenery, "Playground rain behind room", -35, area, 118, 720);
        scenery.Draw(surfaces, "Playground rain on roof and walls", 2, out surfaceMesh);
        surfaceMesh.MarkDynamic();

        SeedSurfaceDrops(roofDrops);
        SeedSurfaceDrop(ref leftCapDrop);
        SeedSurfaceDrops(rightDrops);
    }

    public void Update(float deltaTime)
    {
        background.Update(deltaTime);
        surfaces.Clear();

        Bounds roof = GetSurface(exit.Ceiling);
        Bounds leftRoof = GetSurface(exit.LeftCeiling);
        Bounds rightRoof = GetSurface(exit.RightCeiling);
        for (int i = 0; i < roofDrops.Length; i++)
        {
            ref SurfaceDrop drop = ref roofDrops[i];
            Advance(ref drop, deltaTime);
            float x = Mathf.Lerp(originalRoof.min.x, originalRoof.max.x, drop.Contact);
            if (TouchesRoof(x, roof) || TouchesRoof(x, leftRoof) || TouchesRoof(x, rightRoof))
                DrawSurfaceDrop(drop, new Vector2(x, originalRoof.max.y), Vector2.up);
        }

        // The exposed left wall cap can catch drops falling from the upper right.
        Bounds left = GetSurface(leftWall);
        if (left.size.x > 0f && left.max.y >= originalRoof.max.y)
        {
            Advance(ref leftCapDrop, deltaTime);
            float x = Mathf.Lerp(left.min.x + .12f, left.max.x - .12f, leftCapDrop.Contact);
            DrawSurfaceDrop(leftCapDrop, new Vector2(x, left.max.y), Vector2.up);
        }
        DrawRightWall(GetSurface(rightWall), deltaTime);

        surfaces.Upload(surfaceMesh);
    }

    private void DrawRightWall(Bounds wall, float deltaTime)
    {
        if (wall.size.y <= 0f) return;
        for (int i = 0; i < rightDrops.Length; i++)
        {
            ref SurfaceDrop drop = ref rightDrops[i];
            Advance(ref drop, deltaTime);
            float y = Mathf.Lerp(wall.min.y + .35f, wall.max.y - .6f, drop.Contact);
            DrawSurfaceDrop(drop, new Vector2(wall.max.x, y), Vector2.right);
        }
    }

    private void DrawSurfaceDrop(SurfaceDrop drop, Vector2 contact, Vector2 normal)
    {
        float sinceContact = drop.Age - drop.TravelTime;
        if (sinceContact < 0f)
        {
            Vector2 tip = contact + drop.Fall * sinceContact;
            surfaces.Line(tip, tip - drop.Fall.normalized * (.49f * drop.Size), .039f,
                SchoolRainStreaks.Tint(.67f * Mathf.Clamp01(drop.Age * 4f)));
            return;
        }

        float time = sinceContact / SplashTime;
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        Color color = SchoolRainStreaks.Tint(.86f * (1f - time));
        surfaces.Line(contact - tangent * .09f, contact + tangent * .09f, .033f, color);
        for (int i = -1; i <= 1; i++)
        {
            Vector2 spread = tangent * (i * (.065f + time * .23f)) + normal * (.055f + time * .14f);
            Vector2 particle = contact + spread * drop.Size;
            surfaces.Line(particle, particle + spread.normalized * .08f, .038f, color);
        }
    }

    private void SeedSurfaceDrops(SurfaceDrop[] drops)
    {
        for (int i = 0; i < drops.Length; i++)
            SeedSurfaceDrop(ref drops[i]);
    }

    private void SeedSurfaceDrop(ref SurfaceDrop drop)
    {
        Reset(ref drop, 0f);
        drop.Age = Range(0f, drop.TravelTime + SplashTime);
    }

    private void Advance(ref SurfaceDrop drop, float deltaTime)
    {
        drop.Age += deltaTime;
        if (drop.Age >= drop.TravelTime + SplashTime)
            Reset(ref drop, drop.Age - drop.TravelTime - SplashTime);
    }

    private void Reset(ref SurfaceDrop drop, float age)
    {
        drop.Age = age;
        drop.Fall = SchoolRainStreaks.Fall(random);
        drop.TravelTime = Range(.64f, 1.5f);
        drop.Contact = Range(.015f, .985f);
        drop.Size = Range(.75f, 1.35f);
    }

    private static Bounds GetSurface(BoxCollider2D collider) => collider != null ? collider.bounds : default;
    private static bool TouchesRoof(float x, Bounds roof) => roof.size.x > 0f && x >= roof.min.x && x <= roof.max.x;
    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
}
