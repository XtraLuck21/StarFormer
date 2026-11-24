using UnityEngine;
using UnityEngine.UI;

// Connects each scene's existing entry button while displaying
// the same collection prefab used by Navigation. The instance belongs to this scene.
public sealed class SceneCollectionBookUI : MonoBehaviour
{
    [SerializeField] private Button entryButton;
    [SerializeField] private HelpBookUI collectionPrefab;
    private HelpBookUI book;

    private void Awake()
    {
        if (entryButton == null || collectionPrefab == null)
        {
            Debug.LogError("Collection book requires an entry button and shared prefab.", this);
            enabled = false;
            return;
        }
        ApplyEntryAppearance();
        book = Instantiate(collectionPrefab);
        book.name = "Scene Collection Book";
        // Keep additive-scene lifetimes correct as well as ordinary scene switches.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(book.gameObject, gameObject.scene);
        book.UseExternalEntry(entryButton);
        // Modal collection must cover the Home HUD (31010), including its close-button corner.
        book.GetComponent<Canvas>().sortingOrder = 32000;
        book.CanOpen = () => isActiveAndEnabled && Time.timeScale > 0f;
    }

    private void ApplyEntryAppearance()
    {
        // Navigation owns a separate entry; retain its existing presentation.
        if (gameObject.scene.name == "Navigation") return;
        var rect = (RectTransform)entryButton.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-140, -24);
        rect.sizeDelta = new Vector2(88, 88);
        rect.localScale = Vector3.one;

        var icon = entryButton.GetComponentInChildren<CollectionBookGraphic>(true);
        if (icon == null)
        {
            // Older levels still use a text Help button for the same collection entry.
            var obj = new GameObject("Book Icon", typeof(RectTransform), typeof(CollectionBookGraphic));
            obj.layer = entryButton.gameObject.layer;
            obj.transform.SetParent(rect, false);
            icon = obj.GetComponent<CollectionBookGraphic>();
        }
        foreach (var graphic in entryButton.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = graphic == icon;
        var art = icon.rectTransform;
        art.anchorMin = art.anchorMax = art.pivot = new Vector2(.5f, .5f);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(88, 88);
        art.localScale = Vector3.one;
        icon.gameObject.SetActive(true);
        icon.color = Color.white;
        icon.raycastTarget = true;
        icon.UseSquareCover();
        entryButton.targetGraphic = icon;
    }

    private void OnDisable()
    {
        if (book != null) book.Close();
    }

    private void OnDestroy()
    {
        if (book != null) Destroy(book.gameObject);
    }
}
