using UnityEngine;

/// <summary>Passing the solved tooth door changes the player to Spider in place.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileDoorSpiderGate : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private TeethTriggeredDoor toothDoor;
    [SerializeField] private SmileFaceFinalRoute route;
    [SerializeField] private PossessableBody spider;
    [SerializeField] private Vector3 spiderCheckpointPosition = new Vector3(58.5f, 6.4f, 0f);
    public Vector3 SpiderCheckpointPosition => spiderCheckpointPosition;
    public bool HasChangedForm { get; private set; }
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void Start() => HasChangedForm = SmileFaceFinalRoute.IsRouteCheckpoint(player);
    private void OnTriggerEnter2D(Collider2D other) => Enter(other);
    private void OnTriggerStay2D(Collider2D other) => Enter(other);
    private void Enter(Collider2D other)
    {
        if (HasChangedForm || !toothDoor.IsOpen || player.IsDead || player.IsRestarting || Time.timeScale <= 0f ||
            other.attachedRigidbody == null || player.CurrentBody == null ||
            other.attachedRigidbody != player.CurrentBody.Body) return;
        // Convert below the inner neck's opening so the next input leads straight
        // through the passage to the outer climbing wall, even after a high bounce.
        Vector2 position = player.CurrentBody.Body.position;
        if (position.x < 56.2f || position.y > 8.3f) return;
        if (!player.TryEnterSpiderAtGate(spider)) return;
        HasChangedForm = true;
        route.SaveCheckpoint(false, spiderCheckpointPosition);
    }
}
