using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Optional welcome interaction: only a controlled Star jumping up underneath can activate it.
[RequireComponent(typeof(BoxCollider2D))]
public class StarStartGreeting : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private Transform signVisual;
    [SerializeField] private SpriteRenderer face;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite sparkleSprite;
    [SerializeField, Min(.1f)] private float animationSeconds = .75f;
    [SerializeField, Min(0f)] private float bounceHeight = .3f;

    private BoxCollider2D sensor;
    private Vector3 restingPosition;
    private Vector3 restingScale;
    private Color restingColor;
    private readonly SpriteRenderer[] sparkles = new SpriteRenderer[8];
    private Vector3 burstOrigin;
    private float animationTime;
    private bool animating;
    private readonly HashSet<Collider2D> activatedContacts = new HashSet<Collider2D>();
    public bool HasTriggered { get; private set; }

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        sensor.isTrigger = true;
        if (playerController == null || signVisual == null || face == null || label == null || sparkleSprite == null)
        {
            Debug.LogError("Assign the player, welcome sign visuals, label and sparkle sprite.", this);
            enabled = false;
            return;
        }
        restingPosition = signVisual.localPosition;
        restingScale = signVisual.localScale;
        restingColor = face.color;
        label.text = "READY?";
        for (int i = 0; i < sparkles.Length; i++)
        {
            var particle = new GameObject("Welcome sparkle " + i, typeof(SpriteRenderer));
            particle.transform.SetParent(transform, false);
            particle.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            var renderer = particle.GetComponent<SpriteRenderer>();
            renderer.sprite = sparkleSprite;
            renderer.sharedMaterial = face.sharedMaterial;
            renderer.sortingOrder = 15;
            renderer.enabled = false;
            sparkles[i] = renderer;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryActivate(other);
    private void OnTriggerStay2D(Collider2D other) => TryActivate(other);
    private void OnTriggerExit2D(Collider2D other) => activatedContacts.Remove(other);

    private void TryActivate(Collider2D other)
    {
        if (!isActiveAndEnabled || activatedContacts.Contains(other) || Time.timeScale <= 0f ||
            !playerController.isActiveAndEnabled || playerController.IsDead)
            return;
        PossessableBody body = playerController.CurrentBody;
        if (body == null || !body.isActiveAndEnabled || !body.IsReady || !body.IsControlled ||
            body.Form != BodyForm.Star || other.attachedRigidbody != body.Body ||
            body.Body.linearVelocity.y <= .1f || body.BodyCollider.bounds.center.y >= sensor.bounds.min.y)
            return;

        // Keep the greeting text, but replay the effects on each new upward contact.
        // Stay callbacks for the same contact must not restart the animation every frame.
        activatedContacts.Add(other);
        HasTriggered = true;
        label.text = "LET’S GO!";
        burstOrigin = body.transform.position;
        animationTime = 0f;
        animating = true;
        Animate(0f);
    }

    private void Update()
    {
        if (!animating || Time.deltaTime <= 0f) return;
        animationTime += Time.deltaTime;
        float progress = Mathf.Clamp01(animationTime / animationSeconds);
        Animate(progress);
        if (progress >= 1f) ResetVisuals();
    }

    private void Animate(float progress)
    {
        float envelope = (1f - progress) * (1f - progress);
        float bounce = Mathf.Sin(progress * Mathf.PI * 3f) * envelope;
        signVisual.localPosition = restingPosition + Vector3.up * (bounce * bounceHeight);
        signVisual.localScale = restingScale * (1f + .1f * Mathf.Sin(progress * Mathf.PI) * envelope);
        face.color = Color.Lerp(Color.white, restingColor, Mathf.Clamp01(progress * 4f));
        for (int i = 0; i < sparkles.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / sparkles.Length;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var particle = sparkles[i];
            particle.transform.position = burstOrigin + direction * (.4f + .65f * progress);
            particle.transform.localScale = Vector3.one * (.12f * (1f - progress));
            particle.color = new Color(1f, .86f, .28f, 1f - progress);
            particle.enabled = progress < 1f;
        }
    }

    private void ResetVisuals()
    {
        animating = false;
        if (signVisual != null)
        {
            signVisual.localPosition = restingPosition;
            signVisual.localScale = restingScale;
        }
        if (face != null) face.color = restingColor;
        foreach (var particle in sparkles)
            if (particle != null) particle.enabled = false;
    }

    private void OnDisable()
    {
        activatedContacts.Clear();
        if (sensor != null && label != null) ResetVisuals();
    }
}
