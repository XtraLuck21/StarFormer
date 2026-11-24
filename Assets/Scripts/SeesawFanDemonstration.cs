using System.Collections.Generic;
using UnityEngine;

/// <summary>A one-shot inset that demonstrates wind direction without driving the live puzzle.</summary>
internal sealed class SeesawFanDemonstration
{
    private const float Duration = 3.15f;
    private const float FanX = -2.65f;
    private static readonly Color Ink = new Color32(21, 38, 49, 255);
    private static readonly Color Wind = new Color32(168, 224, 238, 255);
    private readonly Transform root, beam, fan, rotor, ball;
    private readonly Transform[] streaks = new Transform[6];
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly Material material;
    private float elapsed;

    public SeesawFanDemonstration(Transform parent, Vector3 position)
    {
        root = new GameObject("Seesaw fan demonstration").transform;
        root.SetParent(parent, false);
        root.position = position;
        material = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave
        };

        var backdrop = new SchoolSceneDrawing();
        backdrop.Rect(0f, 0f, 10.2f, 3.58f, new Color32(164, 187, 182, 255));
        backdrop.Rect(0f, 0f, 10.06f, 3.44f, Ink);
        backdrop.Polygon(new Color32(94, 148, 145, 255),
            new Vector2(-.56f, -1.49f), new Vector2(0f, -.70f), new Vector2(.56f, -1.49f));
        Draw(backdrop, "Demonstration backdrop and fulcrum", root, 25);

        var planks = new SchoolSceneDrawing();
        planks.Rect(0f, 0f, 7.86f, .29f, new Color32(164, 107, 63, 255));
        planks.Rect(0f, .113f, 7.86f, .064f, new Color32(219, 157, 94, 255));
        beam = Draw(planks, "Demonstration seesaw", root, 26);
        beam.localPosition = Vector3.down * .72f;

        var fanArt = new SchoolSceneDrawing();
        fanArt.Rect(0f, -.463f, .83f, .13f, new Color32(89, 153, 168, 255));
        fanArt.Circle(Vector2.zero, .474f, new Color32(105, 175, 198, 255));
        fanArt.Circle(Vector2.zero, .381f, Ink);
        fanArt.Polygon(Wind, new Vector2(.36f, .24f), new Vector2(.64f, .15f),
            new Vector2(.64f, -.15f), new Vector2(.36f, -.24f));
        fan = Draw(fanArt, "Demonstration fan", beam, 28);
        fan.localPosition = new Vector3(FanX, .67f, 0f);

        var blades = new SchoolSceneDrawing();
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            Vector2 arm = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 across = new Vector2(-arm.y, arm.x);
            blades.Polygon(Wind, arm * .06f - across * .035f, arm * .29f - across * .065f,
                arm * .32f + across * .08f, arm * .18f + across * .11f);
        }
        blades.Circle(Vector2.zero, .082f, Wind);
        rotor = Draw(blades, "Demonstration fan blades", fan, 29);

        var sphere = new SchoolSceneDrawing();
        sphere.Circle(Vector2.zero, .332f, new Color32(111, 39, 43, 255));
        sphere.Circle(Vector2.zero, .29f, new Color32(228, 70, 63, 255));
        sphere.Arc(Vector2.zero, .215f, 108f, 152f, .042f, new Color32(252, 151, 130, 255));
        ball = Draw(sphere, "Demonstration red ball", beam, 29);

        var gust = new SchoolSceneDrawing();
        gust.Line(new Vector2(-.42f, -.055f), new Vector2(.12f, -.055f), .064f, Wind);
        gust.Line(new Vector2(-.21f, .085f), new Vector2(.43f, .085f), .054f, Wind);
        gust.Line(new Vector2(.27f, .23f), new Vector2(.43f, .085f), .054f, Wind);
        for (int i = 0; i < streaks.Length; i++)
            streaks[i] = Draw(gust, "Demonstration airflow", beam, 27);
        Advance(0f);
    }

    public bool Advance(float seconds)
    {
        elapsed += seconds;
        if (elapsed >= Duration) return false;

        float turn = Ease(.52f, .86f);
        float facingScale = Mathf.Lerp(-1f, 1f, turn);
        fan.localScale = new Vector3(Mathf.Sign(facingScale) * Mathf.Max(.08f, Mathf.Abs(facingScale)), 1f, 1f);
        int direction = elapsed < .52f ? -1 : 1;
        bool blowing = elapsed is >= .13f and < .52f or >= .87f and < 2.47f;
        if (blowing) rotor.Rotate(0f, 0f, -seconds * 900f);

        float roll = Ease(1.20f, 2.43f);
        float ballX = Mathf.Lerp(.16f, 3.34f, roll);
        float ballY = Mathf.Lerp(1.18f, .48f, Ease(.92f, 1.35f));
        ball.localPosition = new Vector3(ballX, ballY, 0f);
        ball.localRotation = Quaternion.Euler(0f, 0f, -roll * 520f);
        beam.localRotation = Quaternion.Euler(0f, 0f, -10.5f * Ease(1.62f, 2.43f));

        float streamLength = direction < 0 ? 1.15f : Mathf.Max(1.15f, ballX - FanX - 1.18f);
        for (int i = 0; i < streaks.Length; i++)
        {
            Transform streak = streaks[i];
            streak.gameObject.SetActive(blowing);
            if (!blowing) continue;
            float flow = Mathf.Repeat(elapsed * 2.2f + i * .18f, 1f);
            streak.localPosition = new Vector3(FanX + direction * (.86f + flow * streamLength),
                .61f + (i % 3 - 1) * .22f, 0f);
            streak.localScale = new Vector3(direction, 1f, 1f);
        }

        material.color = new Color(1f, 1f, 1f, Ease(0f, .15f) * (1f - Ease(2.79f, Duration)));
        return true;
    }

    public void Dispose()
    {
        root.gameObject.SetActive(false);
        SchoolSceneDrawing.Release(root.gameObject);
        foreach (Mesh mesh in meshes) SchoolSceneDrawing.Release(mesh);
        SchoolSceneDrawing.Release(material);
    }

    private float Ease(float start, float end) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, elapsed));

    private Transform Draw(SchoolSceneDrawing art, string name, Transform parent, int order)
    {
        var item = art.Build(name, parent, material, order, out Mesh mesh);
        meshes.Add(mesh);
        return item.transform;
    }
}
