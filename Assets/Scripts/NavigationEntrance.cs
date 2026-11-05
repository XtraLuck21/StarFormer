using UnityEngine;

// Animate the artwork separately from the fixed collider so the bounce cannot trap the star.
[RequireComponent(typeof(BoxCollider2D))]
public class NavigationEntrance : MonoBehaviour
{
    [SerializeField] private string destinationLabel;
    [SerializeField] private bool hitFromBelow;
    [SerializeField] private Transform artwork;
    [SerializeField] private Renderer plaqueFace;
    [SerializeField] private Material sparkleMaterial;
    [SerializeField] private Vector2 previewReturnPosition;
    [SerializeField] private float animationSeconds = .85f;
    [SerializeField] private float enteringSeconds = 1.15f;

    private MaterialPropertyBlock tint;
    private readonly Renderer[] sparkles = new Renderer[8];
    private Mesh sparkleMesh;
    private Vector3 restPosition;
    private Vector3 restScale;
    private Color restColor;
    private Vector2 burstOrigin;
    private NavigationStarController player;
    private float elapsed;
    public bool IsAnimating { get; private set; }
    public bool IsShowingEntering => IsAnimating && elapsed >= animationSeconds;

    private void Awake()
    {
        tint = new MaterialPropertyBlock();
        restPosition = artwork.localPosition;
        restScale = artwork.localScale;
        restColor = plaqueFace.sharedMaterial.color;
        sparkleMesh = new Mesh { name = "Navigation sparkle" };
        sparkleMesh.vertices = new[] { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
        sparkleMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        sparkleMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        sparkleMesh.RecalculateBounds();
        for (int i = 0; i < sparkles.Length; i++)
        {
            var particle = new GameObject("Entrance sparkle " + i, typeof(MeshFilter), typeof(MeshRenderer));
            particle.transform.SetParent(transform, false);
            particle.GetComponent<MeshFilter>().sharedMesh = sparkleMesh;
            var renderer = particle.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = sparkleMaterial;
            renderer.sortingOrder = 40;
            renderer.enabled = false;
            sparkles[i] = renderer;
        }
    }

    public bool TryActivate(NavigationStarController star, float verticalSpeed, float normalY, Vector2 contact)
    {
        if (!isActiveAndEnabled || IsAnimating || star.IsPreviewing) return false;
        bool correctDirection = hitFromBelow ? verticalSpeed > .1f && normalY < -.65f :
            verticalSpeed < -.1f && normalY > .65f;
        if (!correctDirection) return false;
        player = star;
        burstOrigin = contact;
        elapsed = 0f;
        IsAnimating = true;
        player.BeginPreview(hitFromBelow);
        Animate(0f);
        return true;
    }

    private void Update()
    {
        if (!IsAnimating) return;
        elapsed += Time.deltaTime;
        if (elapsed < animationSeconds)
        {
            Animate(elapsed / animationSeconds);
            return;
        }
        ResetArtwork();
        float enteringTime = elapsed - animationSeconds;
        float fade = Mathf.Min(Mathf.Clamp01(enteringTime / .18f),
            Mathf.Clamp01((enteringSeconds - enteringTime) / .2f));
        player.ShowEntering(destinationLabel, fade);
        if (enteringTime >= enteringSeconds)
        {
            IsAnimating = false;
            player.ReturnToRoute(previewReturnPosition);
            player = null;
        }
    }

    private void Animate(float progress)
    {
        float envelope = (1f - progress) * (1f - progress);
        float bounce = Mathf.Sin(progress * Mathf.PI * 3f) * envelope;
        artwork.localPosition = restPosition + Vector3.up * (bounce * (hitFromBelow ? .32f : -.22f));
        float stretch = .075f * Mathf.Sin(progress * Mathf.PI * 2f) * envelope;
        artwork.localScale = Vector3.Scale(restScale, new Vector3(1f - stretch * .5f, 1f + stretch, 1f));
        float flash = Mathf.Clamp01(1f - progress * 4f);
        tint.SetColor("_Color", Color.Lerp(restColor, Color.white, flash));
        plaqueFace.SetPropertyBlock(tint);
        player.SetFlash(flash);
        for (int i = 0; i < sparkles.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / sparkles.Length;
            var particle = sparkles[i];
            particle.transform.position = (Vector3)burstOrigin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (.25f + progress * .9f);
            particle.transform.localScale = Vector3.one * (.1f * (1f - progress));
            tint.SetColor("_Color", new Color(1f, .86f, .28f, 1f - progress));
            particle.SetPropertyBlock(tint);
            particle.enabled = true;
        }
    }

    private void ResetArtwork()
    {
        artwork.localPosition = restPosition;
        artwork.localScale = restScale;
        plaqueFace.SetPropertyBlock(null);
        foreach (var particle in sparkles) if (particle != null) particle.enabled = false;
        if (player != null) player.SetFlash(0f);
    }

    private void OnDisable()
    {
        if (artwork != null && plaqueFace != null) ResetArtwork();
        if (player != null) player.ReturnToRoute(previewReturnPosition);
        player = null;
        IsAnimating = false;
    }

    private void OnDestroy()
    {
        if (sparkleMesh != null) Destroy(sparkleMesh);
    }
}
