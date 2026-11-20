using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// World-space silk lesson for IntroSpider: a pulsing ghost strand shows where to shoot,
/// a ring marks the anchor, and a mouse / web-button prompt explains the control.
/// The lesson completes once the spider is pulled by silk inside its activation area.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class IntroSpiderSilkHint : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [Tooltip("Local start of the demonstration strand (where the spider should stand).")]
    [SerializeField] private Vector2 launchPoint = new Vector2(-1.2f, 0f);
    [Tooltip("Local anchor of the demonstration strand (a climbable surface).")]
    [SerializeField] private Vector2 anchorPoint = new Vector2(1.2f, 0f);
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 1.4f);
    [SerializeField] private string message = "Hold to aim, release to shoot silk";
    [SerializeField] private string mobileMessage = "Hold the web button, drag to aim";
    [SerializeField] private string successMessage = "Nice web!";
    [SerializeField] private bool showControls = true;

    [Header("Activation Area")]
    [SerializeField] private Vector2 activationAreaSize = new Vector2(6f, 4f);
    [SerializeField] private Vector2 activationAreaOffset;

    [Header("Appearance")]
    [SerializeField] private Color silkColor = new Color(0.55f, 0.92f, 1f, 1f);
    [SerializeField] private Color textColor = new Color(0.13725491f, 0.27450982f, 0.3647059f, 1f);
    [SerializeField] private Color successColor = new Color(0.22f, 0.66f, 0.3f, 1f);
    [SerializeField, Min(0.05f)] private float dashLength = 0.22f;
    [SerializeField, Min(0.01f)] private float strandWidth = 0.06f;
    [SerializeField] private int sortingOrder = 6;
    [SerializeField] private bool previewInEditor = true;

    private GameObject visuals;
    private Sprite sprite;
    private readonly List<SpriteRenderer> dashes = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> ring = new List<SpriteRenderer>();
    private Transform ringRoot;
    private Transform traveller;
    private TextMeshPro label;
    private GameObject mouseIcon;
    private SpiderSilkTutorialVisual mouseDemo;
    private MobileFormControls mobileControls;
    private bool mobileLesson;
    private bool rebuild;
    private bool completed;
    private float age, successAge = -1f;

    public bool Completed => completed;

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;

    private void Update()
    {
        if (rebuild && playerController != null) mobileControls = playerController.GetComponent<MobileFormControls>();
        bool mobile = Application.IsPlaying(gameObject) && mobileControls != null && mobileControls.MobileMode;
        if (rebuild || mobile != mobileLesson)
        {
            rebuild = false;
            mobileLesson = mobile;
            Build();
        }
    }

    private void LateUpdate()
    {
        if (visuals == null) return;
        if (!Application.IsPlaying(gameObject))
        {
            visuals.SetActive(previewInEditor);
            if (mouseDemo != null)
            {
                mouseDemo.Hide();
                mouseDemo.Draw(transform.TransformPoint(launchPoint), transform.TransformPoint(anchorPoint), 1.35f);
            }
            else Animate(1f, 0.55f);
            return;
        }

        PossessableBody body = playerController != null ? playerController.CurrentBody : null;
        bool spider = body != null && body.Form == BodyForm.Spider && body.isActiveAndEnabled &&
            !playerController.IsDead && Time.timeScale > 0f;
        bool inside = spider && IsInsideArea(body);
        SpiderSilkAbility silk = spider ? body.GetComponent<SpiderSilkAbility>() : null;

        if (!completed && inside && silk != null && silk.IsPulling)
        {
            completed = true;
            successAge = 0f;
        }

        if (mouseDemo != null)
        {
            bool demonstrate = inside && !completed && silk != null && !silk.IsAiming && !silk.IsFiring;
            visuals.SetActive(demonstrate);
            if (demonstrate)
                mouseDemo.Draw(body.BodyCollider.bounds.center, transform.TransformPoint(anchorPoint), Time.deltaTime);
            else mouseDemo.Hide();
            return;
        }

        if (completed)
        {
            if (successAge < 0f || successAge > 1.6f || !spider) { visuals.SetActive(false); return; }
            successAge += Time.deltaTime;
            visuals.SetActive(true);
            SetStrandAlpha(0f);
            if (mouseIcon != null) mouseIcon.SetActive(false);
            label.text = successMessage;
            label.color = successColor;
            label.alpha = 1f - Mathf.Clamp01((successAge - 1.1f) / 0.5f);
            return;
        }

        visuals.SetActive(inside);
        if (!inside) { age = 0f; return; }
        age += Time.deltaTime;
        // While the player is aiming, dim the demonstration so the real aim indicator reads first.
        Animate(silk != null && silk.IsAiming ? 0.25f : 1f, age);
    }

    // One loop: the strand shoots out (0.6 s), the anchor pulses, then the strand fades.
    private void Animate(float strength, float time)
    {
        float cycle = Mathf.Repeat(time, 1.6f);
        float shoot = Mathf.Clamp01(cycle / 0.6f);
        float fade = 1f - Mathf.Clamp01((cycle - 1.1f) / 0.5f);
        for (int i = 0; i < dashes.Count; i++)
        {
            float t = (i + 0.5f) / dashes.Count;
            SetAlpha(dashes[i], (t <= shoot ? 0.9f * fade : 0.18f) * strength);
        }
        if (traveller != null)
        {
            traveller.localPosition = Vector2.Lerp(launchPoint, anchorPoint, shoot);
            traveller.gameObject.SetActive(shoot < 1f);
        }
        if (ringRoot != null)
            ringRoot.localScale = Vector3.one * (shoot >= 1f ? 1f + 0.25f * Mathf.Sin((cycle - 0.6f) * 12f) * fade : 1f);
        foreach (SpriteRenderer part in ring) SetAlpha(part, (shoot >= 1f ? 0.95f : 0.5f) * strength);
        if (label != null) { label.color = textColor; label.alpha = Mathf.Max(0.35f, strength); }
    }

    private void SetStrandAlpha(float alpha)
    {
        foreach (SpriteRenderer dash in dashes) SetAlpha(dash, alpha);
        foreach (SpriteRenderer part in ring) SetAlpha(part, alpha);
        if (traveller != null) traveller.gameObject.SetActive(false);
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private bool IsInsideArea(PossessableBody body)
    {
        if (body.BodyCollider == null) return false;
        Vector2 local = (Vector2)transform.InverseTransformPoint(body.BodyCollider.bounds.center) - activationAreaOffset;
        return Mathf.Abs(local.x) <= activationAreaSize.x * 0.5f && Mathf.Abs(local.y) <= activationAreaSize.y * 0.5f;
    }

    private void Build()
    {
        Release();
        visuals = new GameObject("IntroSpiderSilkHintVisuals") { hideFlags = HideFlags.HideAndDontSave };
        visuals.transform.SetParent(transform, false);
        visuals.SetActive(false);
        Texture2D texture = Texture2D.whiteTexture;
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f,
            texture.width, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.HideAndDontSave;

        if (!mobileLesson)
        {
            mouseDemo = new SpiderSilkTutorialVisual(visuals.transform, sprite, sortingOrder);
            return;
        }

        // Dashed demonstration strand.
        Vector2 delta = anchorPoint - launchPoint;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        int count = Mathf.Max(2, Mathf.RoundToInt(delta.magnitude / (dashLength * 1.8f)));
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            dashes.Add(Part("Silk dash", visuals.transform, Vector2.Lerp(launchPoint, anchorPoint, t),
                new Vector2(dashLength, strandWidth), angle, silkColor, sortingOrder));
        }

        // Travelling silk tip.
        traveller = Part("Silk tip", visuals.transform, launchPoint, Vector2.one * 0.16f, 45f,
            Color.white, sortingOrder + 1).transform;

        // Anchor ring: eight short strokes around the target point.
        ringRoot = new GameObject("Anchor ring") { hideFlags = HideFlags.HideAndDontSave }.transform;
        ringRoot.SetParent(visuals.transform, false);
        ringRoot.localPosition = anchorPoint;
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            ring.Add(Part("Ring", ringRoot, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.28f,
                new Vector2(0.05f, 0.16f), i * 45f + 90f, silkColor, sortingOrder));
        }
        ring.Add(Part("Anchor dot", ringRoot, Vector2.zero, Vector2.one * 0.1f, 45f, silkColor, sortingOrder));

        // Control prompt: left mouse button above the message on desktop.
        Vector2 promptCenter = (launchPoint + anchorPoint) * 0.5f + promptOffset;
        if (showControls && !mobileLesson)
        {
            MouseButtonHintIcon.Draw(visuals.transform, sprite, promptCenter + Vector2.up * 0.6f, 0.9f,
                false, textColor, sortingOrder + 2);
            mouseIcon = visuals.transform.GetChild(visuals.transform.childCount - 1).gameObject;
        }
        var labelObject = new GameObject("Silk prompt", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        labelObject.transform.SetParent(visuals.transform, false);
        labelObject.transform.localPosition = promptCenter;
        label = labelObject.AddComponent<TextMeshPro>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = mobileLesson ? mobileMessage : message;
        label.fontSize = 3f;
        label.fontStyle = FontStyles.Bold;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.rectTransform.sizeDelta = new Vector2(5.5f, 0.8f);
        label.renderer.sortingOrder = sortingOrder + 2;
    }

    private SpriteRenderer Part(string partName, Transform parent, Vector2 position, Vector2 size, float angle,
        Color color, int order)
    {
        var part = new GameObject(partName, typeof(SpriteRenderer)) { hideFlags = HideFlags.HideAndDontSave };
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        part.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();

    private void Release()
    {
        mouseDemo?.Dispose();
        mouseDemo = null;
        dashes.Clear();
        ring.Clear();
        ringRoot = null;
        traveller = null;
        label = null;
        mouseIcon = null;
        if (visuals != null) { visuals.SetActive(false); DestroyGenerated(visuals); }
        if (sprite != null) DestroyGenerated(sprite);
        visuals = null;
        sprite = null;
    }

    private static void DestroyGenerated(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(activationAreaOffset, activationAreaSize);
        Gizmos.DrawLine(launchPoint, anchorPoint);
    }
}
