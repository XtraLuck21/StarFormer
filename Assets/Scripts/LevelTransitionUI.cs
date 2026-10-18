using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Scene-local progression and final-completion overlays; never persist into another scene.
public class LevelTransitionUI : MonoBehaviour
{
    private static LevelTransitionUI activeTransition;
    private PlayerPossessionController controller;
    private bool controllerWasEnabled;
    private float previousTimeScale;
    private GameObject previousSelection;
    private bool ownsPause;

    public static void ShowAndLoad(string sceneName, string levelLabel,
        PlayerPossessionController playerController, float duration)
    {
        LevelTransitionUI transition = Begin(playerController);
        if (transition == null) return;
        transition.BuildOverlay(levelLabel);
        transition.StartCoroutine(transition.LoadAfterDelay(sceneName, duration));
    }

    public static void ShowFinalResult(string menuSceneName,
        PlayerPossessionController playerController)
    {
        LevelTransitionUI transition = Begin(playerController);
        if (transition == null) return;
        transition.BuildOverlay(null, true);
        transition.AddMenuButton(menuSceneName);
    }

    private static LevelTransitionUI Begin(PlayerPossessionController playerController)
    {
        if (activeTransition != null) return null;

        var root = new GameObject("LevelCompletionOverlay", typeof(RectTransform));
        var transition = root.AddComponent<LevelTransitionUI>();
        activeTransition = transition;
        transition.controller = playerController;
        transition.controllerWasEnabled = playerController != null && playerController.enabled;
        transition.previousTimeScale = Time.timeScale;
        transition.ownsPause = true;
        if (playerController != null) playerController.enabled = false;
        Time.timeScale = 0f;

        // Block both pointer clicks and keyboard submission to the old scene's UI.
        if (EventSystem.current != null)
        {
            transition.previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
        }
        return transition;
    }

    private void BuildOverlay(string levelLabel, bool finalResult = false)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var background = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(transform, false);
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.1f, 0.88f);

        AddText("Title", finalResult ? "Level Complete" : "Level up!", 64f, 50f);
        AddText("Destination", finalResult ? "Thanks for playing!" : "Entering " + levelLabel + "...", 36f, -45f);
    }

    private void AddText(string objectName, string message, float fontSize, float y)
    {
        var label = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(1400f, 100f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private void AddMenuButton(string menuSceneName)
    {
        var buttonObject = new GameObject("ReturnToMainMenu", typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -180f);
        rect.sizeDelta = new Vector2(520f, 90f);
        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.36f, 0.46f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        // Keep navigation inside the result overlay, away from the old scene's buttons.
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "Return to Main Menu";
        text.fontSize = 32f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        button.onClick.AddListener(() =>
        {
            if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                Debug.LogError($"Add and enable scene '{menuSceneName}' in the Build Profile.", this);
                return;
            }
            button.interactable = false;
            PlayerPossessionController.ResetCheckpointSession();
            RestoreState();
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        });
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(buttonObject);
    }

    private IEnumerator LoadAfterDelay(string sceneName, float duration)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration));
        PlayerPossessionController.ResetCheckpointSession();
        RestoreState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void RestoreState()
    {
        if (!ownsPause) return;
        ownsPause = false;
        Time.timeScale = previousTimeScale;
        if (controller != null) controller.enabled = controllerWasEnabled;
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreState();
        if (activeTransition == this) activeTransition = null;
    }
}
