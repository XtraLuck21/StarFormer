using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ChaseFloorFracture : MonoBehaviour
{
    [SerializeField] private SpikeWallEnemyChase spikeWall;
    [SerializeField, Min(0f)] private float clearance = 0.15f;
    [SerializeField, Min(0f)] private float fractureDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float debrisLifetime = 2.5f;

    private BoxCollider2D floorCollider;
    private SpriteRenderer visual;
    private bool wallReached;
    private bool cracking;
    private float timer;
    private Color intactColor;
    private readonly SpriteRenderer[] cracks = new SpriteRenderer[3];
    private readonly Vector3[] crackScales = new Vector3[3];

    private void Awake()
    {
        floorCollider = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        intactColor = visual.color;
    }

    private void FixedUpdate()
    {
        if (cracking || spikeWall == null || !spikeWall.HasStarted)
        {
            return;
        }

        Bounds bounds = floorCollider.bounds;
        if (!wallReached && spikeWall.FrontEdgeX >= bounds.min.x &&
            spikeWall.RearEdgeX <= bounds.max.x)
        {
            wallReached = true;
        }

        // The entire wall must clear the entire slab before support is removed.
        if (!wallReached || spikeWall.RearEdgeX < bounds.max.x + clearance)
        {
            return;
        }

        cracking = true;
        ShowCracks(bounds);
    }

    private void Update()
    {
        if (!cracking)
        {
            return;
        }

        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, fractureDelay));
        for (int i = 0; i < cracks.Length; i++)
        {
            float reveal = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(progress * 1.7f - i * 0.18f));
            cracks[i].color = new Color(1f, 0.65f, 0.25f, reveal);
            Vector3 scale = crackScales[i];
            scale.y *= Mathf.Lerp(0.15f, 1f, reveal);
            cracks[i].transform.localScale = scale;
        }
        visual.color = Color.Lerp(intactColor,
            new Color(0.55f, 0.4f, 0.23f, intactColor.a), progress * 0.35f);

        if (timer >= fractureDelay)
        {
            Fracture(floorCollider.bounds);
        }
    }

    private void ShowCracks(Bounds bounds)
    {
        for (int i = 0; i < 3; i++)
        {
            var crack = new GameObject("Fresh crack");
            crack.transform.position = new Vector3(
                bounds.min.x + bounds.size.x * (i + 1) / 4f,
                bounds.center.y, transform.position.z);
            crack.transform.rotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 25f : -30f);
            crack.transform.localScale = new Vector3(0.065f, bounds.size.y * 1.15f, 1f);
            crack.transform.SetParent(transform, true);
            SpriteRenderer line = crack.AddComponent<SpriteRenderer>();
            CopyVisual(line);
            line.color = new Color(1f, 0.65f, 0.25f, 0f);
            line.sortingOrder = visual.sortingOrder + 1;
            cracks[i] = line;
            crackScales[i] = crack.transform.localScale;
        }
    }

    private void Fracture(Bounds bounds)
    {
        floorCollider.enabled = false;
        const int count = 5;
        for (int i = 0; i < count; i++)
        {
            var piece = new GameObject("Falling chase-floor fragment");
            piece.transform.position = new Vector3(
                bounds.min.x + bounds.size.x * (i + 0.5f) / count,
                bounds.center.y, transform.position.z);
            piece.transform.localScale = new Vector3(
                bounds.size.x / count * 0.98f, bounds.size.y, 1f);
            SpriteRenderer fragment = piece.AddComponent<SpriteRenderer>();
            CopyVisual(fragment);
            Rigidbody2D body = piece.AddComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = 2f;
            body.linearVelocity = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-2.2f, -1.3f));
            body.angularVelocity = Random.Range(-100f, 100f);
            // Cosmetic debris has no collider, so it cannot push or kill Star.
            piece.AddComponent<ShatterPiece>().Init(debrisLifetime, 0.7f);
        }
        Destroy(gameObject);
    }

    private void CopyVisual(SpriteRenderer target)
    {
        target.sprite = visual.sprite;
        target.sharedMaterial = visual.sharedMaterial;
        target.color = visual.color;
        target.sortingLayerID = visual.sortingLayerID;
        target.sortingOrder = visual.sortingOrder;
    }
}
