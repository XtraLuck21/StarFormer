using UnityEngine;

/// <summary>Fades the floor-colored cover when the controlled Spider enters the underpass.</summary>
public sealed class SeesawUnderpassCover : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawOverloadSequence passage;
    [SerializeField] private SpriteRenderer upperCover;
    [SerializeField] private SpriteRenderer lowerCover;
    [SerializeField] private Rect revealArea = new Rect(24f, -26.6f, 11.8f, 2.8f);
    [SerializeField, Min(.05f)] private float fadeSeconds = .45f;
    public bool HasRevealed { get; private set; }
    public float Opacity { get; private set; } = 1f;

    private void Awake() => ApplyOpacity();

    private void LateUpdate()
    {
        if (player != null && !player.IsDead && !player.IsRestarting && Time.timeScale > 0f)
        {
            if (!HasRevealed && passage.IsPassageOpen && player.CurrentBody != null &&
                player.CurrentBody.Form == BodyForm.Spider && player.CurrentBody.IsControlled &&
                revealArea.Contains(player.CurrentBody.Body.position))
                HasRevealed = true;
            if (HasRevealed) Opacity = Mathf.MoveTowards(Opacity, 0f, Time.deltaTime / fadeSeconds);
        }
        ApplyOpacity();
    }

    private void ApplyOpacity()
    {
        Color ground = upperCover.color;
        ground.a = Opacity;
        upperCover.color = ground;
        lowerCover.color = ground;
    }
}
