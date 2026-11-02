using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in scene HUD. Owns its pause across the menu and its destructive-action confirmations.
public sealed class MainMenuButtonUI : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private string menuSceneName = "Mainmenu";
    private GameObject hud;
    private GameObject dialog;
    private Button home;
    private HomeButtonGraphic icon;
    private GameObject previousSelection;
    private float previousTimeScale;
    private bool ownsPause;
    private enum Confirmation { None, RestartLevel, MainMenu }
    private Confirmation confirmation;
    public bool IsOpen => ownsPause;

    private void Awake()
    {
        hud = new GameObject("Home HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        hud.transform.SetParent(transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31010;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        if (EventSystem.current == null)
        {
            var events = new GameObject("Home UI events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(hud.transform, false);
        }

        var obj = new GameObject("Home · Restart", typeof(RectTransform), typeof(HomeButtonGraphic), typeof(Button));
        obj.transform.SetParent(hud.transform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24, -24);
        rect.sizeDelta = new Vector2(88, 88);
        home = obj.GetComponent<Button>();
        icon = obj.GetComponent<HomeButtonGraphic>();
        home.targetGraphic = icon;
        home.transition = Selectable.Transition.None;
        home.navigation = new Navigation { mode = Navigation.Mode.None };
        home.onClick.AddListener(Show);
        var tooltip = ImageRect("Home tooltip", hud.transform, new Color(.07f, .09f, .12f, .97f));
        tooltip.anchorMin = tooltip.anchorMax = tooltip.pivot = Vector2.one;
        tooltip.anchoredPosition = new Vector2(-24, -122);
        tooltip.sizeDelta = new Vector2(230, 48);
        tooltip.GetComponent<Image>().raycastTarget = false;
        Label("Home · Restart", tooltip, 25, Vector2.zero, tooltip.sizeDelta);
        icon.Tooltip = tooltip.gameObject;
        tooltip.gameObject.SetActive(false);
    }

    private bool CanOpen()
    {
        if (player == null || !player.isActiveAndEnabled || player.IsDead || player.IsRestarting || Time.timeScale <= 0) return false;
        var restart = player.GetComponent<RestartMenuUI>();
        return restart == null || !restart.IsOpen;
    }

    private void Update()
    {
        home.interactable = !IsOpen && CanOpen();
        icon.SetAvailable(home.interactable);
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (confirmation != Confirmation.None) ShowPaused();
            else ContinuePlaying();
        }
    }

    public void Show()
    {
        if (IsOpen || !CanOpen()) return;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        player.ClearInputForRestartMenu();
        player.enabled = false;
        Time.timeScale = 0;
        home.interactable = false;
        icon.SetAvailable(false);

        ShowPaused();
    }

    private RectTransform Panel(string title, float height)
    {
        ClearDialog();
        var backdrop = ImageRect(title, hud.transform, new Color(.015f, .025f, .045f, .78f));
        dialog = backdrop.gameObject;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        var panel = ImageRect("Choices", backdrop, new Color(.055f, .08f, .12f, 1));
        panel.sizeDelta = new Vector2(680, height);
        Label(title, panel, 38, new Vector2(0, height * .5f - 65), new Vector2(620, 65));
        return panel;
    }

    private void ShowPaused()
    {
        if (!IsOpen) return;
        confirmation = Confirmation.None;
        var panel = Panel("Paused", 540);
        var resume = Choice("Continue Playing", panel, 120, ContinuePlaying);
        var checkpoint = Choice("Return to Checkpoint", panel, 40, ReturnToCheckpoint);
        checkpoint.interactable = player.HasSavedCheckpoint;
        if (!checkpoint.interactable)
        {
            var label = checkpoint.GetComponentInChildren<TMP_Text>();
            label.fontSize = 26;
            label.color = new Color(.55f, .58f, .62f);
            label.rectTransform.anchoredPosition = new Vector2(0, 12);
            label.rectTransform.sizeDelta = new Vector2(540, 34);
            Label("No checkpoint yet", checkpoint.transform, 19, new Vector2(0, -17), new Vector2(500, 26));
        }
        var divider = ImageRect("Divider", panel, new Color(.3f, .36f, .42f, .6f));
        divider.anchoredPosition = new Vector2(0, -18);
        divider.sizeDelta = new Vector2(540, 1);
        divider.GetComponent<Image>().raycastTarget = false;
        var restart = Choice("Restart Level", panel, -80, RestartLevel);
        var menu = Choice("Main Menu", panel, -160, ReturnToMainMenu);
        SetNavigation(resume, checkpoint, restart, menu);
    }

    private static void SetNavigation(params Button[] choices)
    {
        var active = new List<Button>();
        foreach (var button in choices)
            if (button.interactable) active.Add(button);
        for (int i = 0; i < active.Count; i++)
            active[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = active[(i + active.Count - 1) % active.Count],
                selectOnDown = active[(i + 1) % active.Count]
            };
        if (EventSystem.current != null && active.Count > 0)
            EventSystem.current.SetSelectedGameObject(active[0].gameObject);
    }

    private void ShowConfirmation(Confirmation action)
    {
        if (!IsOpen || confirmation != Confirmation.None) return;
        confirmation = action;
        bool restart = action == Confirmation.RestartLevel;
        var panel = Panel(restart ? "Restart Level?" : "Return to Main Menu?", 390);
        Label(restart ? "Restarting clears this level's\ncheckpoints and progress." :
            "Leaving clears this level's\ncheckpoints and progress.", panel, 25,
            new Vector2(0, 48), new Vector2(600, 75));
        var cancel = Choice("Cancel", panel, -42, ShowPaused);
        var confirm = Choice(restart ? "Restart Level" : "Main Menu", panel, -125,
            () => Confirm(action));
        SetNavigation(cancel, confirm);
    }

    private void ClearDialog()
    {
        if (dialog == null) return;
        dialog.SetActive(false);
        Destroy(dialog);
        dialog = null;
    }

    public void ContinuePlaying()
    {
        if (!ownsPause) return;
        ownsPause = false;
        confirmation = Confirmation.None;
        ClearDialog();
        Time.timeScale = previousTimeScale;
        if (player != null) { player.ClearInputForRestartMenu(); player.enabled = true; }
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy &&
                previousSelection != home.gameObject ? previousSelection : null);
    }

    public void ReturnToCheckpoint()
    {
        if (!IsOpen || confirmation != Confirmation.None || !player.HasSavedCheckpoint) return;
        Restart(false);
    }

    public void RestartLevel() => ShowConfirmation(Confirmation.RestartLevel);
    public void ReturnToMainMenu() => ShowConfirmation(Confirmation.MainMenu);

    private void Restart(bool clearCheckpoint)
    {
        // The existing restart API requires an enabled controller. No frame advances
        // between enabling it and scheduling the reload, so paused input cannot leak.
        player.enabled = true;
        if (player.ConfirmManualRestart(clearCheckpoint)) ContinuePlaying();
        else player.enabled = false;
    }

    private void Confirm(Confirmation action)
    {
        if (!IsOpen || confirmation != action) return;
        if (action == Confirmation.RestartLevel)
        {
            Restart(true);
            return;
        }
        if (action != Confirmation.MainMenu) return;
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError($"Add and enable scene '{menuSceneName}' in the Build Profile.", this);
            return;
        }
        PlayerPossessionController.ResetCheckpointSession();
        ContinuePlaying();
        Time.timeScale = 1;
        SceneManager.LoadScene(menuSceneName);
    }

    private static RectTransform ImageRect(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        obj.GetComponent<Image>().color = color;
        return rect;
    }

    private static void Label(string text, Transform parent, float size, Vector2 at, Vector2 dimensions)
    {
        var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = at;
        rect.sizeDelta = dimensions;
        var label = obj.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static Button Choice(string text, Transform parent, float y, UnityEngine.Events.UnityAction action)
    {
        var rect = ImageRect(text, parent, Color.white);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(540, 64);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        colors.normalColor = new Color(.13f, .22f, .3f);
        colors.highlightedColor = colors.selectedColor = new Color(.22f, .4f, .5f);
        colors.pressedColor = new Color(.1f, .3f, .4f);
        colors.disabledColor = new Color(.12f, .15f, .19f);
        colors.fadeDuration = 0;
        button.colors = colors;
        Label(text, rect, 28, Vector2.zero, rect.sizeDelta);
        button.onClick.AddListener(action);
        return button;
    }

    private void OnEnable() { if (hud != null) hud.SetActive(true); }
    private void OnDisable() { ContinuePlaying(); if (hud != null) hud.SetActive(false); }
}
