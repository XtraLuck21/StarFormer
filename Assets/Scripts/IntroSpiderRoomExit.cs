using UnityEngine;
using UnityEngine.SceneManagement;

// Pop Bubble, land on the pressure plate, then fall through to the next tutorial.
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class IntroSpiderRoomExit : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private GameObject dropPlatform;
    [SerializeField] private string nextSceneName = "FanTutorial";
    private bool hadBubble;
    private bool opened;
    private bool loading;
    private bool rebuild;
    private GameObject visuals;
    private Transform button;
    private SpriteRenderer buttonRenderer;
    private Sprite sprite;

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;
    private void Awake()
    {
        if (!Application.IsPlaying(gameObject)) return;
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (playerController == null || dropPlatform == null)
        {
            Debug.LogError("Assign the Spider room controller and drop platform.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!rebuild) return;
        rebuild = false;
        Release();
        visuals = new GameObject("Exit Pressure Plate Visuals");
        visuals.hideFlags = HideFlags.HideAndDontSave;
        visuals.transform.SetParent(transform, false);
        Texture2D texture = Texture2D.whiteTexture;
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, texture.width);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        AddPlatePart("Base", new Vector2(0, -.04f), new Vector2(1.2f, .1f), new Color(.3f, .32f, .34f));
        button = AddPlatePart("Button", new Vector2(0, opened ? -.01f : .04f), new Vector2(.85f, .12f),
            opened ? Color.green : new Color(1f, .8f, .2f));
        buttonRenderer = button.GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (!Application.IsPlaying(gameObject) || playerController == null || playerController.IsDead || Time.timeScale <= 0f) return;
        PossessableBody body = playerController.CurrentBody;
        if (body == null || !body.isActiveAndEnabled) return;
        if (body.Form == BodyForm.Bubble) hadBubble = true;
        // Load only after the released Star has actually fallen through the removed floor.
        if (opened && !loading && body.Form == BodyForm.Star && body.BodyCollider != null &&
            body.BodyCollider.bounds.max.y < transform.position.y - .6f)
        {
            loading = true;
            PlayerPossessionController.ResetCheckpointSession();
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryPress(other);
    private void OnTriggerStay2D(Collider2D other) => TryPress(other);

    private void TryPress(Collider2D other)
    {
        if (!Application.IsPlaying(gameObject) || opened || !hadBubble || playerController == null ||
            playerController.IsDead || Time.timeScale <= 0f) return;
        PossessableBody body = playerController.CurrentBody;
        if (body == null || !body.isActiveAndEnabled || !body.IsControlled || body.Form != BodyForm.Star ||
            other.attachedRigidbody != body.Body || !body.IsGrounded()) return;
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Enable scene '{nextSceneName}' in the Build Profile before opening the floor.", this);
            return;
        }
        opened = true;
        if (button != null) button.localPosition = new Vector3(0, -.01f, 0);
        if (buttonRenderer != null) buttonRenderer.color = Color.green;
        dropPlatform.SetActive(false);
    }

    private Transform AddPlatePart(string partName, Vector2 position, Vector2 size, Color color)
    {
        var part = new GameObject(partName, typeof(SpriteRenderer));
        part.hideFlags = HideFlags.HideAndDontSave;
        part.transform.SetParent(visuals.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 12;
        return part.transform;
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();
    private void Release()
    {
        if (visuals != null)
        {
            visuals.SetActive(false);
            DestroyGenerated(visuals);
        }
        if (sprite != null) DestroyGenerated(sprite);
        visuals = null;
        button = null;
        buttonRenderer = null;
        sprite = null;
    }

    private static void DestroyGenerated(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
