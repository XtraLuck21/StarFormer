using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(BoxCollider2D))]
public class DarkStarPassage : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private level2Camera levelCamera;
    [SerializeField] private Camera passageCamera;
    [SerializeField] private Collider2D landingFloor;
    [SerializeField] private SpriteRenderer darknessVeil;
    [SerializeField] private SpriteRenderer playerGlow;
    [SerializeField] private SpriteRenderer doorStar;
    [SerializeField] private SpriteRenderer doorGlow;
    [SerializeField] private PassageLightPickup[] lightPickups = new PassageLightPickup[0];
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.8f;
    [Header("Light Recovery")]
    [SerializeField, Min(0.1f)] private float initialLightRadius = 3.2f;
    [SerializeField, Min(0.1f)] private float faintLightRadius = 1.1f;
    [SerializeField, Min(0.1f)] private float recoveredLightRadius = 5.5f;
    [SerializeField, Range(0f, 1f)] private float faintLightStrength = 0.45f;
    [SerializeField, Range(0f, 1f)] private float recoveredLightStrength = 0.96f;

    private readonly Dictionary<SpriteRenderer, int> playerOrders = new Dictionary<SpriteRenderer, int>();
    private readonly HashSet<PassageLightPickup> availableLights = new HashSet<PassageLightPickup>();
    private readonly HashSet<PassageLightPickup> collectedLights = new HashSet<PassageLightPickup>();
    private MaterialPropertyBlock veilProperties;
    private PossessableBody visibleBody;
    private SpriteRenderer starRenderer;
    private Color originalStarColor;
    private Color originalBackgroundColor;
    private bool backgroundCaptured;
    private bool active;
    private bool landed;
    private bool completed;
    private float descentStartY;
    private float descentProgress;
    private float darkness;
    private float shownRecovery;
    private float lightRadius;
    private float pickupFlash;

    private static readonly int LightCenterId = Shader.PropertyToID("_LightCenter");
    private static readonly int LightRadiusId = Shader.PropertyToID("_LightRadius");
    private static readonly int LightStrengthId = Shader.PropertyToID("_LightStrength");

    public int CollectedCount => collectedLights.Count;
    public float RecoveryProgress => availableLights.Count == 0 ? 0f : (float)collectedLights.Count / availableLights.Count;
    public float CurrentLightRadius => lightRadius;
    public bool HasLanded => landed;
    public bool IsComplete => completed;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }
        foreach (PassageLightPickup pickup in lightPickups)
        {
            if (pickup != null)
            {
                availableLights.Add(pickup);
            }
        }
        veilProperties = new MaterialPropertyBlock();
        if (passageCamera != null)
        {
            originalBackgroundColor = passageCamera.backgroundColor;
            backgroundCaptured = true;
        }
        lightRadius = initialLightRadius;
        if (playerGlow != null)
        {
            playerGlow.enabled = false;
        }
    }

    private void Start()
    {
        // StarVisual builds the door emblem during OnEnable.
        if (doorStar != null)
        {
            doorStar.sortingOrder = 120;
            doorStar.color = new Color(0.26f, 0.2f, 0.09f);
        }
    }

    public void BeginDescent(PossessableBody star)
    {
        if (completed || active || !IsCurrentStar(star))
        {
            return;
        }
        active = true;
        descentStartY = star.Body.position.y;
        lightRadius = initialLightRadius;
    }

    public void RestoreAtBottom(PossessableBody star)
    {
        if (completed || !IsCurrentStar(star)) return;
        active = true;
        landed = true;
        descentStartY = star.Body.position.y;
        descentProgress = 1f;
        darkness = 1f;
        lightRadius = faintLightRadius;
        if (levelCamera != null) levelCamera.RestoreDarkPassage();
    }

    private bool IsCurrentStar(PossessableBody star)
    {
        return player != null && !player.IsDead && star != null &&
            star.Form == BodyForm.Star && star.IsControlled && player.CurrentBody == star;
    }

    public bool CanCollect(PossessableBody star) => active && landed && IsCurrentStar(star);

    public bool TryAbsorbLight(PassageLightPickup pickup, PossessableBody star)
    {
        if (!CanCollect(star) || pickup == null || !availableLights.Contains(pickup) ||
            !collectedLights.Add(pickup))
        {
            return false;
        }
        // Collected light persists for the rest of this passage; it never drains.
        pickupFlash = 1f;
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryLand(other);
    private void OnTriggerStay2D(Collider2D other) => TryLand(other);

    private void TryLand(Collider2D other)
    {
        PossessableBody current = player != null ? player.CurrentBody : null;
        if (landed || !IsCurrentStar(current) || other.attachedRigidbody != current.Body ||
            !current.IsGrounded())
        {
            return;
        }
        // Also supports starting directly at the bottom in an Editor test/retry.
        BeginDescent(current);
        landed = true;
        descentProgress = 1f;
        if (levelCamera != null)
        {
            levelCamera.BeginDarkPassage();
        }
    }

    private void Update()
    {
        if (!active || player == null || player.CurrentBody == null)
        {
            return;
        }
        PossessableBody current = player.CurrentBody;
        if (!landed && landingFloor != null)
        {
            float landingY = landingFloor.bounds.max.y;
            if (current.BodyCollider != null)
            {
                landingY += current.BodyCollider.bounds.extents.y;
            }
            descentProgress = Mathf.Max(descentProgress,
                Mathf.InverseLerp(descentStartY, landingY, current.Body.position.y));
        }
        float targetDarkness = Mathf.SmoothStep(0f, 1f, descentProgress);
        darkness = Mathf.MoveTowards(darkness, targetDarkness, Time.deltaTime / fadeDuration);
        shownRecovery = Mathf.Lerp(shownRecovery, RecoveryProgress,
            1f - Mathf.Exp(-5f * Time.deltaTime));
        float uncollectedRadius = Mathf.Lerp(initialLightRadius, faintLightRadius, descentProgress);
        float targetRadius = Mathf.Lerp(uncollectedRadius, recoveredLightRadius, shownRecovery);
        lightRadius = Mathf.Lerp(lightRadius, targetRadius, 1f - Mathf.Exp(-6f * Time.deltaTime));
        pickupFlash = Mathf.MoveTowards(pickupFlash, 0f, Time.deltaTime * 3f);
    }

    private void LateUpdate()
    {
        if (!active || player == null || player.CurrentBody == null)
        {
            return;
        }
        PossessableBody current = player.CurrentBody;
        if (visibleBody != current)
        {
            RestorePlayer();
            visibleBody = current;
            foreach (SpriteRenderer renderer in current.GetComponentsInChildren<SpriteRenderer>(true))
            {
                playerOrders[renderer] = renderer.sortingOrder;
                renderer.sortingOrder = 125;
            }
            StarVisual art = current.GetComponentInChildren<StarVisual>(true);
            starRenderer = art != null ? art.GetComponent<SpriteRenderer>() : null;
            if (starRenderer != null)
            {
                originalStarColor = starRenderer.color;
            }
        }

        float recoveryBrightness = Mathf.Lerp(1f - descentProgress, 1f, shownRecovery);
        if (starRenderer != null)
        {
            Color faintColor = new Color(originalStarColor.r * 0.55f,
                originalStarColor.g * 0.55f, originalStarColor.b * 0.55f, originalStarColor.a);
            starRenderer.color = Color.Lerp(faintColor, originalStarColor, recoveryBrightness);
        }
        if (passageCamera != null && darknessVeil != null)
        {
            passageCamera.backgroundColor = Color.Lerp(originalBackgroundColor,
                new Color(0.018f, 0.022f, 0.032f, 1f), descentProgress);
            darknessVeil.transform.position = new Vector3(
                passageCamera.transform.position.x, passageCamera.transform.position.y, 0f);
            darknessVeil.transform.localScale = new Vector3(
                passageCamera.orthographicSize * passageCamera.aspect * 2f + 2f,
                passageCamera.orthographicSize * 2f + 2f, 1f);
            darknessVeil.color = new Color(0.006f, 0.008f, 0.014f, darkness);
            float strengthBeforeRecovery = Mathf.Lerp(0.9f, faintLightStrength, descentProgress);
            darknessVeil.GetPropertyBlock(veilProperties);
            veilProperties.SetVector(LightCenterId, current.transform.position);
            veilProperties.SetFloat(LightRadiusId, lightRadius);
            veilProperties.SetFloat(LightStrengthId,
                Mathf.Lerp(strengthBeforeRecovery, recoveredLightStrength, shownRecovery));
            darknessVeil.SetPropertyBlock(veilProperties);
        }
        if (playerGlow != null)
        {
            playerGlow.enabled = true;
            playerGlow.transform.position = current.transform.position;
            playerGlow.transform.localScale = Vector3.one * lightRadius * 2f;
            playerGlow.color = new Color(1f, 0.72f, 0.24f,
                Mathf.Lerp(0.07f, 0.2f, recoveryBrightness) + pickupFlash * 0.05f);
        }
        if (doorStar != null)
        {
            float proximity = 1f - Mathf.Clamp01(
                Mathf.Abs(current.transform.position.x - doorStar.transform.position.x) / 8f);
            float response = proximity * Mathf.Lerp(0.1f, 1f, Mathf.Sqrt(shownRecovery));
            doorStar.color = Color.Lerp(new Color(0.26f, 0.2f, 0.09f),
                new Color(1f, 0.9f, 0.5f), response);
            if (doorGlow != null)
            {
                doorGlow.color = new Color(1f, 0.64f, 0.2f, 0.03f + response * 0.22f);
            }
        }
    }

    private void OnDisable()
    {
        ClearPresentation();
    }

    public void CompletePassage()
    {
        completed = true;
        active = false;
        ClearPresentation();
    }

    private void ClearPresentation()
    {
        RestorePlayer();
        if (passageCamera != null && backgroundCaptured)
        {
            passageCamera.backgroundColor = originalBackgroundColor;
        }
        if (darknessVeil != null)
        {
            darknessVeil.color = Color.clear;
        }
        if (playerGlow != null)
        {
            playerGlow.enabled = false;
        }
    }

    private void RestorePlayer()
    {
        if (starRenderer != null)
        {
            starRenderer.color = originalStarColor;
        }
        foreach (var entry in playerOrders)
        {
            if (entry.Key != null)
            {
                entry.Key.sortingOrder = entry.Value;
            }
        }
        playerOrders.Clear();
        visibleBody = null;
        starRenderer = null;
    }
}
