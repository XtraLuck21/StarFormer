using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime HUD shared by positional and Bouncy Ball checkpoints.</summary>
public sealed class CheckpointFeedbackUI : MonoBehaviour
{
    private sealed class Marker
    {
        public Transform Target;
        public string Id;
        public TMP_Text Label;
        public Vector3 Position;
        public bool IsBall;
    }

    private readonly List<Marker> markers = new List<Marker>();
    private PlayerPossessionController player;
    private GameObject root;
    private RectTransform canvasRect;
    private TMP_Text toast;
    private TMP_Text respawnNotice;
    private TMP_Text routeHint;
    private float routeHintUntil;
    private float savedUntil;
    private Camera worldCamera;
    public bool TextSuppressed { get; private set; }

    public void SetTextSuppressed(bool suppressed)
    {
        TextSuppressed = suppressed;
        if (!suppressed) return;
        savedUntil = 0f;
        if (toast != null) toast.text = "";
        foreach (Marker marker in markers) marker.Label.enabled = false;
    }

    public void Initialize(PlayerPossessionController controller)
    {
        player = controller;
        if (root != null) return;

        root = new GameObject("Checkpoint HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        // Keep the HUD in the player's scene and clean it up with the controller.
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = root.GetComponent<RectTransform>();

        toast = CreateLabel("Saved notification", root.transform, 20f, -45f, 360f);
        toast.color = new Color(0.35f, 1f, 0.7f);
        respawnNotice = CreateLabel("Respawning notice", root.transform, 32f, 0f, 380f);
        respawnNotice.rectTransform.anchorMin = respawnNotice.rectTransform.anchorMax = Vector2.one * .5f;
        respawnNotice.text = "Respawning...";
        respawnNotice.enabled = false;
    }

    public void ShowSaved()
    {
        savedUntil = Time.unscaledTime + 2.5f;
    }

    public void ShowRespawning()
    {
        respawnNotice.enabled = true;
    }

    public void ShowRouteHint(string message)
    {
        if (routeHint == null)
            routeHint = CreateLabel("Route hint", root.transform, 30f, -98f, 900f);
        routeHint.text = message;
        routeHint.enabled = true;
        routeHintUntil = Time.time + 4f;
    }

    public void RegisterMarker(Transform target, string id, bool isBall = false)
    {
        TMP_Text label = CreateLabel("Checkpoint " + id, root.transform, 16f, 0f, 180f);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.one * 0.5f;
        markers.Add(new Marker
        {
            Target = target, Id = id, Label = label,
            Position = target.position, IsBall = isBall
        });
    }

    private static TMP_Text CreateLabel(string name, Transform parent, float size, float y, float width)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.fontSize = size;
        label.fontStyle = FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineColor = new Color32(0, 0, 0, 255);
        label.outlineWidth = 0.15f;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = size;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, 36f);
        label.text = "";
        return label;
    }

    private void LateUpdate()
    {
        if (player == null || root == null) return;
        if (routeHint != null && Time.time >= routeHintUntil) routeHint.enabled = false;

        toast.text = !TextSuppressed && Time.unscaledTime < savedUntil
            ? "CHECKPOINT SAVED"
            : "";

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;
        foreach (Marker marker in markers)
        {
            if (marker.Target == null)
            {
                marker.Label.enabled = false;
                continue;
            }

            // Ball recovery uses its original spawn, even after the ball moves.
            Vector3 screen = worldCamera.WorldToScreenPoint(
                marker.IsBall ? marker.Position : marker.Target.position);
            bool visible = !TextSuppressed && screen.z > 0f && worldCamera.pixelRect.Contains((Vector2)screen);
            marker.Label.enabled = visible;
            if (!visible) continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screen, null, out Vector2 local);
            marker.Label.rectTransform.anchoredPosition = local + Vector2.up * 45f;
            bool active = marker.IsBall
                ? player.IsCurrentBouncyBallCheckpoint(marker.Id)
                : player.IsCurrentCheckpoint(marker.Id);
            marker.Label.text = active ? "SAVED\nv"
                : marker.IsBall ? "POSSESS TO SAVE\nv" : "CHECKPOINT\nv";
            marker.Label.color = active
                ? new Color(0.35f, 1f, 0.7f)
                : new Color(1f, 0.85f, 0.35f);
        }
    }

    private void OnEnable()
    {
        if (root != null) root.SetActive(true);
    }

    private void OnDisable()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root);
    }
}
