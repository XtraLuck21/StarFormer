using TMPro;
using UnityEngine;

// Fixed map lessons that can be revisited. The diagrams never alter ball physics.
public class IntroBouncyBallHints : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [Header("Lesson regions (world X)")]
    [SerializeField] private float entryEndX = -3.4f;
    [SerializeField] private float waterEndX = .6f;
    [SerializeField] private float lowCeilingEndX = 4.3f;
    [SerializeField, Min(0f)] private float boundaryHysteresis = .18f;
    [Header("Fixed map anchors")]
    [SerializeField] private Vector2 entryAnchor = new Vector2(-5.9f, 3.7f);
    [SerializeField] private Vector2 waterAnchor = new Vector2(-1.85f, 4.5f);
    [SerializeField] private Vector2 quickBounceAnchor = new Vector2(2.5f, 4.5f);
    [SerializeField] private Vector2 spikeAnchor = new Vector2(6.4f, 3.7f);

    private enum Lesson { Entry, Water, LowCeiling, Spikes }
    private Lesson lesson;
    private Vector2 anchor;
    private Color normalColor;
    private Canvas canvas;
    private Camera sceneCamera;
    private GameObject diagrams;
    private readonly GameObject[] groups = new GameObject[4];
    private Sprite strokeSprite;
    private Transform tallDot;
    private Transform quickDot;

    private void Start()
    {
        if (playerController == null || titleText == null || subtitleText == null)
        {
            Debug.LogError("Assign the ball tutorial controller and both hint labels.", this);
            enabled = false;
            return;
        }

        canvas = titleText.canvas;
        sceneCamera = Camera.main;
        normalColor = titleText.color;
        titleText.raycastTarget = false;
        titleText.enableAutoSizing = false;
        titleText.fontSize = 30f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.rectTransform.anchorMin = titleText.rectTransform.anchorMax = new Vector2(.5f, .5f);
        titleText.rectTransform.pivot = new Vector2(.5f, .5f);
        titleText.rectTransform.sizeDelta = new Vector2(360f, 105f);
        subtitleText.text = string.Empty;
        subtitleText.enabled = false;
        BuildDiagrams();
    }

    private void LateUpdate()
    {
        if (playerController == null || titleText == null || diagrams == null) return;
        if (Time.timeScale <= 0f && !playerController.IsDead) return;

        PossessableBody body = playerController.CurrentBody;
        if (body == null) return;
        if (!playerController.IsDead)
        {
            float x = body.transform.position.x;
            // Hysteresis prevents flicker at a boundary; vertical bouncing cannot change lessons.
            while (lesson < Lesson.Spikes && x > Boundary((int)lesson) + boundaryHysteresis)
                lesson = (Lesson)((int)lesson + 1);
            while (lesson > Lesson.Entry && x < Boundary((int)lesson - 1) - boundaryHysteresis)
                lesson = (Lesson)((int)lesson - 1);
        }

        for (int i = 0; i < groups.Length; i++)
            groups[i].SetActive(!playerController.IsDead && i == (int)lesson);

        bool onDarkRoof = lesson == Lesson.Water || lesson == Lesson.LowCeiling;
        titleText.color = onDarkRoof ? Color.white : normalColor;
        titleText.alpha = 1f;
        switch (lesson)
        {
            case Lesson.Entry:
                anchor = entryAnchor;
                titleText.text = body.Form == BodyForm.BouncyBall
                    ? "You bounce high\nautomatically!"
                    : "Touch the ball.\nReach the high ledge!";
                break;
            case Lesson.Water:
                anchor = waterAnchor;
                titleText.text = "A little\nfloating break.";
                break;
            case Lesson.LowCeiling:
                anchor = quickBounceAnchor;
                titleText.text = "Lower ceiling.\nMore bounces\nper second.";
                break;
            default:
                anchor = spikeAnchor;
                titleText.text = "Watch the spike!\nBounce over it.";
                break;
        }

        if (playerController.IsDead)
        {
            if (playerController.LastDeathCause == AnalyticsDeathCause.Spike)
            {
                anchor = spikeAnchor;
                titleText.color = normalColor;
                titleText.text = "Ouch! Spikes\naren't for hugs.";
            }
            else titleText.alpha = 0f;
        }
        else
        {
            AnimateBounce(tallDot, new Vector2(-.72f, 0f), 1.15f, 1.6f);
            AnimateBounce(quickDot, new Vector2(.72f, 0f), .35f, .55f);
        }
        PositionLabel();
    }

    private float Boundary(int index)
    {
        return index == 0 ? entryEndX : index == 1 ? waterEndX : lowCeilingEndX;
    }

    private void PositionLabel()
    {
        if (sceneCamera == null || canvas == null) return;
        RectTransform rect = (RectTransform)canvas.transform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,
            sceneCamera.WorldToScreenPoint(anchor), uiCamera, out Vector2 point)) return;
        float halfWidth = Mathf.Min(180f, rect.rect.width * .5f - 20f);
        titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, halfWidth * 2f);
        point.x = Mathf.Clamp(point.x, rect.rect.xMin + halfWidth + 16f, rect.rect.xMax - halfWidth - 16f);
        point.y = Mathf.Clamp(point.y, rect.rect.yMin + 80f, rect.rect.yMax - 65f);
        titleText.rectTransform.anchoredPosition = point;
    }

    private void BuildDiagrams()
    {
        diagrams = new GameObject("Bouncy lesson diagrams");
        diagrams.hideFlags = HideFlags.HideAndDontSave;
        // These are world-space roots, not children of the screen-space Canvas.
        strokeSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        strokeSprite.hideFlags = HideFlags.HideAndDontSave;
        Vector2[] origins = { entryAnchor + Vector2.down * 1.6f, waterAnchor + Vector2.down * 1.6f,
            quickBounceAnchor + Vector2.down * 2.3f, spikeAnchor + Vector2.down * 1.6f };
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i] = new GameObject(((Lesson)i).ToString());
            groups[i].transform.SetParent(diagrams.transform, false);
            groups[i].transform.position = origins[i];
            groups[i].SetActive(false);
        }

        Transform entry = groups[0].transform;
        // A high arc ending above a raised ledge, with a directional arrowhead.
        Arc(entry, new Vector2(-1f, -.3f), new Vector2(.85f, .25f), 1.05f);
        Stroke(entry, new Vector2(.15f, -.45f), new Vector2(.15f, .15f));
        Stroke(entry, new Vector2(.15f, .15f), new Vector2(1.05f, .15f));
        Arrow(entry, new Vector2(.55f, .75f), new Vector2(.85f, .25f));

        Transform water = groups[1].transform;
        Ring(water, new Vector2(0f, .27f), .25f);
        for (int i = 0; i < 24; i++)
        {
            float x = -1.05f + i * .0875f;
            float next = x + .0875f;
            Stroke(water, new Vector2(x, -.12f + .05f * Mathf.Sin(x * 12f)),
                new Vector2(next, -.12f + .05f * Mathf.Sin(next * 12f)));
        }
        Arrow(water, new Vector2(.45f, .3f), new Vector2(1f, .3f));

        Transform low = groups[2].transform;
        Stroke(low, new Vector2(-1.35f, -.12f), new Vector2(1.35f, -.12f));
        Stroke(low, new Vector2(-1.3f, 1.55f), new Vector2(-.15f, 1.55f));
        Stroke(low, new Vector2(.15f, .7f), new Vector2(1.3f, .7f));
        // Side-by-side vertical motion makes frequency distinct from horizontal speed.
        tallDot = Ring(low, new Vector2(-.72f, 0f), .12f);
        quickDot = Ring(low, new Vector2(.72f, 0f), .12f);
        Arrow(low, new Vector2(-.35f, .55f), new Vector2(-.35f, 1.25f));
        Arrow(low, new Vector2(1.1f, .05f), new Vector2(1.1f, .45f));

        Transform spike = groups[3].transform;
        Stroke(spike, new Vector2(-.25f, -.3f), new Vector2(0, .3f));
        Stroke(spike, new Vector2(0, .3f), new Vector2(.25f, -.3f));
        Stroke(spike, new Vector2(.25f, -.3f), new Vector2(-.25f, -.3f));
        Arc(spike, new Vector2(-1f, -.1f), new Vector2(1f, -.1f), 1.1f);
        Arrow(spike, new Vector2(.7f, .4f), new Vector2(1f, -.1f));
    }

    private static void AnimateBounce(Transform dot, Vector2 bottom, float height, float period)
    {
        if (dot == null) return;
        // Same horizontal position, different vertical period. Time.time respects pause.
        float t = Mathf.Repeat(Time.time / period, 1f);
        dot.localPosition = bottom + Vector2.up * (4f * height * t * (1f - t));
    }

    private void Arc(Transform parent, Vector2 from, Vector2 to, float height)
    {
        Vector2 previous = from;
        for (int i = 1; i <= 20; i++)
        {
            float t = i / 20f;
            Vector2 point = Vector2.Lerp(from, to, t) + Vector2.up * (4f * height * t * (1f - t));
            Stroke(parent, previous, point);
            previous = point;
        }
    }

    private Transform Ring(Transform parent, Vector2 center, float radius)
    {
        Transform ring = new GameObject("Ball outline").transform;
        ring.SetParent(parent, false);
        ring.localPosition = center;
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI / 12f;
            float b = (i + 1) * Mathf.PI / 12f;
            Stroke(ring, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius);
        }
        return ring;
    }

    private void Arrow(Transform parent, Vector2 from, Vector2 to)
    {
        Stroke(parent, from, to);
        Vector2 direction = (to - from).normalized;
        Vector2 side = new Vector2(-direction.y, direction.x);
        Stroke(parent, to, to - direction * .15f + side * .1f);
        Stroke(parent, to, to - direction * .15f - side * .1f);
    }

    private void Stroke(Transform parent, Vector2 from, Vector2 to)
    {
        Transform part = new GameObject("Stroke").transform;
        part.SetParent(parent, false);
        Vector2 delta = to - from;
        part.localPosition = (from + to) * .5f;
        part.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        part.localScale = new Vector3(delta.magnitude, .035f, 1f);
        SpriteRenderer renderer = part.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = strokeSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 20;
    }

    private void OnDisable()
    {
        if (titleText != null) titleText.text = string.Empty;
        if (subtitleText != null) subtitleText.text = string.Empty;
        if (diagrams != null) Destroy(diagrams);
        if (strokeSprite != null) Destroy(strokeSprite);
    }
}
