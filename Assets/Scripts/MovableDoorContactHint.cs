using TMPro;
using UnityEngine;

// Scene-local help: deliberately walk/bounce into the sign; no extra input binding.
public sealed class MovableDoorContactHint : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private RatchetingCeiling door;
    [SerializeField] private AdvancedLevelCamera2D levelCamera;
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Rect room = new Rect(21f, .8f, 17.2f, 6.2f);
    [SerializeField] private Vector2 buttonPosition = new Vector2(35f, 3.8f);
    [SerializeField] private Vector2 captionPosition = new Vector2(35.6f, 5.8f);
    [SerializeField, Min(1f)] private float stuckSeconds = 20f;
    [SerializeField, Min(.1f)] private float solvedRise = .8f;
    [SerializeField, Min(.5f)] private float demonstrationSeconds = 2.8f;

    private const float ButtonScale = 1.3f;

    private Transform buttonVisual, buttonStar, demoStar;
    private BoxCollider2D sensor;
    private SpriteRenderer buttonFace, ghostDoor;
    private TMP_Text caption;
    private Material iconMaterial;
    private Mesh starMesh;
    private float initialBottom, lastBottom, idleSeconds, attentionSeconds;
    private float demoSeconds, savedTimeScale, celebrateSeconds;
    private Vector3 ghostStart;
    private bool visible, armed, solved, presenting, hasShown;
    private bool playerWasEnabled;

    private void Start()
    {
        if (player == null || door == null || levelCamera == null || squareSprite == null)
        {
            Debug.LogError("Assign the door hint's player, lower door, camera and square sprite.", this);
            enabled = false;
            return;
        }
        initialBottom = lastBottom = door.CurrentBottom;
        CreateVisuals();
        SetVisible(false);
        SetDemoVisible(false);
    }

    private bool CanPlay => player != null && player.isActiveAndEnabled &&
        !player.IsDead && !player.IsRestarting && Time.timeScale > 0f && !levelCamera.IsCinematic;

    private PossessableBody ControlledBody()
    {
        var body = player.CurrentBody;
        return body != null && body.isActiveAndEnabled && body.IsReady && body.IsControlled &&
            body.Body.simulated && body.BodyCollider.enabled ? body : null;
    }

    private void Update()
    {
        if (buttonVisual == null) return;
        if (presenting)
        {
            demoSeconds += Time.unscaledDeltaTime;
            AnimateDemo(Mathf.Clamp01(demoSeconds / demonstrationSeconds));
            if (demoSeconds >= demonstrationSeconds) EndDemo();
            return;
        }
        if (!CanPlay) return;
        if (solved)
        {
            if (visible)
            {
                celebrateSeconds += Time.deltaTime;
                buttonVisual.localScale = Vector3.one * ButtonScale * (1f + .12f * Mathf.Sin(celebrateSeconds * Mathf.PI / .65f));
                buttonFace.color = Color.Lerp(Color.white, new Color(1f, .85f, .3f, 0f), celebrateSeconds / .65f);
                if (celebrateSeconds >= .65f) SetVisible(false);
            }
            return;
        }

        var body = ControlledBody();
        float bottom = door.CurrentBottom;
        // Actual door progress counts even while the player keeps bouncing in place.
        if (bottom >= initialBottom + solvedRise ||
            (body != null && body.Body.position.x >= room.xMax && body.Body.position.y < 11f))
        {
            solved = true;
            sensor.enabled = false;
            return;
        }
        if (Mathf.Abs(bottom - lastBottom) > .025f)
        {
            lastBottom = bottom;
            idleSeconds = 0f;
        }
        if (body == null || !room.Contains(body.Body.position))
        {
            idleSeconds = 0f;
            SetVisible(false);
            return;
        }
        bool eligible = body.Form == BodyForm.Box || body.Form == BodyForm.BouncyBall;
        if (!eligible) { idleSeconds = 0f; SetVisible(false); return; }

        // Once dismissed or used, the sign stays retired for this scene visit.
        if (!visible && hasShown) return;

        idleSeconds += Time.deltaTime;
        if (!visible && idleSeconds >= stuckSeconds)
        {
            hasShown = true;
            SetVisible(true);
            // Only an existing contact blocks activation, not standing below the sign.
            armed = false;
            attentionSeconds = 0f;
            Physics2D.SyncTransforms();
        }
        if (!visible) return;
        ColliderDistance2D contact = sensor.Distance(body.BodyCollider);
        // Separation in any direction arms the next touch, including Box growth
        // from underneath. The one-shot lifecycle prevents repeated bounce activation.
        if (contact.isValid && contact.distance > .03f) armed = true;
        attentionSeconds += Time.deltaTime;
        float hop = attentionSeconds < 1f ? .2f * Mathf.Abs(Mathf.Sin(attentionSeconds * Mathf.PI * 2f)) : 0f;
        buttonStar.localPosition = new Vector3(0f, .08f + hop, -.02f);
        buttonFace.color = Color.Lerp(new Color(.85f, .95f, 1f), Color.white,
            .25f + .15f * Mathf.Sin(attentionSeconds * 2f));
        if (armed && contact.isValid && contact.distance <= .01f) BeginDemo();
    }

    private void BeginDemo()
    {
        if (!CanPlay || !levelCamera.TryBeginCinematic()) return;
        savedTimeScale = Time.timeScale;
        playerWasEnabled = player.enabled;
        player.ClearInputForRestartMenu();
        player.enabled = false;
        Time.timeScale = 0f;
        presenting = true;
        armed = false;
        demoSeconds = idleSeconds = 0f;
        SetVisible(false);
        Bounds bounds = door.GetComponent<BoxCollider2D>().bounds;
        ghostStart = bounds.center;
        ghostDoor.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
        caption.text = "A little lift?";
        SetDemoVisible(true);
        AnimateDemo(0f);
    }

    private void AnimateDemo(float t)
    {
        float travel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .25f));
        float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .3f) / .45f));
        float bottom = door.CurrentBottom;
        float rise = lift;
        ghostDoor.transform.position = ghostStart + Vector3.up * rise;
        Vector3 destination = new Vector3(ghostStart.x, bottom - .3f + rise, -.15f);
        demoStar.position = Vector3.Lerp(new Vector3(buttonPosition.x, buttonPosition.y, -.15f), destination, travel);
        if (t >= .25f && t < .75f)
            demoStar.position += Vector3.up * (.12f * Mathf.Abs(Mathf.Sin((t - .25f) * Mathf.PI * 4f)));

    }

    private void EndDemo()
    {
        if (!presenting) return;
        presenting = false;
        SetDemoVisible(false);
        if (levelCamera != null) levelCamera.EndCinematic();
        if (player != null)
        {
            player.ClearInputForRestartMenu();
            player.enabled = playerWasEnabled;
        }
        Time.timeScale = savedTimeScale;
    }

    private void SetVisible(bool value)
    {
        visible = value;
        buttonVisual.gameObject.SetActive(value);
    }

    private void SetDemoVisible(bool value)
    {
        ghostDoor.enabled = value;
        demoStar.gameObject.SetActive(value);
        caption.enabled = value;
    }

    private SpriteRenderer Square(string name, Transform parent, Vector3 position, Vector2 size, Color color, int order)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.sharedMaterial = iconMaterial;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    private TMP_Text Text(string name, Transform parent, Vector3 position, Vector2 size, float fontSize)
    {
        var obj = new GameObject(name, typeof(TextMeshPro));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        var text = obj.GetComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = size;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.08f, .18f, .22f);
        text.GetComponent<MeshRenderer>().sortingOrder = 24;
        text.text = name;
        return text;
    }

    private Transform Star(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localScale = Vector3.one * .52f;
        obj.GetComponent<MeshFilter>().sharedMesh = starMesh;
        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = iconMaterial;
        renderer.sortingOrder = 23;
        return obj.transform;
    }

    private void CreateVisuals()
    {
        iconMaterial = new Material(Shader.Find("Sprites/Default"));
        var vertices = new Vector3[11];
        var colors = new Color[11];
        var triangles = new int[30];
        colors[0] = new Color(1f, .78f, .12f);
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI * .5f + i * Mathf.PI / 5f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? .5f : .23f);
            colors[i + 1] = colors[0];
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % 10 + 1;
        }
        starMesh = new Mesh { name = "Door hint star", vertices = vertices, colors = colors, triangles = triangles };
        starMesh.RecalculateBounds();
        buttonVisual = new GameObject("Contact Hint Button").transform;
        buttonVisual.SetParent(transform, false);
        buttonVisual.position = new Vector3(buttonPosition.x, buttonPosition.y, -.1f);
        buttonVisual.localScale = Vector3.one * ButtonScale;
        Square("Border", buttonVisual, Vector3.zero, Vector2.one, new Color(.08f, .18f, .22f), 20);
        buttonFace = Square("Face", buttonVisual, new Vector3(0, 0, -.01f), Vector2.one * .88f, Color.white, 21);
        sensor = buttonVisual.gameObject.AddComponent<BoxCollider2D>();
        sensor.size = Vector2.one;
        sensor.isTrigger = true;
        buttonStar = Star("Hint star", buttonVisual);
        buttonStar.localPosition = new Vector3(0f, .08f, -.02f);
        Text("Hint", buttonVisual, new Vector3(0f, -.3f, -.03f), new Vector2(.9f, .3f), 2.2f);
        ghostDoor = Square("Door preview (no collider)", transform, Vector3.zero, Vector2.one, new Color(.5f, .83f, 1f, .32f), 20);
        demoStar = Star("Helpful star", transform);
        caption = Text("Door hint caption", transform, new Vector3(captionPosition.x, captionPosition.y, -.2f), new Vector2(4.9f, 1.2f), 4.4f);
    }

    private void OnDisable()
    {
        EndDemo();
        if (buttonVisual != null) SetVisible(false);
        idleSeconds = 0f;
    }

    private void OnDestroy()
    {
        if (starMesh != null) Destroy(starMesh);
        if (iconMaterial != null) Destroy(iconMaterial);
    }
}
