using UnityEngine;

/// <summary>Raises the fan barrier once the seesaw's left end reaches its final height.</summary>
[DisallowMultipleComponent]
public sealed class FanBlockLift : MonoBehaviour
{
    [SerializeField] private ObstacleSeesawTilt2D seesaw;
    [SerializeField] private float targetWorldY = -2.78f;
    [SerializeField, Min(0.05f)] private float liftDuration = 0.8f;

    private bool lifting;
    private bool completed;
    private float startY;
    private float elapsed;

    private void Awake()
    {
        if (seesaw != null) return;
        Debug.LogError("Assign the seesaw to FanBlockLift.", this);
        enabled = false;
    }

    private void LateUpdate()
    {
        if (completed || seesaw == null) return;
        if (!lifting)
        {
            if (!seesaw.HasTriggered || seesaw.IsTilting) return;
            startY = transform.position.y;
            lifting = true;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, liftDuration));
        Vector3 position = transform.position;
        position.y = Mathf.Lerp(startY, targetWorldY, Mathf.SmoothStep(0f, 1f, t));
        transform.position = position;
        completed = t >= 1f;
    }
}
