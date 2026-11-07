using TMPro;
using UnityEngine;

/// <summary>Level 1's checkpoint presentation, saving the room position and completed blue/green stage.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawCheckpoint : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private string checkpointId = "SEESAW_ROOM";
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private Collider2D board;
    [SerializeField] private level2Camera roomCamera;
    [SerializeField] private DarkStarPassage passage;
    [SerializeField] private SeesawRoomStage roomStage;
    [SerializeField] private LineRenderer outline;
    [SerializeField] private TMP_Text savedLabel;
    [SerializeField] private Color unsavedColor = new Color32(47, 155, 236, 255);
    [SerializeField] private Color savedColor = new Color32(255, 211, 77, 255);

    private float savedUntil;
    private static string completedStageScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStageSession() => completedStageScene = null;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        savedLabel.enabled = false;
    }

    private void Start()
    {
        if (player.IsCurrentCheckpoint(checkpointId))
        {
            if (roomStage != null && completedStageScene == gameObject.scene.path) roomStage.RestoreUnlocked();
            PrepareRoom();
        }
        RefreshVisual();
    }

    private void PrepareRoom()
    {
        if (passage != null) passage.CompletePassage();
        supplies.EnterRoom();
        roomCamera.BeginSeesawRoom(board);
    }

    private void OnTriggerEnter2D(Collider2D other) => TrySave(other);
    private void OnTriggerStay2D(Collider2D other) => TrySave(other);

    private void TrySave(Collider2D other)
    {
        if (player.IsDead || player.IsRestarting || Time.timeScale <= 0f ||
            player.IsCurrentCheckpoint(checkpointId) || other.attachedRigidbody == null ||
            !other.attachedRigidbody.TryGetComponent(out PossessableBody body) ||
            body != player.CurrentBody || !body.IsControlled) return;
        completedStageScene = null;
        player.SetCheckpoint(checkpointId, spawnPosition, showFeedback: false);
        if (roomStage != null && roomStage.IsUnlocked) SaveBlueGreenStage();
        PrepareRoom();
        savedUntil = Time.unscaledTime + 2.5f;
        RefreshVisual();
    }

    private void LateUpdate() => RefreshVisual();

    public void SaveBlueGreenStage()
    {
        if (player.IsCurrentCheckpoint(checkpointId)) completedStageScene = gameObject.scene.path;
    }

    private void RefreshVisual()
    {
        Color color = player.IsCurrentCheckpoint(checkpointId) ? savedColor : unsavedColor;
        outline.startColor = outline.endColor = color;
        savedLabel.enabled = Time.unscaledTime < savedUntil;
    }
}
