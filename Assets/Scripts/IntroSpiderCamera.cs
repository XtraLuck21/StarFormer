using UnityEngine;

/// <summary>Keep the whole IntroSpider map visible, including on narrow displays.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class IntroSpiderCamera : MonoBehaviour
{
    [SerializeField] private Vector2 structureCenter = new Vector2(-0.2f, -0.7f);
    [SerializeField] private Vector2 structureSize = new Vector2(26.4f, 9.6f);
    [SerializeField, Min(0f)] private float structurePadding = 0.25f;

    private void OnEnable() => FrameMap();
    private void LateUpdate() => FrameMap();

    private void FrameMap()
    {
        Camera sceneCamera = GetComponent<Camera>();
        sceneCamera.orthographic = true;
        sceneCamera.orthographicSize = Mathf.Max(structureSize.y * 0.5f,
            structureSize.x * 0.5f / Mathf.Max(sceneCamera.aspect, 0.01f)) + structurePadding;
        transform.position = new Vector3(structureCenter.x, structureCenter.y, transform.position.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(structureCenter, structureSize);
    }
}
