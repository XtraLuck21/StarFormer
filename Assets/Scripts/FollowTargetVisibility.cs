using UnityEngine;

/// <summary>
/// Shows this object's children only while the target is active, and moves them with the target.
/// Used for pins fixed to the Smile Face return wall, which is hidden until it rises.
/// </summary>
[DisallowMultipleComponent]
public sealed class FollowTargetVisibility : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Target world position at which the children sit at their authored positions.")]
    [SerializeField] private Vector3 targetRestPosition;

    private Vector3 restPosition;
    private bool shown = true;

    private void Awake() => restPosition = transform.position;

    private void LateUpdate()
    {
        if (target == null) return;
        bool visible = target.gameObject.activeInHierarchy;
        if (visible != shown)
        {
            shown = visible;
            foreach (Transform child in transform) child.gameObject.SetActive(visible);
        }
        if (visible) transform.position = restPosition + (target.position - targetRestPosition);
    }
}
