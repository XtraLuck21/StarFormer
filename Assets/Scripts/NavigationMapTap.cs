using UnityEngine;
using UnityEngine.EventSystems;

// The movement area can also contain a level sign. Only a tap should enter it.
public sealed class NavigationMapTap : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    private NavigationStarController star;
    internal void Initialize(NavigationStarController controller) => star = controller;

    public void OnDrag(PointerEventData pointer)
    {
        float threshold = EventSystem.current.pixelDragThreshold;
        if ((pointer.position - pointer.pressPosition).sqrMagnitude > threshold * threshold)
            pointer.eligibleForClick = false;
    }

    public void OnPointerClick(PointerEventData pointer)
    {
        if (pointer.button == PointerEventData.InputButton.Left)
            star.ActivateEntranceAt(pointer.pressPosition);
    }
}
