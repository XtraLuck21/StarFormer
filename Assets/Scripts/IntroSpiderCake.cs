using UnityEngine;

// Decorative disguise for this spike row. The original hazards own all collisions.
[ExecuteAlways]
public class IntroSpiderCake : MonoBehaviour
{
    [SerializeField] private Color cakeColor = new Color(1f, 0.82f, 0.22f, 1f);
    [SerializeField, Min(0.01f)] private float cakeHeight = 0.25f;
    [SerializeField, Range(1, 8)] private int cherryCount = 3;
    [SerializeField, Min(0.01f)] private float cherryDiameter = 0.16f;
    public bool IsRevealed => revealed;

    private GameObject generatedRoot;
    private GameObject decorations;
    private Sprite rectangleSprite;
    private bool rebuild;
    private bool revealed;

    private void OnEnable()
    {
        revealed = Application.IsPlaying(gameObject) && PlayerPossessionController.IntroSpiderDeathCount >= 1;
        rebuild = true;
    }
    private void OnValidate() => rebuild = true;

    private void Update()
    {
        bool shouldReveal = Application.IsPlaying(gameObject) && PlayerPossessionController.IntroSpiderDeathCount >= 1;
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

        Texture2D texture = Texture2D.whiteTexture;
        rectangleSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            Vector2.one * 0.5f, texture.width);
        rectangleSprite.hideFlags = HideFlags.HideAndDontSave;
        Vector3 cakePosition = new Vector3(bounds.center.x, bounds.max.y + cakeHeight * 0.5f, transform.position.z);
        var cake = new GameObject("Yellow Cake", typeof(SpriteRenderer));
        cake.hideFlags = HideFlags.HideAndDontSave;
        cake.transform.SetParent(decorations.transform, false);
        cake.transform.position = cakePosition;
        cake.transform.localScale = new Vector3(bounds.size.x + 0.12f, cakeHeight, 1f);
        var cakeRenderer = cake.GetComponent<SpriteRenderer>();
        cakeRenderer.sprite = rectangleSprite;
        cakeRenderer.color = cakeColor;
        cakeRenderer.sortingOrder = 4;

        for (int i = 0; i < cherryCount; i++)
        {
            var cherry = new GameObject("Red Cherry " + (i + 1), typeof(SpriteRenderer));
            cherry.hideFlags = HideFlags.HideAndDontSave;
            cherry.transform.SetParent(decorations.transform, false);
            cherry.transform.position = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x,
                (i + 0.5f) / cherryCount), bounds.min.y - cherryDiameter * 0.35f, transform.position.z);
            cherry.transform.localScale = Vector3.one * cherryDiameter;
            cherry.AddComponent<CircleVisual>();
            cherry.GetComponent<SpriteRenderer>().sortingOrder = 4;
        }

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
        if (rectangleSprite != null) DestroyGenerated(rectangleSprite);
        generatedRoot = decorations = null;
        rectangleSprite = null;
    }

    private static void DestroyGenerated(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
