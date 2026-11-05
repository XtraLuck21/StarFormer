using UnityEngine;
using UnityEngine.UI;

// Only the navigation scene needs this bridge; the help prefab is scene-independent.
public sealed class NavigationHelpBook : MonoBehaviour
{
    [SerializeField] private HelpBookUI book;
    [SerializeField] private Button bookButton;
    [SerializeField] private NavigationStarController star;
    [SerializeField] private Transform buttonAnchor;

    private void OnEnable()
    {
        book.CanOpen = CanOpen;
        book.OpenStateChanged += OnOpenChanged;
        star.SetControlsBlocked(book.IsOpen);
    }

    private bool CanOpen() => star != null && star.isActiveAndEnabled && !star.IsPreviewing;
    private void OnOpenChanged(bool open) => star.SetControlsBlocked(open);

    private void LateUpdate()
    {
        bookButton.interactable = !book.IsOpen && CanOpen();
        if (buttonAnchor == null || Camera.main == null) return;
        var rect = (RectTransform)bookButton.transform;
        var parent = rect.parent as RectTransform;
        var canvas = bookButton.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
            Camera.main.WorldToScreenPoint(buttonAnchor.position), uiCamera, out Vector2 point))
            rect.anchoredPosition = point - parent.rect.center;
    }

    private void OnDisable()
    {
        if (book != null)
        {
            book.OpenStateChanged -= OnOpenChanged;
            book.CanOpen = null;
            if (book.IsOpen) book.Close();
        }
        if (star != null) star.SetControlsBlocked(false);
    }
}
