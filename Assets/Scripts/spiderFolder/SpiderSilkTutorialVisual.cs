using UnityEngine;

// A visual-only mouse gesture. Never sends input or moves the controlled Spider.
internal sealed class SpiderSilkTutorialVisual
{
    private static readonly Color Ink = new Color(.08f, .16f, .22f);
    private static readonly Color Gold = new Color(1f, .77f, .2f);
    private readonly GameObject root;
    private readonly Transform cursor, target, pulse;
    private readonly SpriteRenderer button;
    private readonly SpriteRenderer[] dashes = new SpriteRenderer[9];
    private readonly Mesh cursorMesh;
    private readonly Material material;
    private readonly MeshRenderer cursorFace;
    private readonly SpriteRenderer[] strokes;
    private readonly Color[] colors;
    private float age;

    public SpiderSilkTutorialVisual(Transform parent, Sprite sprite, int order)
    {
        root = new GameObject("Silk mouse demonstration") { hideFlags = HideFlags.HideAndDontSave };
        root.transform.SetParent(parent, false);
        // All paths are in world units, independent of the hint's authored icon scale.
        root.transform.localScale = new Vector3(1f / parent.lossyScale.x, 1f / parent.lossyScale.y, 1f);
        material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
        material.mainTexture = Texture2D.whiteTexture;
        cursor = Node("Demo cursor", root.transform);
        Vector3[] points = {
            new Vector3(0, 0), new Vector3(.02f, -.66f), new Vector3(.19f, -.49f),
            new Vector3(.33f, -.75f), new Vector3(.46f, -.68f),
            new Vector3(.32f, -.43f), new Vector3(.56f, -.41f)
        };
        cursorMesh = new Mesh { name = "Tutorial cursor", hideFlags = HideFlags.HideAndDontSave };
        cursorMesh.vertices = points;
        cursorMesh.triangles = new[] { 0, 1, 2, 0, 2, 5, 0, 5, 6, 2, 3, 4, 2, 4, 5 };
        cursorMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white };
        cursorMesh.uv = new Vector2[points.Length];
        cursorMesh.RecalculateBounds();
        cursor.gameObject.AddComponent<MeshFilter>().sharedMesh = cursorMesh;
        cursorFace = cursor.gameObject.AddComponent<MeshRenderer>();
        cursorFace.sharedMaterial = material;
        cursorFace.sortingOrder = order + 3;
        for (int i = 0; i < points.Length; i++)
            Stroke(cursor, points[i], points[(i + 1) % points.Length], .045f, Ink, sprite, order + 4);

        MouseButtonHintIcon.Draw(cursor, sprite, new Vector2(.86f, -.38f), 1f, false, Ink, order + 4);
        button = cursor.Find("Left mouse button/Button fill").GetComponent<SpriteRenderer>();
        target = Node("Silk target", root.transform);
        pulse = Node("Mouse release pulse", root.transform);
        Ring(target, .23f, .035f, Gold, sprite, order + 1);
        Ring(pulse, .26f, .03f, Color.white, sprite, order + 2);
        for (int i = 0; i < dashes.Length; i++)
            dashes[i] = Stroke(root.transform, Vector2.zero, Vector2.right * .08f, .025f,
                new Color(1f, 1f, 1f, .55f), sprite, order);
        strokes = root.GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[strokes.Length];
        for (int i = 0; i < strokes.Length; i++) colors[i] = strokes[i].color;
        Hide();
    }

    public void Draw(Vector2 origin, Vector2 destination, float deltaTime)
    {
        root.SetActive(true);
        age += Mathf.Max(0f, deltaTime);
        float t = Mathf.Repeat(age, 3.8f);
        float travel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, 2f, t));
        float alpha = Mathf.Clamp01(t / .25f) * (1f - Mathf.InverseLerp(3.1f, 3.6f, t));
        bool held = t >= .35f && t < 2.3f;
        cursor.position = Vector3.Lerp(origin, destination, travel);
        target.position = destination;
        target.localScale = Vector3.one * (1f + .12f * Mathf.Sin(t * 4f));
        pulse.position = t < 1f ? origin : destination;
        float clickAge = t < 1f ? t - .35f : t - 2.3f;
        pulse.gameObject.SetActive(clickAge >= 0f && clickAge < .45f);
        pulse.localScale = Vector3.one * (1f + Mathf.Clamp01(clickAge / .45f) * 1.2f);
        for (int i = 0; i < strokes.Length; i++)
        {
            Color color = colors[i];
            color.a *= alpha;
            strokes[i].color = color;
        }
        button.color = held ? new Color(Gold.r, Gold.g, Gold.b, alpha) : new Color(1, 1, 1, alpha * .15f);
        material.color = new Color(1, 1, 1, alpha);
        for (int i = 0; i < dashes.Length; i++)
        {
            float fraction = (i + 1f) / (dashes.Length + 1f);
            dashes[i].enabled = fraction <= travel && held;
            dashes[i].transform.position = Vector2.Lerp(origin, destination, fraction);
            Vector2 direction = destination - origin;
            dashes[i].transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }

    public void Hide()
    {
        age = 0f;
        if (root != null) root.SetActive(false);
    }

    public void Dispose()
    {
        Hide();
        Destroy(root);
        Destroy(cursorMesh);
        Destroy(material);
    }

    private static Transform Node(string name, Transform parent)
    {
        var node = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        node.transform.SetParent(parent, false);
        return node.transform;
    }

    private static SpriteRenderer Stroke(Transform parent, Vector2 from, Vector2 to,
        float width, Color color, Sprite sprite, int order)
    {
        Transform part = Node("Stroke", parent);
        Vector2 delta = to - from;
        part.localPosition = (from + to) * .5f;
        part.localScale = new Vector3(delta.magnitude, width, 1f);
        part.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var renderer = part.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void Ring(Transform parent, float radius, float width, Color color, Sprite sprite, int order)
    {
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI / 16f, b = (i + 1) * Mathf.PI / 16f;
            Stroke(parent, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, color, sprite, order);
        }
    }

    private static void Destroy(Object value)
    {
        if (Application.isPlaying) Object.Destroy(value);
        else Object.DestroyImmediate(value);
    }
}
