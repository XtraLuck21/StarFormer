using System;
using UnityEngine;

public class ShapeClosetView : MonoBehaviour
{
    [Serializable]
    private struct Slot
    {
        public BodyForm form;
        public GameObject lockedVisual;
        public GameObject unlockedVisual;
    }

    [SerializeField] private Slot[] slots;

    private void OnEnable()
    {
        ShapeClosetProgress.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => ShapeClosetProgress.Changed -= Refresh;

    private void Refresh()
    {
        if (slots == null) return;
        foreach (Slot slot in slots)
        {
            bool unlocked = ShapeClosetProgress.IsUnlocked(slot.form);
            if (slot.lockedVisual != null) slot.lockedVisual.SetActive(!unlocked);
            if (slot.unlockedVisual != null) slot.unlockedVisual.SetActive(unlocked);
        }
    }
}
