using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Shared six-form collection, discovery refresh, and modal pause ownership.</summary>
public class HelpBookUI : MonoBehaviour
{
    [Serializable]
    public class CollectionControl
    {
        public string action;
        public string keys;
        [Tooltip("-1: none; 0: left mouse; 1: right mouse; 2: mouse aim")]
        public int mouse = -1;
        public bool hold;
    }

    [Serializable]
    public class HelpPage
    {
        public string title;
        // Also used by the compact in-game form HUD.
        public Sprite icon;
        public string unlockId;
        public CollectionControl[] collectionControls;
        public string collectionNote;
        public string collectionQuote;
    }

    [SerializeField] private HelpBookContent content;
    [SerializeField] private Button helpButton;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private RectTransform bookFrame;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backdropButton;
    [SerializeField] private bool closeWithKeyboard = true;
    [SerializeField] private bool pauseWhileOpen = true;
    [SerializeField] private Texture2D collectionArt;
    [SerializeField] private Texture2D collectionPaper;
    [SerializeField] private Texture2D collectionSpiderArt;

    private CollectionBookOverview overview;
    private float timeScaleBeforeOpen = 1f;
    public event Action<bool> OpenStateChanged;
    public Func<bool> CanOpen { get; set; }
    public int PageCount => content == null || content.Pages == null ? 0 : content.Pages.Length;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (helpButton == null || helpPanel == null || bookFrame == null || labelFont == null || content == null)
        {
            Debug.LogError("Collection book has missing UI or content references.", this);
            enabled = false;
            return;
        }
        if (TryGetComponent<Canvas>(out var canvas))
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
        }
        var panel = (RectTransform)bookFrame.parent;
        panel.localScale = Vector3.one;
        panel.anchorMin = new Vector2(.02f, .025f);
        panel.anchorMax = new Vector2(.98f, .975f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        bookFrame.offsetMin = new Vector2(16, 16);
        bookFrame.offsetMax = new Vector2(-16, -16);
        overview = new CollectionBookOverview(bookFrame, labelFont, content.Pages,
            collectionArt, collectionPaper, collectionSpiderArt);
        if (closeButton != null)
        {
            overview.StyleClose(closeButton);
            var rect = (RectTransform)closeButton.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.anchoredPosition = new Vector2(-55, -50);
            rect.sizeDelta = new Vector2(60, 60);
            rect.SetAsLastSibling();
            var label = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "X";
                label.fontSize = 34;
                label.color = new Color32(78, 49, 26, 255);
            }
            closeButton.onClick.AddListener(Close);
        }
        helpButton.onClick.AddListener(Open);
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        helpPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen && closeWithKeyboard && MenuEscapeInput.TryConsume()) Close();
    }

    public void UseExternalEntry(Button entry)
    {
        if (entry == null || entry == helpButton) return;
        helpButton.onClick.RemoveListener(Open);
        helpButton.gameObject.SetActive(false);
        helpButton = entry;
        helpButton.onClick.AddListener(Open);
    }

    public void Open()
    {
        if (!isActiveAndEnabled || overview == null || IsOpen || (CanOpen != null && !CanOpen())) return;
        IsOpen = true;
        helpPanel.SetActive(true);
        helpPanel.transform.SetAsLastSibling();
        overview.Refresh();
        if (pauseWhileOpen)
        {
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }
        OpenStateChanged?.Invoke(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (helpPanel != null) helpPanel.SetActive(false);
        if (pauseWhileOpen) Time.timeScale = timeScaleBeforeOpen;
        OpenStateChanged?.Invoke(false);
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }
    private void OnEnable() => HelpBookUnlocks.Changed += RefreshDiscovery;
    private void RefreshDiscovery() { if (IsOpen) overview?.Refresh(); }
    private void OnDisable()
    {
        HelpBookUnlocks.Changed -= RefreshDiscovery;
        Close();
    }
    private void OnDestroy()
    {
        overview?.Dispose();
        if (helpButton != null) helpButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
    }
}
