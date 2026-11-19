using UnityEngine;

/// <summary>Keeps the beam-mounted flag upright and unlocks its goal after the seesaw finishes tilting.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class SeesawGoalLift : MonoBehaviour
{
    [SerializeField] private ObstacleSeesawTilt2D seesaw;
    [SerializeField] private BoxCollider2D goalTrigger;

    private Quaternion uprightRotation;

    private void Awake()
    {
        uprightRotation = transform.rotation;
        if (goalTrigger != null) goalTrigger.enabled = false;
        if (seesaw == null || goalTrigger == null)
        {
            Debug.LogError("Assign the seesaw and the flag's goal trigger to SeesawGoalLift.", this);
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        // Position follows the actual beam through the hierarchy, including any newly authored placement.
        transform.rotation = uprightRotation;
        // Keep the goal locked until the beam reaches its final angle.
        goalTrigger.enabled = seesaw.HasTriggered && !seesaw.IsTilting;
    }
}
