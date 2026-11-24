using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>A one-use colored contact on the board, paired with one room button.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawColorContact : MonoBehaviour
{
    [SerializeField] private BoxCollider2D matchingButton;
    [SerializeField] private bool dissolvePairOnPress;
    [SerializeField] private UnityEvent onPressed = new UnityEvent();

    public bool IsPressed { get; private set; }
    private BoxCollider2D sensor;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        sensor.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryPress(other);
    private void OnTriggerStay2D(Collider2D other) => TryPress(other);

    private void TryPress(Collider2D other)
    {
        if (IsPressed || !isActiveAndEnabled || other != matchingButton ||
            matchingButton == null || !matchingButton.isActiveAndEnabled) return;

        Press(dissolvePairOnPress);
    }

    public void RestorePressed() => Press(false);

    private void Press(bool animate)
    {
        if (IsPressed) return;

        IsPressed = true;
        // Disable contact immediately, then run any configured button action once.
        sensor.enabled = false;
        matchingButton.enabled = false;
        try
        {
            onPressed.Invoke();
        }
        finally
        {
            if (animate && isActiveAndEnabled && matchingButton.gameObject.activeInHierarchy)
                StartCoroutine(DissolvePair());
            else
                HidePair();
        }
    }

    private IEnumerator DissolvePair()
    {
        Transform key = transform;
        Transform target = matchingButton.transform;
        Vector3 keyStart = key.position, lockStart = target.position;
        Vector3 keyScale = key.localScale, lockScale = target.localScale;
        Vector3 meet = (keyStart + lockStart) * .5f;
        const float mergeSeconds = .24f;
        float elapsed = 0f;
        while (elapsed < mergeSeconds)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / mergeSeconds));
            key.position = Vector3.Lerp(keyStart, meet, progress);
            target.position = Vector3.Lerp(lockStart, meet, progress);
            key.localScale = keyScale * Mathf.Lerp(1f, .015f, progress);
            target.localScale = lockScale * Mathf.Lerp(1f, .015f, progress);
            yield return null;
        }

        var sprite = GetComponent<SpriteRenderer>();
        var wave = new GameObject("Seesaw key and lock dissolve");
        wave.transform.SetParent(key, false);
        var ring = wave.AddComponent<LineRenderer>();
        ring.sharedMaterial = sprite.sharedMaterial;
        ring.sortingLayerID = sprite.sortingLayerID;
        ring.sortingOrder = sprite.sortingOrder + 8;
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = 32;
        const float fadeSeconds = .32f;
        elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeSeconds);
            float radius = Mathf.Lerp(.16f, .85f, 1f - (1f - t) * (1f - t));
            Color color = Color.Lerp(sprite.color, Color.white, .27f);
            color.a *= 1f - t;
            ring.startColor = ring.endColor = color;
            ring.widthMultiplier = Mathf.Lerp(.15f, .025f, t);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * (2f * Mathf.PI / ring.positionCount);
                ring.SetPosition(i, meet + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
            yield return null;
        }
        Destroy(wave);
        HidePair();
    }

    private void HidePair()
    {
        matchingButton.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
