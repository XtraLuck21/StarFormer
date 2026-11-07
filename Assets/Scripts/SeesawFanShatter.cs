using UnityEngine;

/// <summary>Retains a snapshot of the procedural Fan sprite while its fragments fade.</summary>
public sealed class SeesawFanShatter : MonoBehaviour
{
    private Texture2D texture;
    private Sprite sprite;

    public static void Play(PossessableBody fan)
    {
        FanVisual visual = fan.GetComponentInChildren<FanVisual>();
        SpriteRenderer source = visual != null ? visual.GetComponent<SpriteRenderer>() : fan.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null) return;

        var effectObject = new GameObject("Fan Spike Shatter");
        effectObject.transform.position = fan.transform.position;
        var snapshot = effectObject.AddComponent<SeesawFanShatter>();
        snapshot.texture = Instantiate(source.sprite.texture);
        Rect rect = source.sprite.rect;
        Vector2 pivot = source.sprite.pivot;
        snapshot.sprite = Sprite.Create(snapshot.texture, rect,
            new Vector2(pivot.x / rect.width, pivot.y / rect.height), source.sprite.pixelsPerUnit);
        var renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = snapshot.sprite;
        renderer.color = source.color;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.enabled = false;
        effectObject.AddComponent<ShatterEffect>().Play();
        Destroy(effectObject, 2.1f);
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
