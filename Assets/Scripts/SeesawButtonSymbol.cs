using UnityEngine;

/// <summary>High-contrast, font-independent symbols on matching seesaw buttons and contacts.</summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SeesawButtonSymbol : MonoBehaviour
{
    public enum Symbol { Lock, Key, Spider }
    [SerializeField] private Symbol symbol;
    private GameObject drawing;
    private SpriteRenderer button;
    private Vector3 drawnScale;
    private int drawnSortingOrder, drawnSortingLayer;
    private bool rebuildRequested;

    private void OnEnable() => rebuildRequested = true;
    private void OnValidate() => rebuildRequested = true;

    private void Update()
    {
        if (!rebuildRequested && drawnScale == transform.lossyScale && button != null &&
            drawnSortingOrder == button.sortingOrder && drawnSortingLayer == button.sortingLayerID) return;
        rebuildRequested = false;
        Rebuild();
    }

    private void Rebuild()
    {
        ClearDrawing();
        button = GetComponent<SpriteRenderer>();
        if (button.sprite == null) return;
        drawnScale = transform.lossyScale;
        drawnSortingOrder = button.sortingOrder;
        drawnSortingLayer = button.sortingLayerID;
        float sx = Mathf.Max(.001f, Mathf.Abs(drawnScale.x));
        float sy = Mathf.Max(.001f, Mathf.Abs(drawnScale.y));
        Vector2 size = new Vector2(button.sprite.bounds.size.x * sx, button.sprite.bounds.size.y * sy);
        float unit = Mathf.Min(size.y * .8f, .58f);
        drawing = new GameObject("Seesaw Button " + symbol + " Drawing");
        drawing.hideFlags = HideFlags.HideAndDontSave;
        drawing.transform.SetParent(transform, false);
        drawing.transform.localPosition = button.sprite.bounds.center;
        // Preserve proportions on the wide, shallow button while following its rotation.
        drawing.transform.localScale = new Vector3(1f / sx, 1f / sy, 1f);

        Vector2 half = size * .5f - Vector2.one * .025f;
        Stroke("Button Rim", .024f, new Color(1f, 1f, 1f, .8f), true,
            new Vector2(-half.x, -half.y), new Vector2(-half.x, half.y),
            new Vector2(half.x, half.y), new Vector2(half.x, -half.y));
        Color ink = new Color32(14, 25, 40, 255);
        if (symbol == Symbol.Lock)
        {
            Stroke("Lock Body", .05f, ink, true,
                new Vector2(-.34f, -.45f) * unit, new Vector2(-.34f, .08f) * unit,
                new Vector2(.34f, .08f) * unit, new Vector2(.34f, -.45f) * unit);
            var arch = new Vector2[15];
            arch[0] = new Vector2(-.23f, .08f) * unit;
            for (int i = 0; i < 13; i++)
            {
                float a = Mathf.PI * (1f - i / 12f);
                arch[i + 1] = new Vector2(Mathf.Cos(a) * .23f, .27f + Mathf.Sin(a) * .23f) * unit;
            }
            arch[14] = new Vector2(.23f, .08f) * unit;
            Stroke("Lock Shackle", .05f, ink, false, arch);
            Stroke("Lock Keyhole", .055f, ink, false, new Vector2(0f, -.11f) * unit, new Vector2(0f, -.29f) * unit);
        }
        else if (symbol == Symbol.Key)
        {
            var ring = new Vector2[24];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i * Mathf.PI * 2f / ring.Length;
                ring[i] = new Vector2(-.55f + Mathf.Cos(a) * .32f, Mathf.Sin(a) * .32f) * unit;
            }
            Stroke("Key Bow", .05f, ink, true, ring);
            Stroke("Key Shaft", .05f, ink, false, new Vector2(-.23f, 0f) * unit, new Vector2(.9f, 0f) * unit);
            Stroke("Key Tooth One", .05f, ink, false, new Vector2(.5f, 0f) * unit, new Vector2(.5f, -.28f) * unit);
            Stroke("Key Tooth Two", .05f, ink, false, new Vector2(.82f, 0f) * unit, new Vector2(.82f, -.28f) * unit);
        }
        else
        {
            // Match the current Spider placeholder: a yellow hexagon.
            // The button supplies the yellow fill; the dark edge keeps it legible.
            var body = new Vector2[6];
            for (int i = 0; i < body.Length; i++)
            {
                float a = i * Mathf.PI / 3f;
                body[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (.52f * unit);
            }
            Stroke("Spider Hexagon", .04f, ink, true, body);
        }
        // Yellow is drawn behind the ceiling while emerging; reveal its mark
        // with the button when the room stage restores the foreground order.
        drawing.SetActive(button.sortingOrder >= 0);
    }

    private void Stroke(string name, float width, Color color, bool loop, params Vector2[] points)
    {
        var part = new GameObject(name);
        part.hideFlags = HideFlags.HideAndDontSave;
        part.transform.SetParent(drawing.transform, false);
        var line = part.AddComponent<LineRenderer>();
        line.sharedMaterial = button.sharedMaterial;
        line.useWorldSpace = false;
        line.loop = loop;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.startColor = line.endColor = color;
        line.sortingLayerID = button.sortingLayerID;
        line.sortingOrder = button.sortingOrder + 2;
        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i]);
    }

    private void OnDisable() => ClearDrawing();
    private void OnDestroy() => ClearDrawing();

    private void ClearDrawing()
    {
        if (drawing == null) return;
        drawing.SetActive(false);
        if (Application.IsPlaying(gameObject)) Destroy(drawing);
        else DestroyImmediate(drawing);
        drawing = null;
    }
}
