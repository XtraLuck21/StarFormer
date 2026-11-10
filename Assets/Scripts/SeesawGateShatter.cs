using UnityEngine;

/// <summary>Cracking glass and fading, non-colliding fragments driven by the room reveal.</summary>
public sealed class SeesawGateShatter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    private GameObject effects;
    private LineRenderer[] cracks;
    private SpriteRenderer[] pieces;
    private Vector3[] origins;
    private Color color;

    public void SetProgress(float progress)
    {
        if (progress >= 1f)
        {
            visual.enabled = false;
            if (effects != null) effects.SetActive(false);
            return;
        }
        if (progress <= 0f) return;
        if (effects == null) BuildEffects();
        const float breakAt = .42f;
        float crackProgress = Mathf.Clamp01(progress / breakAt);
        visual.enabled = progress < breakAt;
        for (int i = 0; i < cracks.Length; i++)
        {
            cracks[i].enabled = progress < breakAt;
            Color tint = new Color(.55f, .12f, .12f,
                Mathf.Clamp01(crackProgress * 2f - i * .1f) * .85f);
            cracks[i].startColor = cracks[i].endColor = tint;
        }
        float t = Mathf.Clamp01((progress - breakAt) / (1f - breakAt));
        for (int i = 0; i < pieces.Length; i++)
        {
            SpriteRenderer piece = pieces[i];
            piece.enabled = progress >= breakAt;
            float side = i % 2 == 0 ? -1f : 1f;
            float speed = .65f + (i % 5) * .16f;
            piece.transform.localPosition = origins[i] +
                new Vector3(side * speed * t, (.3f + (i % 3) * .1f) * t - 2.8f * t * t, 0f);
            piece.transform.localRotation = Quaternion.Euler(0f, 0f, side * (55f + i * 13f) * t);
            piece.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
        }
    }

    private void BuildEffects()
    {
        color = visual.color;
        effects = new GameObject("Gate Cracks and Fragments");
        effects.transform.SetParent(transform, false);
        Vector2 size = transform.InverseTransformVector(visual.bounds.size);
        Vector2 center = transform.InverseTransformPoint(visual.bounds.center);
        cracks = new LineRenderer[9];
        for (int i = 0; i < cracks.Length; i++)
        {
            var crack = new GameObject("Glass Crack");
            crack.transform.SetParent(effects.transform, false);
            LineRenderer line = crack.AddComponent<LineRenderer>();
            line.sharedMaterial = visual.sharedMaterial;
            line.sortingLayerID = visual.sortingLayerID;
            line.sortingOrder = visual.sortingOrder + 1;
            line.useWorldSpace = false;
            line.widthMultiplier = .025f;
            line.positionCount = 5;
            float y = center.y + size.y * ((i + .5f) / cracks.Length - .5f);
            for (int j = 0; j < 5; j++)
                line.SetPosition(j, new Vector3(center.x + size.x * (j / 4f - .5f),
                    y + (j % 2 == 0 ? -.13f : .13f) + (j - 2) * .075f, 0f));
            cracks[i] = line;
        }
        const int rows = 18;
        pieces = new SpriteRenderer[rows * 2];
        origins = new Vector3[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            var part = new GameObject("Glass Fragment");
            part.transform.SetParent(effects.transform, false);
            origins[i] = new Vector3(center.x + (i % 2 == 0 ? -.25f : .25f) * size.x,
                center.y + ((i / 2 + .5f) / rows - .5f) * size.y, 0f);
            part.transform.localScale = new Vector3(size.x * .46f / visual.sprite.bounds.size.x,
                size.y / rows * .92f / visual.sprite.bounds.size.y, 1f);
            SpriteRenderer piece = part.AddComponent<SpriteRenderer>();
            piece.sprite = visual.sprite;
            piece.sharedMaterial = visual.sharedMaterial;
            piece.sortingLayerID = visual.sortingLayerID;
            piece.sortingOrder = visual.sortingOrder;
            pieces[i] = piece;
        }
    }
}
