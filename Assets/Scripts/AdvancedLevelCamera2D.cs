using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AdvancedLevelCamera2D : MonoBehaviour
{
    private enum Region { Entrance, WaterTransfer, DoorRoom, RightAscent, UpperReturn }

    [Serializable]
    private class View
    {
        public Vector2 center;
        public Vector2 followRange;
        public Vector2 offset;
        [Min(0.1f)] public float size;
        [Min(0f)] public float lookAhead;

        public View(Vector2 center, Vector2 range, Vector2 offset, float size, float lookAhead = 0f)
        {
            this.center = center;
            followRange = range;
            this.offset = offset;
            this.size = size;
            this.lookAhead = lookAhead;
        }
    }

    [SerializeField] private PlayerPossessionController playerController;

    [Header("Regional Framing (16:9)")]
    [SerializeField] private View entrance = new(new(8.5f, 4.7f), new(1f, 0f), new(2f, 0f), 4.2f);
    [SerializeField] private View waterTransfer = new(new(12.9f, 5.8f), new(2.3f, 0.3f), new(1.5f, 1.2f), 5.6f);
    [SerializeField] private View doorRoom = new(new(31f, 5.8f), Vector2.zero, Vector2.zero, 7.1f);
    [SerializeField] private View rightAscent = new(new(36.8f, 9.7f), new(0f, 3.7f), new(0f, 2f), 4.5f);
    [SerializeField] private View upperReturn = new(new(22.15f, 14.1f), new(14.65f, 0f), Vector2.zero, 4f, 1.5f);

    [Header("Region Boundaries")]
    [SerializeField] private float waterEntryX = 7.5f;
    [SerializeField] private float dropShaftX = 18.9f;
    [SerializeField] private float dropArrivalY = 3.8f;
    [SerializeField] private float doorRoomEntryX = 21f;
    [SerializeField] private float ascentEntryX = 39.4f;
    [SerializeField] private float upperFloorY = 11.3f;
    [SerializeField] private float upperLandingX = 40.8f;
    [SerializeField, Min(0f)] private float boundaryHysteresis = 0.35f;

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.35f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.5f;
    [SerializeField, Range(0.02f, 0.2f)] private float targetScreenMargin = 0.08f;

    private Camera viewCamera;
    private Region region;
    private PossessableBody previousBody;
    private Vector2 previousPosition;
    private Vector3 positionVelocity;
    private float zoomVelocity;
    private float facing = -1f;
    private bool initialized;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerPossessionController>();
    }

    private void OnEnable()
    {
        initialized = false;
        positionVelocity = Vector3.zero;
        zoomVelocity = 0f;
        previousBody = null;
        facing = -1f;
    }

    private void LateUpdate()
    {
        UpdateView(Time.deltaTime);
    }

    private void UpdateView(float deltaTime)
    {
        var body = playerController != null ? playerController.CurrentBody : null;
        if (body == null || deltaTime <= 0f) return;

        // Use the rendered body position so interpolated physics remains visually smooth.
        Vector2 point = body.transform.position;
        if (body == previousBody)
        {
            float speedX = (point.x - previousPosition.x) / deltaTime;
            if (Mathf.Abs(speedX) > 0.25f) facing = Mathf.Sign(speedX);
        }
        previousBody = body;
        previousPosition = point;

        region = SelectRegion(point);
        View view = GetView(region);
        Vector2 aim = point + view.offset + Vector2.right * (facing * view.lookAhead);
        // A Ball reaching U2 should not make the ascent camera bounce vertically.
        if (region == Region.RightAscent && body.Form == BodyForm.BouncyBall)
            aim.y = rightAscent.center.y - rightAscent.followRange.y;

        Vector3 destination = new(
            Mathf.Clamp(aim.x, view.center.x - view.followRange.x, view.center.x + view.followRange.x),
            Mathf.Clamp(aim.y, view.center.y - view.followRange.y, view.center.y + view.followRange.y),
            transform.position.z);
        float desiredSize = view.size * Mathf.Max(1f, (16f / 9f) / Mathf.Max(0.1f, viewCamera.aspect));

        if (!initialized)
        {
            transform.position = destination;
            viewCamera.orthographicSize = desiredSize;
            initialized = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, destination,
                ref positionVelocity, positionSmoothTime, Mathf.Infinity, deltaTime);
            viewCamera.orthographicSize = Mathf.SmoothDamp(viewCamera.orthographicSize,
                desiredSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, deltaTime);
        }

        // Keep fast falls and possession transfers visible while a region transition settles.
        KeepTargetVisible(point);
    }

    private Region SelectRegion(Vector2 point)
    {
        float h = initialized ? boundaryHysteresis : 0f;
        float upperY = upperFloorY - (initialized && region == Region.UpperReturn ? h : 0f);
        float landingX = upperLandingX + (initialized && region == Region.UpperReturn ? h : 0f);
        if (point.y >= upperY && point.x <= landingX) return Region.UpperReturn;

        float ascentX = ascentEntryX - (initialized && region == Region.RightAscent ? h : 0f);
        if (point.x >= ascentX) return Region.RightAscent;

        bool wasInDoor = initialized && region == Region.DoorRoom;
        if (point.x >= doorRoomEntryX - (wasInDoor ? h : 0f) ||
            (point.x >= dropShaftX - (wasInDoor ? h : 0f) &&
             point.y <= dropArrivalY + (wasInDoor ? h : 0f)))
            return Region.DoorRoom;

        float waterX = waterEntryX + (initialized && region == Region.Entrance ? h : -h);
        return point.x >= waterX ? Region.WaterTransfer : Region.Entrance;
    }

    private View GetView(Region selection)
    {
        switch (selection)
        {
            case Region.Entrance: return entrance;
            case Region.WaterTransfer: return waterTransfer;
            case Region.DoorRoom: return doorRoom;
            case Region.RightAscent: return rightAscent;
            default: return upperReturn;
        }
    }

    private void KeepTargetVisible(Vector2 point)
    {
        float halfHeight = viewCamera.orthographicSize * (1f - 2f * targetScreenMargin);
        float halfWidth = halfHeight * viewCamera.aspect;
        Vector3 position = transform.position;
        float x = Mathf.Clamp(position.x, point.x - halfWidth, point.x + halfWidth);
        float y = Mathf.Clamp(position.y, point.y - halfHeight, point.y + halfHeight);
        if (!Mathf.Approximately(x, position.x)) positionVelocity.x = 0f;
        if (!Mathf.Approximately(y, position.y)) positionVelocity.y = 0f;
        transform.position = new Vector3(x, y, position.z);
    }
}
