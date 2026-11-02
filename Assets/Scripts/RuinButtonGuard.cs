using UnityEngine;

// R2's first upward hit breaks this cover. Only a later bounce may press RUIN.
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class RuinButtonGuard : MonoBehaviour
{
    [SerializeField] private PossessableBody targetBall;
    private BoxCollider2D cover;
    private SpriteRenderer visual;
    private GameObject cracks;
    private readonly GameObject[] fragments = new GameObject[8];
    private readonly Vector2[] fragmentVelocities = new Vector2[8];
    private float fragmentTime;
    private int breakFrame;
    public bool IsBroken { get; private set; }
    public bool CanActivate { get; private set; }

    private void Awake()
    {
        cover = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        if (targetBall == null)
        {
            Debug.LogError("Assign R2 to the RUIN button cover.", this);
            enabled = false;
            return;
        }
        cracks = new GameObject("CoverCracks");
        cracks.hideFlags = HideFlags.DontSave;
        cracks.transform.SetParent(transform, false);
        Vector2[] points = { new(-.06f, -.45f), new(.05f, -.15f), new(-.04f, .12f), new(.06f, .45f) };
        for (int i = 0; i < points.Length - 1; i++)
        {
            GameObject stroke = new GameObject("Crack");
            stroke.transform.SetParent(cracks.transform, false);
            Vector2 delta = points[i + 1] - points[i];
            stroke.transform.localPosition = (points[i + 1] + points[i]) * .5f;
            stroke.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            stroke.transform.localScale = new Vector3(delta.magnitude, .035f, 1);
            SpriteRenderer renderer = stroke.AddComponent<SpriteRenderer>();
            renderer.sprite = visual.sprite;
            renderer.color = new Color(.12f, .09f, .06f, 1);
            renderer.sortingOrder = visual.sortingOrder + 1;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsBroken || targetBall == null || !targetBall.IsControlled ||
            collision.rigidbody != targetBall.Body || targetBall.BodyCollider.bounds.center.y >= cover.bounds.min.y)
            return;
        bool hitUnderside = false;
        for (int i = 0; i < collision.contactCount; i++)
            if (collision.GetContact(i).normal.y > .65f) hitUnderside = true;
        if (!hitUnderside) return;

        Bounds bounds = cover.bounds;
        IsBroken = true;
        breakFrame = Time.frameCount;
        cover.enabled = false;
        visual.enabled = false;
        if (cracks != null) cracks.SetActive(false);
        // Do not let disabling the cover carry this same jump through to the button.
        Vector2 velocity = targetBall.Body.linearVelocity;
        velocity.y = -Mathf.Max(2f, Mathf.Abs(velocity.y));
        targetBall.Body.linearVelocity = velocity;
        for (int i = 0; i < fragments.Length; i++)
        {
            GameObject piece = new GameObject("CoverFragment");
            piece.hideFlags = HideFlags.DontSave;
            piece.transform.position = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (i + .5f) / fragments.Length), bounds.center.y, transform.position.z);
            piece.transform.localScale = new Vector3(bounds.size.x / fragments.Length * .8f, bounds.size.y * .7f, 1);
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = visual.sprite;
            renderer.color = visual.color;
            renderer.sortingOrder = visual.sortingOrder + 1;
            fragments[i] = piece;
            fragmentVelocities[i] = new Vector2((i - 3.5f) * .55f, 1.5f + (i % 3) * .35f);
        }
    }

    private void Update()
    {
        if (!IsBroken) return;
        if (!CanActivate && Time.frameCount > breakFrame + 1 && targetBall != null &&
            targetBall.IsControlled && targetBall.IsGrounded())
            CanActivate = true;
        fragmentTime += Time.deltaTime;
        for (int i = 0; i < fragments.Length; i++)
        {
            GameObject piece = fragments[i];
            if (piece == null) continue;
            if (fragmentTime >= .6f) { Destroy(piece); fragments[i] = null; continue; }
            fragmentVelocities[i] += Vector2.down * (9f * Time.deltaTime);
            piece.transform.position += (Vector3)(fragmentVelocities[i] * Time.deltaTime);
            piece.transform.Rotate(0, 0, (i % 2 == 0 ? 180 : -180) * Time.deltaTime);
            Color color = visual.color;
            color.a *= 1f - fragmentTime / .6f;
            piece.GetComponent<SpriteRenderer>().color = color;
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject piece in fragments) if (piece != null) Destroy(piece);
    }
}
