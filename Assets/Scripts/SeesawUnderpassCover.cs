using UnityEngine;

/// <summary>Conceals the authored underpass until the living player enters it.</summary>
public sealed class SeesawUnderpassCover : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawOverloadSequence passage;
    [SerializeField] private Camera roomCamera;
    [SerializeField] private SpriteRenderer upperCover;
    [SerializeField] private SpriteRenderer lowerCover;
    [SerializeField] private Rect revealArea = new Rect(24f, -26.6f, 11.8f, 2.8f);
    [SerializeField, Min(.05f)] private float fadeSeconds = .45f;
    public bool HasRevealed { get; private set; }
    public float Opacity { get; private set; } = 1f;

    private void LateUpdate()
    {
        if (player != null && !player.IsDead && !player.IsRestarting && Time.timeScale > 0f)
        {
            if (!HasRevealed && passage.IsPassageOpen && player.CurrentBody != null &&
                player.CurrentBody.IsControlled && revealArea.Contains(player.CurrentBody.Body.position))
                HasRevealed = true;
            if (HasRevealed) Opacity = Mathf.MoveTowards(Opacity, 0f, Time.deltaTime / fadeSeconds);
        }
        upperCover.color = new Color(0f, 0f, 0f, Opacity);
        Color background = roomCamera.backgroundColor;
        lowerCover.color = new Color(background.r, background.g, background.b, Opacity);
    }
}
