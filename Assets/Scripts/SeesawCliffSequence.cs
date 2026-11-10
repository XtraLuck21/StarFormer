using UnityEngine;

public sealed class SeesawCliffSequence : MonoBehaviour
{
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private SeesawCliffBridge bridge;
    public bool BluePressed { get; private set; }
    public bool GreenPressed { get; private set; }
    public bool IsComplete { get; private set; }
    public void PressBlue() { BluePressed = true; CheckComplete(); }
    public void PressGreen() { GreenPressed = true; CheckComplete(); }

    private void CheckComplete()
    {
        if (IsComplete || !BluePressed || !GreenPressed) return;
        IsComplete = true;
        supplies.CompletePuzzle();
        bridge.Extend();
    }
}
