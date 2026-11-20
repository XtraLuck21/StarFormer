using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ChaseFloorFracture : MonoBehaviour
{
    [SerializeField] private SpikeWallEnemyChase spikeWall;
    [SerializeField, Min(0f)] private float clearance = 0.15f;
    [SerializeField, Min(0f)] private float fractureDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float debrisLifetime = 2.5f;

    private BoxCollider2D floorCollider;
    private EdgeCollider2D walkSurface;
    private SpriteRenderer visual;
    private float clearedTime;
    private bool cracking;
    private float timer;
    private Color intactColor;
    private FloorCrack[] cracks;

    private void Awake()
    {
        floorCollider = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        intactColor = visual.color;

        // Give the walk surface straight continuation normals at each slab joint.
        // The solid box underneath still blocks approach from the ends and below.
        Vector2 half = floorCollider.size * .5f;
        Vector2 left = floorCollider.offset + new Vector2(-half.x, half.y);
        Vector2 right = left + Vector2.right * floorCollider.size.x;
        walkSurface = gameObject.AddComponent<EdgeCollider2D>();
        walkSurface.points = new[] { left, right };
        walkSurface.adjacentStartPoint = left - Vector2.right * half.x;
        walkSurface.adjacentEndPoint = right + Vector2.right * half.x;
        walkSurface.useAdjacentStartPoint = walkSurface.useAdjacentEndPoint = true;
        walkSurface.sharedMaterial = floorCollider.sharedMaterial;
        float inset = Mathf.Min(half.y, .08f / Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.y)));
        floorCollider.size -= Vector2.up * inset;
        floorCollider.offset -= Vector2.up * (inset * .5f);
    }

    private void FixedUpdate()
    {
        if (cracking || spikeWall == null || !spikeWall.HasStarted)
        {
            return;
        }

        Bounds bounds = visual.bounds;
        if (spikeWall.FrontEdgeX < bounds.min.x || spikeWall.RearEdgeX > bounds.max.x)
        {
            return;
        }

        cracking = true;
        cracks = FloorCrack.CreateAcross(visual, bounds);
    }

    private void Update()
    {
        if (!cracking)
        {
            return;
        }

        timer += Time.deltaTime;
        FloorCrack.RevealAcross(cracks, timer / .42f);
        visual.color = Color.Lerp(intactColor,
            new Color(0.55f, 0.4f, 0.23f, intactColor.a), Mathf.Clamp01(timer / .42f) * .35f);

        // Leave the split visible behind the clock before taking away support.
        if (spikeWall.RearEdgeX >= visual.bounds.max.x + clearance)
        {
            clearedTime += Time.deltaTime;
            if (clearedTime >= fractureDelay)
            {
                Fracture(visual.bounds);
            }
        }
    }

    private void Fracture(Bounds bounds)
    {
        floorCollider.enabled = false;
        walkSurface.enabled = false;
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

// A bright, jagged splinter grows from the top edge down through the timber.
internal sealed class FloorCrack
{
    private readonly LineRenderer line;
    private readonly Vector3[] corners;

    private FloorCrack(SpriteRenderer surface, Bounds bounds, int index)
    {
        line = new GameObject("Fresh crack").AddComponent<LineRenderer>();
        line.transform.SetParent(surface.transform, false);
        line.useWorldSpace = true;
        line.sharedMaterial = surface.sharedMaterial;
        line.sortingLayerID = surface.sortingLayerID;
        line.sortingOrder = surface.sortingOrder + 10;
        line.startColor = new Color32(255, 241, 186, 255);
        line.endColor = new Color32(255, 180, 94, 255);
        line.startWidth = .115f;
        line.endWidth = .055f;
        line.numCornerVertices = 2;
        float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (index + .5f) / 3f);
        float h = bounds.size.y;
        float bend = (index == 1 ? -1f : 1f) * h;
        float z = surface.transform.position.z;
        corners = new[] {
            new Vector3(x, bounds.max.y, z),
            new Vector3(x + .42f * bend, bounds.max.y - .42f * h, z),
            new Vector3(x + .14f * bend, bounds.max.y - .55f * h, z),
            new Vector3(x + .55f * bend, bounds.min.y, z)
        };
        line.enabled = false;
    }

    public static FloorCrack[] CreateAcross(SpriteRenderer surface, Bounds bounds)
    {
        var result = new FloorCrack[3];
        for (int i = 0; i < result.Length; i++)
            result[i] = new FloorCrack(surface, bounds, i);
        return result;
    }

    public static void RevealAcross(FloorCrack[] cracks, float progress)
    {
        for (int i = 0; i < cracks.Length; i++)
        {
            var crack = cracks[i];
            float t = Mathf.Clamp01(progress * 1.4f - i * .2f) * 3f;
            if (t <= 0) continue;
            int segment = Mathf.Min(2, Mathf.FloorToInt(t));
            crack.line.enabled = true;
            crack.line.positionCount = segment + 2;
            for (int j = 0; j <= segment; j++)
                crack.line.SetPosition(j, crack.corners[j]);
            crack.line.SetPosition(segment + 1, Vector3.Lerp(crack.corners[segment], crack.corners[segment + 1], t - segment));
        }
    }

    public void Remove() => Object.Destroy(line.gameObject);
}
