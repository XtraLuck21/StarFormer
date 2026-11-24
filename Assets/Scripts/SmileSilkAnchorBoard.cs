using UnityEngine;

/// <summary>
/// A classroom timetable board hanging in the Smile Face climb. Its BoxCollider2D is a normal
/// climbable surface, so Spider can silk onto it to get past the pin strip on the walls.
/// The vector art is regenerated from the collider size and never saved into the scene.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileSilkAnchorBoard : MonoBehaviour
{
    [SerializeField, Min(0f)] private float ropeHeight = 2.2f;
    [SerializeField] private int sortingOrder = 4;

    private static readonly Color Ink = new Color32(49, 66, 73, 255);
    private static readonly Color Cream = new Color32(255, 234, 190, 255);
    private static readonly Color Timber = new Color32(148, 104, 76, 255);
    private static readonly Color PaleWood = new Color32(211, 183, 139, 255);
    private static readonly Color Teal = new Color32(87, 151, 149, 255);
    private static readonly Color Honey = new Color32(244, 181, 69, 255);
    private static readonly Color Rope = new Color32(236, 240, 228, 255);

    private GameObject art;
    private Mesh mesh;
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
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = false;
        // Draw in local space so the art always matches the collider the spider climbs.
        float w = box.size.x, h = box.size.y;
        Vector2 o = box.offset;
        float left = o.x - w / 2f, right = o.x + w / 2f, bottom = o.y - h / 2f, top = o.y + h / 2f;
        // Sizes below are world units: keep this object at unit scale and size the collider instead.
        const float frame = 0.08f;

        var a = new SchoolSceneDrawing();
        // Two strings up to the ceiling hook.
        float ropeTop = top + ropeHeight;
        a.Line(new Vector2(left + w * .18f, top), new Vector2(o.x, ropeTop), .04f, Rope);
        a.Line(new Vector2(right - w * .18f, top), new Vector2(o.x, ropeTop), .04f, Rope);
        a.Circle(new Vector2(o.x, ropeTop), .07f, Ink);

        a.ShadedRect(left, bottom, right, top, Timber, PaleWood);
        a.Rect(o.x, o.y, w - frame * 2f, h - frame * 2f, Cream);

        // Clock on the left, timetable stripes on the right.
        float clockRadius = Mathf.Min(h * .36f, w * .2f);
        Vector2 clock = new Vector2(left + frame + clockRadius * 1.25f, o.y);
        a.Circle(clock, clockRadius, Ink);
        a.Circle(clock, clockRadius * .82f, Color.white);
        a.Line(clock, clock + Vector2.up * clockRadius * .62f, .05f, Ink);
        a.Line(clock, clock + new Vector2(.5f, -.1f) * clockRadius, .06f, Ink);
        float stripeLeft = clock.x + clockRadius * 1.5f, stripeRight = right - frame * 2.5f;
        Color[] lessons = { Teal, Honey, Teal };
        for (int i = 0; i < lessons.Length; i++)
        {
            float y = o.y + (1 - i) * h * .22f;
            a.Rect((stripeLeft + stripeRight) / 2f, y, stripeRight - stripeLeft, h * .1f, lessons[i]);
        }

        material = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Smile silk board",
            mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave
        };
        art = a.Build("Timetable board art", transform, material, sortingOrder, out mesh);
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            Color[] colors = mesh.colors;
            for (int i = 0; i < colors.Length; i++) colors[i] = colors[i].linear;
            mesh.colors = colors;
        }
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();

    private void Clear()
    {
        SchoolSceneDrawing.Release(art);
        SchoolSceneDrawing.Release(mesh);
        SchoolSceneDrawing.Release(material);
        art = null;
        mesh = null;
        material = null;
    }
}
