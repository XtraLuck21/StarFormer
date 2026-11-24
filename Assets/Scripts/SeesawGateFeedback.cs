using UnityEngine;

/// <summary>Energy-wall edges, moving streaks and feedback for a blocked Star or Fan.</summary>
public sealed class SeesawGateFeedback : MonoBehaviour
{
    private SeesawRoomStage stage;
    private PlayerPossessionController player;
    private SpriteRenderer visual;
    private Color originalColor;
    private GameObject cues;
    private LineRenderer border;
    private readonly LineRenderer[] streaks = new LineRenderer[7];
    private GameObject hint;
    private Mesh hintMesh;
    private Vector2 center, size;
    private float age, flashRemaining, hintRemaining;
    private PossessableBody blockedBody;
    private bool permanent;
    private bool collisionReveal;
    private Color edgeTint, flashFill, flashEdge;
    private float FlashSeconds => collisionReveal ? .9f : .35f;
    public bool IsFlashing => flashRemaining > 0f;
    public bool IsHintVisible => hint != null && hint.activeInHierarchy;

    public void Initialize(SeesawRoomStage owner, PlayerPossessionController controller, SpriteRenderer renderer,
        bool permanent = false,
        bool collisionReveal = false, Color? energyColor = null)
    {
        this.permanent = permanent;
        this.collisionReveal = collisionReveal;
        edgeTint = energyColor ?? new Color(1f, .32f, .38f, .85f);
        flashFill = energyColor.HasValue ? new Color(edgeTint.r, edgeTint.g, edgeTint.b, .38f) :
            new Color(1f, .45f, .5f, .65f);
        flashEdge = energyColor.HasValue ? Color.Lerp(edgeTint, Color.white, .3f) : Color.white;
        stage = owner;
        player = controller;
        visual = renderer;
        originalColor = visual.color;
        if (collisionReveal) originalColor.a = 0f;
        visual.color = originalColor;
        center = transform.InverseTransformPoint(visual.bounds.center);
        size = transform.InverseTransformVector(visual.bounds.size);
        cues = new GameObject("Barrier Energy Cues");
        cues.transform.SetParent(transform, false);
        border = MakeLine("Bright Barrier Edge", .045f, 4);
        border.loop = true;
        border.SetPositions(new[] {
            (Vector3)(center + new Vector2(-size.x, -size.y) * .5f),
            (Vector3)(center + new Vector2(-size.x, size.y) * .5f),
            (Vector3)(center + new Vector2(size.x, size.y) * .5f),
            (Vector3)(center + new Vector2(size.x, -size.y) * .5f)
        });
        for (int i = 0; i < streaks.Length; i++)
            streaks[i] = MakeLine("Moving Energy Streak", .025f, 3);

        // The gear guard blocks both bodies. Only the two red walls need this hint.
        if (!permanent) BuildHint();
    }

    private void BuildHint()
    {
        var art = new SchoolSceneDrawing();
        float inward = stage.transform.InverseTransformPoint(transform.position).x < 0f ? 1f : -1f;
        Color ink = new Color32(35, 54, 62, 255);
        Color paper = new Color32(255, 251, 237, 255);
        Vector3[] puffs = {
            new Vector3(-1.25f, .05f, .43f), new Vector3(-.98f, .38f, .43f),
            new Vector3(-.41f, .47f, .45f), new Vector3(.22f, .44f, .43f),
            new Vector3(.81f, .35f, .46f), new Vector3(1.21f, .06f, .43f),
            new Vector3(1.08f, -.31f, .43f), new Vector3(.53f, -.42f, .42f),
            new Vector3(-.08f, -.45f, .39f), new Vector3(-.66f, -.40f, .42f),
            new Vector3(-1.16f, -.30f, .42f)
        };
        DrawCloud(ink, 0f);
        DrawCloud(paper, .055f);
        for (int i = 0; i < 3; i++)
        {
            var puff = new Vector2(-inward * (1.11f + .22f * i), -.93f - .16f * i);
            float radius = .14f - .036f * i;
            art.Circle(puff, radius, ink);
            art.Circle(puff, radius - .034f, paper);
        }

        art.Circle(new Vector2(-.97f, 0f), .30f, ink);
        art.Circle(new Vector2(-.97f, 0f), .257f, new Color32(216, 53, 48, 255));
        art.Star(new Vector2(.43f, 0f), .37f, ink);
        art.Star(new Vector2(.43f, 0f), .30f, new Color32(244, 171, 26, 255));

        Color green = new Color32(33, 112, 60, 255);
        art.Line(new Vector2(-.56f, -.025f), new Vector2(-.43f, -.16f), .084f, green);
        art.Line(new Vector2(-.46f, -.17f), new Vector2(-.21f, .16f), .084f, green);

        Color red = new Color32(158, 48, 53, 255);
        art.Line(new Vector2(.86f, .145f), new Vector2(1.15f, -.145f), .084f, red);
        art.Line(new Vector2(.86f, -.145f), new Vector2(1.15f, .145f), .084f, red);

        hint = art.Build("Barrier Hint Icons", cues.transform, visual.sharedMaterial, visual.sortingOrder + 4, out hintMesh);
        hint.GetComponent<MeshRenderer>().sortingLayerID = visual.sortingLayerID;
        // Keep the icons toward the middle of the initial player enclosure.
        var hintTransform = hint.transform;
        Vector3 scale = hintTransform.lossyScale;
        hintTransform.localScale = new Vector3(1f / Mathf.Abs(scale.x), 1f / Mathf.Abs(scale.y), 1f);
        hintTransform.position = transform.TransformPoint(center) + new Vector3(inward * 2.1f, 1.6f, 0f);
        hint.SetActive(false);

        void DrawCloud(Color color, float inset)
        {
            art.Rect(0f, 0f, 2.65f - inset * 2f, 1.15f - inset * 2f, color);
            foreach (Vector3 puff in puffs) art.Circle(puff, puff.z - inset, color);
        }
    }

