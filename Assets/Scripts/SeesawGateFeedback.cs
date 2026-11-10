using TMPro;
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
    private TextMeshPro hint;
    private Vector2 center, size;
    private float age, flashRemaining, hintRemaining;
    private PossessableBody blockedBody;
    private bool permanent;
    private bool collisionReveal;
    private Color edgeTint, flashFill, flashEdge;
    private float FlashSeconds => collisionReveal ? .9f : .35f;
    public bool IsFlashing => flashRemaining > 0f;
    public bool IsHintVisible => hint != null && hint.enabled;

    public void Initialize(SeesawRoomStage owner, PlayerPossessionController controller, SpriteRenderer renderer,
        bool permanent = false, string hintText = "ONLY BALLS\nCAN PASS", Vector2? hintOffset = null,
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

        if (string.IsNullOrEmpty(hintText)) return;
        var label = new GameObject("First Barrier Contact Hint");
        label.transform.SetParent(cues.transform, false);
        // Put the caption toward the middle of the initial player enclosure.
        float inward = owner.transform.InverseTransformPoint(transform.position).x < 0f ? 1f : -1f;
        Vector2 offset = hintOffset ?? new Vector2(inward * 2.1f, 1.6f);
        label.transform.localPosition = new Vector3(center.x + offset.x, center.y + offset.y, 0f);
        hint = label.AddComponent<TextMeshPro>();
        hint.text = hintText;
        hint.fontSize = permanent ? 2.8f : 3.2f;
        hint.fontStyle = FontStyles.Bold;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(1f, .85f, .85f);
        hint.outlineColor = new Color32(30, 10, 15, 255);
        hint.outlineWidth = .18f;
        hint.rectTransform.sizeDelta = new Vector2(permanent ? 4.6f : 3.5f, 1.4f);
        hint.raycastTarget = false;
        var textRenderer = hint.GetComponent<MeshRenderer>();
        textRenderer.sortingLayerID = visual.sortingLayerID;
        textRenderer.sortingOrder = visual.sortingOrder + 4;
        hint.enabled = false;
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
        if (hint != null) hint.enabled = hintRemaining > 0f;
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
        if (permanent || stage.TryClaimGateHint()) ShowHint(2.8f);
    }

    public void ShowHint(float seconds)
    {
        if (hint == null) return;
        hintRemaining = Mathf.Max(hintRemaining, seconds);
    }
}
