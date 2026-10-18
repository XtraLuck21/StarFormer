using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RegionalCameraFollow2D : MonoBehaviour
{
    private enum CameraRegion
    {
        LowerLeft,
        LowerRight,
        Upper
    }

    [Header("Target")]
    [SerializeField] private PlayerPossessionController playerController;

    [Header("Region Boundaries")]
    [SerializeField] private float lowerSplitX = 16f;
    [SerializeField] private float upperRegionY = 8.5f;
    [SerializeField, Min(0f)] private float boundaryHysteresis = 0.5f;

    [Header("Lower Left View")]
    [SerializeField] private Vector2 lowerLeftCenter = new(3f, -2.5f);
    [SerializeField] private Vector2 lowerLeftFollowRange = new(6f, 1.5f);
    [SerializeField, Min(0.1f)] private float lowerLeftSize = 10f;

    [Header("Lower Right View")]
    [SerializeField] private Vector2 lowerRightCenter = new(34f, -2.5f);
    [SerializeField] private Vector2 lowerRightFollowRange = new(6f, 1.5f);
    [SerializeField, Min(0.1f)] private float lowerRightSize = 10f;

    [Header("Upper View")]
    [SerializeField] private Vector2 upperCenter = new(17f, 11f);
    [SerializeField] private Vector2 upperFollowRange = new(21f, 0.5f);
    [SerializeField, Min(0.1f)] private float upperSize = 5.25f;

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.45f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.35f;
    [SerializeField, Min(0f)] private float switchZoomAmount = 1f;
    [SerializeField, Min(0f)] private float switchZoomDuration = 0.45f;

    private Camera attachedCamera;
    private CameraRegion currentRegion;
    private Vector3 positionVelocity;
    private float zoomVelocity;
    private float switchZoomEndTime;
    private bool isInitialized;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        PossessableBody target = playerController != null
            ? playerController.CurrentBody
            : null;

        if (target == null)
        {
            return;
        }

        Vector2 targetPosition = target.Body != null
            ? target.Body.position
            : (Vector2)target.transform.position;

        CameraRegion nextRegion = SelectRegion(targetPosition);

        if (!isInitialized)
        {
            currentRegion = nextRegion;
            Vector2 initialPosition = GetDesiredPosition(
                currentRegion,
                targetPosition
            );

            transform.position = new Vector3(
                initialPosition.x,
                initialPosition.y,
                transform.position.z
            );
            attachedCamera.orthographicSize = GetRegionSize(currentRegion);
            isInitialized = true;
            return;
        }

        if (nextRegion != currentRegion)
        {
            currentRegion = nextRegion;
            switchZoomEndTime = Time.time + switchZoomDuration;
        }

        Vector2 desiredPosition = GetDesiredPosition(
            currentRegion,
            targetPosition
        );
        Vector3 destination = new(
            desiredPosition.x,
            desiredPosition.y,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            destination,
            ref positionVelocity,
            positionSmoothTime
        );

        float desiredSize = GetRegionSize(currentRegion);

        if (Time.time < switchZoomEndTime)
        {
            desiredSize += switchZoomAmount;
        }

        attachedCamera.orthographicSize = Mathf.SmoothDamp(
            attachedCamera.orthographicSize,
            desiredSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    private CameraRegion SelectRegion(Vector2 targetPosition)
    {
        if (!isInitialized)
        {
            if (targetPosition.y >= upperRegionY)
            {
                return CameraRegion.Upper;
            }

            return targetPosition.x < lowerSplitX
                ? CameraRegion.LowerLeft
                : CameraRegion.LowerRight;
        }

        if (currentRegion == CameraRegion.Upper)
        {
            if (targetPosition.y >= upperRegionY - boundaryHysteresis)
            {
                return CameraRegion.Upper;
            }
        }
        else if (targetPosition.y >= upperRegionY + boundaryHysteresis)
        {
            return CameraRegion.Upper;
        }

        if (currentRegion == CameraRegion.LowerLeft &&
            targetPosition.x < lowerSplitX + boundaryHysteresis)
        {
            return CameraRegion.LowerLeft;
        }

        if (currentRegion == CameraRegion.LowerRight &&
            targetPosition.x >= lowerSplitX - boundaryHysteresis)
        {
            return CameraRegion.LowerRight;
        }

        return targetPosition.x < lowerSplitX
            ? CameraRegion.LowerLeft
            : CameraRegion.LowerRight;
    }

    private Vector2 GetDesiredPosition(
        CameraRegion region,
        Vector2 targetPosition)
    {
        Vector2 center;
        Vector2 range;

        switch (region)
        {
            case CameraRegion.LowerRight:
                center = lowerRightCenter;
                range = lowerRightFollowRange;
                break;

            case CameraRegion.Upper:
                center = upperCenter;
                range = upperFollowRange;
                break;

            default:
                center = lowerLeftCenter;
                range = lowerLeftFollowRange;
                break;
        }

        Vector2 offset = targetPosition - center;
        offset.x = Mathf.Clamp(offset.x, -range.x, range.x);
        offset.y = Mathf.Clamp(offset.y, -range.y, range.y);
        return center + offset;
    }

    private float GetRegionSize(CameraRegion region)
    {
        return region switch
        {
            CameraRegion.LowerRight => lowerRightSize,
            CameraRegion.Upper => upperSize,
            _ => lowerLeftSize
        };
    }
}
