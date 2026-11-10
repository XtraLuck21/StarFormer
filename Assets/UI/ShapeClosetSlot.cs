using UnityEngine;

// Keep this slot root active; toggle only the locked artwork and unlocked icon.
public class ShapeClosetSlot : MonoBehaviour
{
    [Header("Form discovery")]
    [Tooltip("BodyForm name: Box, Bubble, BouncyBall, Spider or Fan.")]
    [SerializeField] private string unlockId;
    [SerializeField] private GameObject[] lockedVisuals;
    [SerializeField] private GameObject unlockedVisual;

    private void OnEnable()
    {
        HelpBookUnlocks.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => HelpBookUnlocks.Changed -= Refresh;

    private void Refresh()
    {
        bool unlocked = !string.IsNullOrWhiteSpace(unlockId) && HelpBookUnlocks.IsUnlocked(unlockId);
        if (lockedVisuals != null)
            foreach (GameObject visual in lockedVisuals) SetActive(visual, !unlocked);
        SetActive(unlockedVisual, unlocked);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }
}
