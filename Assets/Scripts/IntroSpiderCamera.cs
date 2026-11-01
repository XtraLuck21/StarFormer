using UnityEngine;

/// <summary>Follow the entry, frame the climbing structure, then follow the exit.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class IntroSpiderCamera : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;

    [Header("Entry Follow")]
    [SerializeField] private Vector2 entryOffset = new Vector2(1f, 1f);
    [SerializeField, Min(0.1f)] private float entrySize = 5f;

    [Header("Fixed Structure View")]
    [SerializeField] private Vector2 structureCenter = Vector2.zero;
    [SerializeField] private Vector2 structureSize = new Vector2(28f, 12f);
    [SerializeField, Min(0f)] private float structurePadding = 0.75f;

    [Header("Exit Follow")]
    [SerializeField] private float exitStartX = 14.4f;
    [SerializeField, Min(0f)] private float returnHysteresis = 1.2f;
    [SerializeField] private Vector2 exitOffset = new Vector2(1f, 0f);
    [SerializeField, Min(0.1f)] private float exitSize = 4.5f;

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.45f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.55f;

    private Camera sceneCamera;
    private Vector3 positionVelocity;
    private float zoomVelocity;
    private bool hasPossessedSpider;
    private bool followingExit;
    private bool initialized;

    private void Awake()
    {
        sceneCamera = GetComponent<Camera>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerPossessionController>();
    }

    private void LateUpdate()
    {
        PossessableBody target = playerController != null ? playerController.CurrentBody : null;
        if (target == null || playerController.IsDead) return;

        if (target.Form == BodyForm.Spider) hasPossessedSpider = true;
        Vector2 position = target.transform.position;
        if (followingExit)
            followingExit = position.x > exitStartX - returnHysteresis;
        else
            followingExit = position.x >= exitStartX;

        Vector2 center;
        float size;
        if (followingExit)
        {
            center = position + exitOffset;
            size = exitSize;
        }
        else if (hasPossessedSpider)
        {
            center = structureCenter;
            // Orthographic size is half the height; also fit the width on narrow displays.
            size = Mathf.Max(structureSize.y * 0.5f,
                structureSize.x * 0.5f / Mathf.Max(sceneCamera.aspect, 0.01f)) + structurePadding;
        }
        else
        {
            center = position + entryOffset;
            size = entrySize;
        }

        Vector3 destination = new Vector3(center.x, center.y, transform.position.z);
        if (!initialized)
        {
            // Avoid panning across the level from the camera's editor position on startup.
            transform.position = destination;
            sceneCamera.orthographicSize = size;
            initialized = true;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, destination,
            ref positionVelocity, positionSmoothTime);
        sceneCamera.orthographicSize = Mathf.SmoothDamp(sceneCamera.orthographicSize,
            size, ref zoomVelocity, zoomSmoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(structureCenter, structureSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(exitStartX, -6f, 0f), new Vector3(exitStartX, 8f, 0f));
    }
}