    private LineRenderer MakeLine(string name, float width, int count)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(cues.transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = visual.sharedMaterial;
        line.useWorldSpace = false;
        line.positionCount = count;
        line.widthMultiplier = width;
        line.sortingLayerID = visual.sortingLayerID;
        line.sortingOrder = visual.sortingOrder + 1;
        Color tint = edgeTint;
        if (collisionReveal) tint.a = 0f;
        line.startColor = line.endColor = tint;
        return line;
    }

    private void Update()
    {
        if (stage == null || cues == null) return;
        if (!permanent && stage.IsUnlocked)
        {
            if (cues.activeSelf)
            {
                cues.SetActive(false);
                visual.color = originalColor;
            }
            return;
        }
        if (player == null || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        age += Time.deltaTime;
        flashRemaining = Mathf.Max(0f, flashRemaining - Time.deltaTime);
        hintRemaining = Mathf.Max(0f, hintRemaining - Time.deltaTime);
        float flash = flashRemaining / FlashSeconds;
        visual.color = Color.Lerp(originalColor, flashFill, flash);
        Color edgeColor = Color.Lerp(edgeTint, flashEdge, flash);
        if (collisionReveal) edgeColor.a *= flash;
        border.startColor = border.endColor = edgeColor;
        for (int i = 0; i < streaks.Length; i++)
        {
            Color streakColor = edgeTint;
            streakColor.a *= collisionReveal ? flash : 1f;
            streaks[i].startColor = streaks[i].endColor = streakColor;
            float y = center.y - size.y * .5f + Mathf.Repeat(age * .7f + i * size.y / streaks.Length, size.y - .2f) + .1f;
            streaks[i].SetPositions(new[] {
                new Vector3(center.x - size.x * .28f, y - .06f, 0f),
                new Vector3(center.x, y + .06f, 0f),
                new Vector3(center.x + size.x * .28f, y - .06f, 0f)
            });
        }
        if (hint != null && hint.activeSelf != (hintRemaining > 0f))
            hint.SetActive(hintRemaining > 0f);
    }

    private void OnCollisionEnter2D(Collision2D collision) => CheckCollision(collision);
    private void OnCollisionStay2D(Collision2D collision) => CheckCollision(collision);

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (blockedBody != null && collision.rigidbody == blockedBody.Body) blockedBody = null;
    }

    private void CheckCollision(Collision2D collision)
    {
        if (stage == null || (!permanent && stage.IsUnlocked) || player == null || player.IsDead ||
            player.IsRestarting || Time.timeScale <= 0f || collision.rigidbody == null ||
            !collision.rigidbody.TryGetComponent(out PossessableBody body) ||
            (!permanent && body.Form != BodyForm.Star && body.Form != BodyForm.Fan) ||
            !body.IsControlled || player.CurrentBody != body || body == blockedBody) return;
        blockedBody = body;
        flashRemaining = FlashSeconds;
        if (!permanent && stage.TryClaimGateHint()) ShowHint(2.8f);
    }

    public void ShowHint(float seconds)
    {
        if (hint == null) return;
        hintRemaining = Mathf.Max(hintRemaining, seconds);
    }

    private void OnDestroy() => SchoolSceneDrawing.Release(hintMesh);
}
