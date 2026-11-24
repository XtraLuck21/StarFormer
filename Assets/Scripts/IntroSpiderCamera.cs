using UnityEngine;

/// <summary>Frame the tutorial and pan when Spider climbs near a screen edge.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class IntroSpiderCamera : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private Vector2 structureCenter = new Vector2(-0.2f, -0.7f);
    [SerializeField] private Vector2 structureSize = new Vector2(26.4f, 9.6f);
    [SerializeField, Min(0f)] private float structurePadding = 0.25f;

    private Camera sceneCamera;
    private Vector2 panVelocity;

    private void OnEnable()
    {
        sceneCamera = GetComponent<Camera>();
        panVelocity = Vector2.zero;
        SetCenter(structureCenter);
        FrameMap();
    }

    private void LateUpdate() => FrameMap();

    private void FrameMap()
    {
        float aspect = Mathf.Max(sceneCamera.aspect, .01f);
        sceneCamera.orthographic = true;
        sceneCamera.orthographicSize = Mathf.Max(structureSize.y * 0.5f,
            structureSize.x * 0.5f / aspect) + structurePadding;

        if (!Application.IsPlaying(gameObject))
        {
            SetCenter(structureCenter);
            return;
        }
        if (Time.deltaTime <= 0f) return;

        Vector2 halfView = new Vector2(aspect, 1f) * sceneCamera.orthographicSize;
        PossessableBody body = player != null ? player.CurrentBody : null;
        bool trackSpider = body != null && body.isActiveAndEnabled && body.Form == BodyForm.Spider &&
            body.BodyCollider != null && !player.IsDead && !player.IsRestarting;
        Bounds spider = trackSpider ? body.BodyCollider.bounds : default;
        Vector2 target = trackSpider ? KeepInView(structureCenter, spider, halfView * .83f) : structureCenter;
        Vector2 center = Vector2.SmoothDamp(transform.position, target, ref panVelocity, .17f);
        // A fast web pull can cross the soft margin within one frame.
        SetCenter(trackSpider ? KeepInView(center, spider, halfView * .96f) : center);
    }

    private static Vector2 KeepInView(Vector2 center, Bounds body, Vector2 halfView)
    {
        Vector2 midpoint = body.center;
        Vector2 space = Vector2.Max(Vector2.zero, halfView - (Vector2)body.extents);
        return midpoint + new Vector2(Mathf.Clamp(center.x - midpoint.x, -space.x, space.x),
            Mathf.Clamp(center.y - midpoint.y, -space.y, space.y));
    }

    private void SetCenter(Vector2 center) => transform.position = new Vector3(center.x, center.y, transform.position.z);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(structureCenter, structureSize);
    }
}
