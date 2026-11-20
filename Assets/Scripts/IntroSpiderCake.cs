using UnityEngine;

// Decorative disguise for this spike row. The original hazards own all collisions.
[ExecuteAlways]
public class IntroSpiderCake : MonoBehaviour
{
    [SerializeField] private Color cakeColor = new Color(1f, 0.82f, 0.22f, 1f);
    [SerializeField, Min(0.01f)] private float cakeHeight = 0.25f;
    [SerializeField, Range(1, 8)] private int cherryCount = 3;
    [SerializeField, Min(0.01f)] private float cherryDiameter = 0.16f;
    [Tooltip("Floor rows: the cake covers the upward spikes and the cherries sit on top.")]
    [SerializeField] private bool sitsOnFloor;
    public bool IsRevealed => revealed;
    public bool ShowFirstContactMessage { get; private set; }
    private static bool hasTouchedCake;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetContact() => hasTouchedCake = false;

    public void NotifyContact()
    {
        if (hasTouchedCake) return;
        hasTouchedCake = true;
        ShowFirstContactMessage = true;
        revealed = true;
        rebuild = true;
    }

    private GameObject generatedRoot;
    private GameObject decorations;
    private Mesh cakeMesh;
    private Material cakeMaterial;
    private bool rebuild;
    private bool revealed;

    private void OnEnable()
    {
        revealed = Application.IsPlaying(gameObject) && hasTouchedCake;
        rebuild = true;
    }
    private void OnValidate() => rebuild = true;

    private void Update()
    {
        bool shouldReveal = Application.IsPlaying(gameObject) && hasTouchedCake;
        if (revealed != shouldReveal)
        {
            revealed = shouldReveal;
            rebuild = true;
        }
        if (!rebuild) return;
        rebuild = false;
        Build();
    }

    private void Build()
    {
        Release();
        if (revealed) return;
        // Find only the original spikes, before creating any decorative renderers.
        SpikeHazard[] spikes = GetComponentsInChildren<SpikeHazard>();
        Bounds bounds = default;
        bool found = false;
        foreach (SpikeHazard spike in spikes)
        {
            if (!spike.TryGetComponent(out SpriteRenderer renderer)) continue;
            if (!found) bounds = renderer.bounds;
            else bounds.Encapsulate(renderer.bounds);
            found = true;
        }
        if (!found) return;

        generatedRoot = new GameObject("Cake Disguise Visuals");
        generatedRoot.hideFlags = HideFlags.HideAndDontSave;
        generatedRoot.transform.SetParent(transform, false);
        decorations = new GameObject("Cake and Cherries");
        decorations.hideFlags = HideFlags.HideAndDontSave;
        decorations.transform.SetParent(generatedRoot.transform, false);

        float cakeBottom = sitsOnFloor ? bounds.min.y : bounds.max.y;
        float cakeTop = sitsOnFloor ? bounds.max.y + cakeHeight * 0.5f : bounds.max.y + cakeHeight;
        float width = bounds.size.x + .12f;
        float height = cakeTop - cakeBottom;
        float left = -width * .5f, right = width * .5f;
        Color cream = new Color32(255, 241, 212, 255);
        Color crust = new Color32(161, 94, 49, 255);
        Color filling = new Color32(185, 77, 72, 255);
        var art = new SchoolSceneDrawing();
        // A sponge cross-section with a visible filling and piped icing.
        art.Rect(0, height * .5f, width, height, crust);
        art.Rect(0, height * .5f + .015f, width - .045f, height - .045f, cakeColor);
        art.Rect(0, height * .43f, width - .045f, height * .13f, filling);
        art.Rect(0, height * .57f, width - .045f, height * .10f, cream);
        art.Rect(0, height - .04f, width, .12f, cream);
        int scallops = Mathf.Max(3, Mathf.RoundToInt(width / .16f));
        for (int i = 0; i < scallops; i++)
        {
            float x = Mathf.Lerp(left + .06f, right - .06f, i / (float)(scallops - 1));
            art.Circle(new Vector2(x, height - .08f), .07f, cream);
        }
        art.Rect(0, -.015f, width + .12f, .035f, new Color32(213, 220, 210, 255));
        for (int i = 0; i < cherryCount; i++)
        {
            float x = Mathf.Lerp(left, right, (i + .5f) / cherryCount);
            float radius = cherryDiameter * .5f;
            float y = sitsOnFloor ? height + radius * .65f : -radius * .65f;
            art.Circle(new Vector2(x, y - radius * .6f), radius * 1.1f, cream);
            art.Circle(new Vector2(x, y), radius, new Color32(164, 43, 55, 255));
            art.Circle(new Vector2(x + radius * .12f, y + radius * .08f), radius * .78f,
                new Color32(221, 67, 70, 255));
            art.Circle(new Vector2(x - radius * .26f, y + radius * .34f), radius * .20f,
                new Color32(255, 194, 168, 255));
            art.Line(new Vector2(x, y + radius * .75f), new Vector2(x + .035f, y + radius + .09f),
                .022f, new Color32(83, 109, 65, 255));
        }
        cakeMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
        cakeMaterial.mainTexture = Texture2D.whiteTexture;
        var visual = art.Build("Layered sponge cake and cherries", decorations.transform, cakeMaterial, 4, out cakeMesh);
        visual.transform.position = new Vector3(bounds.center.x, cakeBottom, transform.position.z);
        visual.transform.rotation = Quaternion.identity;
        Vector3 scale = decorations.transform.lossyScale;
        visual.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();

    private void Release()
    {
        if (generatedRoot != null)
        {
            generatedRoot.SetActive(false);
            DestroyGenerated(generatedRoot);
        }
        if (cakeMesh != null) DestroyGenerated(cakeMesh);
        if (cakeMaterial != null) DestroyGenerated(cakeMaterial);
        generatedRoot = decorations = null;
        cakeMesh = null;
        cakeMaterial = null;
    }

    private static void DestroyGenerated(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
