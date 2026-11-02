using UnityEngine;

/// <summary>Cosmetic spring-driven pupils; never changes the spider's physics.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HexagonSurfaceMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class SpiderGooglyEyes : MonoBehaviour
{
    [Header("Eye Layout (visual local units)")]
    [SerializeField] private Vector2 eyeCenter = new Vector2(0f, 0.12f);
    [SerializeField, Min(0f)] private float eyeSpacing = 0.25f;
    [SerializeField, Min(0.01f)] private float eyeDiameter = 0.23f;
    [SerializeField, Range(0.15f, 0.7f)] private float pupilSize = 0.42f;

    [Header("Wiggle")]
    [SerializeField, Min(1f)] private float springStrength = 85f;
    [SerializeField, Min(0f)] private float damping = 5f;
    [SerializeField, Min(0f)] private float gravityInfluence = 0.035f;
    [SerializeField, Min(0f)] private float accelerationInfluence = 0.008f;
    [SerializeField, Min(0f)] private float movementWiggle = 0.018f;

    private readonly Transform[] pupils = new Transform[2];
    private readonly Vector2[] offsets = new Vector2[2];
    private readonly Vector2[] velocities = new Vector2[2];
    private Rigidbody2D body;
    private PossessableBody possessable;
    private Transform face;
    private GameObject eyeRoot;
    private Sprite circleSprite;
    private Texture2D circleTexture;
    private Vector2 previousVelocity;
    private bool hasPreviousVelocity;

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        face = GetComponent<HexagonSurfaceMovement>().Visual;
        if (face == null || !face.TryGetComponent(out SpriteRenderer bodyRenderer))
        {
            Debug.LogWarning("Spider eyes need a Visual with a SpriteRenderer assigned to surface movement.", this);
            enabled = false;
            return;
        }

        BuildCircle();
        eyeRoot = new GameObject("Googly Eyes");
        eyeRoot.layer = gameObject.layer;
        eyeRoot.transform.SetParent(face, false);
        for (int i = 0; i < pupils.Length; i++)
        {
            float x = (i == 0 ? -0.5f : 0.5f) * eyeSpacing;
            Transform rim = CreateDisc("Eye Rim", eyeRoot.transform,
                eyeCenter + new Vector2(x, 0f),
                eyeDiameter, new Color(0.12f, 0.1f, 0.16f), bodyRenderer, 1);
            CreateDisc("Eye White", rim, Vector2.zero, 0.88f, Color.white, bodyRenderer, 2);
            pupils[i] = CreateDisc("Pupil", rim, Vector2.zero,
                pupilSize, new Color(0.06f, 0.04f, 0.09f), bodyRenderer, 3);
        }
        RefreshVisibility();
    }

    private void OnEnable()
    {
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (possessable == null) possessable = GetComponent<PossessableBody>();
        if (eyeRoot == null) return;

        bool visible = isActiveAndEnabled && possessable != null && possessable.IsControlled;
        eyeRoot.SetActive(visible);
        ResetPupils();
    }

    private void ResetPupils()
    {
        hasPreviousVelocity = false;
        for (int i = 0; i < pupils.Length; i++)
        {
            offsets[i] = velocities[i] = Vector2.zero;
            if (pupils[i] != null) pupils[i].localPosition = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (eyeRoot == null || !eyeRoot.activeSelf || Time.deltaTime <= 0f) return;

        Vector2 currentVelocity = body.linearVelocity;
        Vector2 acceleration = hasPreviousVelocity
            ? (currentVelocity - previousVelocity) / Mathf.Max(Time.deltaTime, 0.001f)
            : Vector2.zero;
        previousVelocity = currentVelocity;
        hasPreviousVelocity = true;

        // Use world gravity even while surface attachment disables body gravity.
        Vector2 localForce = face.InverseTransformVector(
            Physics2D.gravity * gravityInfluence -
            Vector2.ClampMagnitude(acceleration, 40f) * accelerationInfluence);
        float travel = eyeDiameter * (0.88f - pupilSize) * 0.5f;
        float activity = Mathf.Clamp01(currentVelocity.magnitude / 3f);

        // Small fixed substeps keep the spring stable during slow frames.
        float remaining = Mathf.Min(Time.deltaTime, 0.1f);
        while (remaining > 0f)
        {
            float dt = Mathf.Min(remaining, 1f / 120f);
            remaining -= dt;
            for (int i = 0; i < pupils.Length; i++)
            {
                float phase = Time.time * (i == 0 ? 15f : 17f) + i * 2f;
                Vector2 wiggle = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.3f))
                    * (movementWiggle * activity);
                Vector2 target = Vector2.ClampMagnitude(localForce + wiggle, travel);
                velocities[i] += ((target - offsets[i]) * springStrength - velocities[i] * damping) * dt;
                offsets[i] += velocities[i] * dt;
                if (offsets[i].sqrMagnitude > travel * travel)
                {
                    Vector2 normal = offsets[i].normalized;
                    offsets[i] = normal * travel;
                    float outwardSpeed = Vector2.Dot(velocities[i], normal);
                    if (outwardSpeed > 0f) velocities[i] -= normal * (outwardSpeed * 1.35f);
                }
            }
        }

        for (int i = 0; i < pupils.Length; i++)
            pupils[i].localPosition = (Vector3)(offsets[i] / eyeDiameter);
    }

    private Transform CreateDisc(string discName, Transform parent, Vector2 position,
        float diameter, Color color, SpriteRenderer source, int order)
    {
        GameObject disc = new GameObject(discName);
        disc.layer = gameObject.layer;
        disc.transform.SetParent(parent, false);
        disc.transform.localPosition = position;
        disc.transform.localScale = Vector3.one * diameter;
        SpriteRenderer renderer = disc.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = color;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + order;
        return disc.transform;
    }

    private void BuildCircle()
    {
        const int size = 64;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * (size * 0.5f));
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(size * 0.5f - distance));
        }

        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SpiderEyeCircle", filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
        };
        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
        circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        circleSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnDisable()
    {
        if (eyeRoot != null) eyeRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (eyeRoot != null) Destroy(eyeRoot);
        if (circleSprite != null) Destroy(circleSprite);
        if (circleTexture != null) Destroy(circleTexture);
    }
}
