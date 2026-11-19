using UnityEngine;
using UnityEngine.UI;

// Retains each scene's existing entry button and archived help panel, while displaying
// the same collection prefab used by Navigation. The instance belongs to this scene.
public sealed class SceneCollectionBookUI : MonoBehaviour
{
    [SerializeField] private Button entryButton;
    [SerializeField] private GameObject legacyPanel;
    [SerializeField] private HelpBookUI collectionPrefab;
    private HelpBookUI book;

    private void Awake()
    {
        if (legacyPanel != null) legacyPanel.SetActive(false);
        if (entryButton == null || collectionPrefab == null)
        {
            Debug.LogError("Collection book requires an entry button and shared prefab.", this);
            enabled = false;
            return;
        }
        book = Instantiate(collectionPrefab);
        book.name = "Scene Collection Book";
        // Keep additive-scene lifetimes correct as well as ordinary scene switches.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(book.gameObject, gameObject.scene);
        book.UseExternalEntry(entryButton);
        // Modal collection must cover the Home HUD (31010), including its close-button corner.
        book.GetComponent<Canvas>().sortingOrder = 32000;
        book.CanOpen = () => isActiveAndEnabled && Time.timeScale > 0f;
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
