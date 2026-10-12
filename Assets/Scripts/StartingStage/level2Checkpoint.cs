using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class level2Checkpoint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private Transform respawnPoint;

    [Header("Identity")]
    [SerializeField] private string checkpointId = "CP01";

    [Header("Events")]
    // 玩家进入这个检查点时执行。
    [SerializeField] private UnityEvent onCheckpointReached =
        new UnityEvent();

    // 死亡后从这个检查点重生时执行。
    [SerializeField] private UnityEvent onRespawnHere =
        new UnityEvent();

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }
    }

    private void Start()
    {
        if (player != null &&
            player.IsCurrentCheckpoint(checkpointId))
        {
            onRespawnHere.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (player == null ||
            player.IsDead ||
            respawnPoint == null ||
            string.IsNullOrWhiteSpace(checkpointId))
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null ||
            !otherBody.TryGetComponent<PossessableBody>(out var candidate) ||
            candidate != player.CurrentBody)
        {
            return;
        }

        // 同一个检查点不重复保存或执行事件。
        if (player.IsCurrentCheckpoint(checkpointId))
        {
            return;
        }

        player.SetCheckpoint(checkpointId, respawnPoint.position);
        onCheckpointReached.Invoke();
    }
}