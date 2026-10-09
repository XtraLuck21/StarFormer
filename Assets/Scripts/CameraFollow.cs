using UnityEngine;

/// <summary>
/// Follows the body the player currently controls (star, box, fan, ...).
/// Attach to the Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;

    [Header("Follow")]
    [Tooltip("Camera position relative to the target.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Tooltip("Higher = snappier camera. 0 = instant.")]
    [SerializeField, Min(0f)] private float smoothSpeed = 8f;

    [Tooltip("How strongly Y follows the player. 0 keeps camera height fixed.")]
    [SerializeField, Range(0f, 1f)] private float verticalWeight = 1f;

    private void Awake()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }
    }

    private void LateUpdate()
    {
        Transform target = ResolveTarget();

        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;

        if (verticalWeight < 1f)
        {
            // Keep most of the camera height stable on platformer jumps.
            desired.y = transform.position.y +
                (desired.y - transform.position.y) * verticalWeight;
        }

        if (smoothSpeed <= 0f)
        {
            transform.position = desired;
            return;
        }

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }

    private Transform ResolveTarget()
    {
        if (player != null)
        {
            PossessableBody body = player.CurrentBody;

            if (body != null)
            {
                return body.transform;
            }
        }

        return null;
    }
}
