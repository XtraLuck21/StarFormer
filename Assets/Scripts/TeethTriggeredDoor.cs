using UnityEngine;

/// <summary>Open the door permanently once every linked tooth has reached its zone.</summary>
public class TeethTriggeredDoor : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private TeethArrivalTriggerZone[] requiredTriggers;

    public bool IsOpen { get; private set; }

    private void Start()
    {
        TryOpen();
    }

    // Called by each zone's On Tooth Arrived event, after its state is recorded.
    public void TryOpen()
    {
        if (!isActiveAndEnabled || IsOpen || door == null ||
            requiredTriggers == null || requiredTriggers.Length == 0)
        {
            return;
        }

        foreach (TeethArrivalTriggerZone trigger in requiredTriggers)
        {
            if (trigger == null || !trigger.HasArrived)
            {
                return;
            }
        }

        IsOpen = true;
        door.SetActive(false);
    }
}
