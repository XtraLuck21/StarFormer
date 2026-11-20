using UnityEngine;

// Scene-local decorative layer. Runs after tutorial camera reveals without changing the camera.
[ExecuteAlways, DefaultExecutionOrder(1000)]
public sealed class WoodlandCameraBackdrop : MonoBehaviour
{
    public Camera view;
    public SpriteRenderer artwork;
    void LateUpdate()
    {
        if (view == null || artwork == null || artwork.sprite == null) return;
        float height = view.orthographicSize * 2f;
        Vector2 native = artwork.sprite.bounds.size;
        float scale = Mathf.Max(height / native.y, height * view.aspect / native.x);
        transform.position = new Vector3(view.transform.position.x, view.transform.position.y, 1f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
