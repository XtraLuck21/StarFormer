using UnityEngine;

/// <summary>A disposable snapshot: the ball cracks, then its two shell halves peel away.</summary>
public sealed class SeesawBallShellExit : MonoBehaviour
{
    public const float CrackSeconds = .3f;
    public const float Duration = 1.1f;
    private Texture2D texture;
    private Material material;
    private readonly Mesh[] meshes = new Mesh[2];
    private readonly Transform[] halves = new Transform[2];
    private readonly MeshRenderer[] renderers = new MeshRenderer[2];
    private LineRenderer crack;
    private Color shellColor;
    private Vector3[] crackPoints;

    public static SeesawBallShellExit Create(PossessableBody ball, Transform elevator)
    {
        var visual = ball.GetComponentInChildren<CircleVisual>();
        var source = visual != null ? visual.GetComponent<SpriteRenderer>() : ball.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null) return null;
        var root = new GameObject("Bouncy Ball Opening Shell");
        root.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        root.transform.localScale = source.transform.lossyScale;
        root.transform.SetParent(elevator, true);
        var effect = root.AddComponent<SeesawBallShellExit>();
        effect.Build(source);
        effect.SetAge(0f);
        return effect;
    }

    private void Build(SpriteRenderer source)
    {
        texture = Instantiate(source.sprite.texture);
        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture };
        shellColor = source.color;
        Bounds bounds = source.sprite.bounds;
        Vector2[] seam = { new Vector2(0f, .46f), new Vector2(.07f, .29f),
            new Vector2(-.06f, .1f), new Vector2(.06f, -.08f),
            new Vector2(-.05f, -.27f), new Vector2(0f, -.46f) };
        crackPoints = new Vector3[seam.Length];
        for (int i = 0; i < seam.Length; i++)
            crackPoints[i] = bounds.center + new Vector3(seam[i].x * bounds.size.x, seam[i].y * bounds.size.y);
        Rect uvRect = source.sprite.textureRect;
        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? -1f : 1f;
            // A fan around the interior of each half keeps the jagged edge shared.
            var points = new Vector2[seam.Length + 3];
            points[0] = new Vector2(sign * .25f, 0f);
            points[1] = new Vector2(sign * .5f, -.5f);
            points[2] = new Vector2(sign * .5f, .5f);
            for (int i = 0; i < seam.Length; i++) points[i + 3] = seam[i];
            var vertices = new Vector3[points.Length];
            var uvs = new Vector2[points.Length];
            var colors = new Color[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                vertices[i] = bounds.center + new Vector3(points[i].x * bounds.size.x, points[i].y * bounds.size.y);
                uvs[i] = new Vector2((uvRect.x + (points[i].x + .5f) * uvRect.width) / texture.width,
                    (uvRect.y + (points[i].y + .5f) * uvRect.height) / texture.height);
                colors[i] = shellColor;
            }
            var triangles = new int[(points.Length - 1) * 3];
            for (int i = 1; i < points.Length; i++)
            {
                int n = (i - 1) * 3;
                triangles[n] = 0; triangles[n + 1] = i;
                triangles[n + 2] = i == points.Length - 1 ? 1 : i + 1;
            }
            meshes[side] = new Mesh { name = "Cracked Ball Half", vertices = vertices, uv = uvs,
                colors = colors, triangles = triangles };
            var half = new GameObject(side == 0 ? "Left Shell" : "Right Shell");
            half.transform.SetParent(transform, false);
            halves[side] = half.transform;
            half.AddComponent<MeshFilter>().sharedMesh = meshes[side];
            renderers[side] = half.AddComponent<MeshRenderer>();
            renderers[side].sharedMaterial = material;
            renderers[side].sortingLayerID = source.sortingLayerID;
            renderers[side].sortingOrder = source.sortingOrder + 20;
        }
        var lineObject = new GameObject("Growing Shell Crack");
        lineObject.transform.SetParent(transform, false);
        crack = lineObject.AddComponent<LineRenderer>();
        crack.sharedMaterial = source.sharedMaterial;
        crack.useWorldSpace = false;
        crack.widthMultiplier = .035f * bounds.size.x;
        crack.startColor = crack.endColor = new Color(.95f, .88f, 1f);
        crack.sortingLayerID = source.sortingLayerID;
        crack.sortingOrder = source.sortingOrder + 21;
    }

    public void SetAge(float age)
    {
        float growth = Mathf.Clamp01(age / CrackSeconds) * (crackPoints.Length - 1);
        int segments = Mathf.Min(Mathf.FloorToInt(growth), crackPoints.Length - 2);
        crack.positionCount = segments + 2;
        for (int i = 0; i <= segments; i++) crack.SetPosition(i, crackPoints[i]);
        crack.SetPosition(segments + 1, Vector3.Lerp(crackPoints[segments], crackPoints[segments + 1], growth - segments));
        float split = Mathf.Clamp01((age - CrackSeconds) / (Duration - CrackSeconds));
        crack.enabled = split <= 0f;
        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? -1f : 1f;
            halves[side].localPosition = new Vector3(sign * .8f * split, -.55f * split * split);
            halves[side].localRotation = Quaternion.Euler(0f, 0f, -sign * 40f * split);
        }
        material.color = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(.35f, 1f, split));
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        if (texture != null) Destroy(texture);
    }
}
