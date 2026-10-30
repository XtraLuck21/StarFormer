using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Scene-local restart choices. Opening or cancelling never changes checkpoint state.</summary>
public sealed class RestartMenuUI : MonoBehaviour
{
    private PlayerPossessionController player;
    private GameObject overlay;
    private GameObject previousSelection;
    private float previousTimeScale;
    private float openedAt;

    public bool IsOpen => overlay != null;

    public void Show(PlayerPossessionController controller)
    {
        if (IsOpen) return;
        player = controller;
        previousTimeScale = Time.timeScale;
        openedAt = Time.unscaledTime;
        previousSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        player.ClearInputForRestartMenu();
        Time.timeScale = 0f;

        overlay = new GameObject("Restart menu", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
        {
            var events = new GameObject("Restart menu events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(overlay.transform, false);
        }
        EventSystem.current.SetSelectedGameObject(null);

        RectTransform backdrop = AddImage("Backdrop", overlay.transform,
            new Color(0.015f, 0.025f, 0.045f, 0.75f));
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;

        bool hasCheckpoint = player.HasSavedCheckpoint;
        float height = hasCheckpoint ? 360f : 280f;
        RectTransform panel = AddImage("Choices", backdrop, new Color(0.055f, 0.08f, 0.12f, 1f));
        panel.sizeDelta = new Vector2(560f, height);
        AddLabel("Restart", panel, 36f, new Vector2(0f, height * 0.5f - 45f), new Vector2(500f, 60f));

        var buttons = new List<Button>();
        float y = hasCheckpoint ? 45f : 5f;
        if (hasCheckpoint)
        {
            buttons.Add(AddButton("Return to Checkpoint", panel, y, () => Restart(false)));
            y -= 80f;
        }
        buttons.Add(AddButton("Restart Level", panel, y, () => Restart(true)));
        buttons.Add(AddButton("Cancel", panel, y - 80f, Close));

        // Keep keyboard navigation inside the dialog, with Cancel selected initially.
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + buttons.Count - 1) % buttons.Count],
                selectOnDown = buttons[(i + 1) % buttons.Count]
            };
        }
        EventSystem.current.SetSelectedGameObject(buttons[buttons.Count - 1].gameObject);
    }

    private void Restart(bool clearCheckpoint)
    {
        if (player != null && player.ConfirmManualRestart(clearCheckpoint)) Close();
    }

    public void Close()
    {
        if (!IsOpen) return;
        GameObject oldOverlay = overlay;
        overlay = null;
        oldOverlay.SetActive(false);
        Destroy(oldOverlay);
        Time.timeScale = previousTimeScale;
        if (player != null) player.ResumeAfterRestartMenu(Time.unscaledTime - openedAt);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : null);
    }

    private static RectTransform AddImage(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        obj.GetComponent<Image>().color = color;
        return rect;
    }

    private static void AddLabel(string text, Transform parent, float size, Vector2 position, Vector2 dimensions)
    {
        var obj = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TMP_Text label = obj.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static Button AddButton(string text, Transform parent, float y, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = AddImage(text, parent, Color.white);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(460f, 64f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.13f, 0.22f, 0.3f);
        colors.highlightedColor = colors.selectedColor = new Color(0.22f, 0.4f, 0.5f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.4f);
        button.colors = colors;
        AddLabel(text, rect, 28f, Vector2.zero, rect.sizeDelta);
        button.onClick.AddListener(onClick);
        return button;
    }

    private void OnDisable() => Close();
    private void OnDestroy() => Close();
}
