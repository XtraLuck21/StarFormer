using UnityEngine;

/// <summary>Reveal the entire underpass when Yellow is pressed, before the Spider enters it.</summary>
public sealed class SeesawUnderpassCover : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawOverloadSequence passage;
    [SerializeField] private SpriteRenderer upperCover;
    [SerializeField] private SpriteRenderer lowerCover;
    [SerializeField,Min(.05f)] private float fadeSeconds=.45f;
    public bool HasRevealed { get; private set; }
    public float Opacity { get; private set; }=1f;
    private void Awake() => ApplyOpacity();
    private void LateUpdate()
    {
        if(player!=null && !player.IsDead && !player.IsRestarting && Time.timeScale>0f)
        {
            if(passage!=null && (passage.HasStarted || passage.IsPassageOpen))HasRevealed=true;
            if(HasRevealed)Opacity=Mathf.MoveTowards(Opacity,0f,Time.deltaTime/fadeSeconds);
        }
        ApplyOpacity();
    }
    private void ApplyOpacity()
    {
        ApplyCover(upperCover);ApplyCover(lowerCover);
    }
    private void ApplyCover(SpriteRenderer cover)
    {
        if(cover==null)return;
        Color color=cover.color;color.a=Opacity;cover.color=color;
        cover.enabled=Opacity>.001f;
    }
}
