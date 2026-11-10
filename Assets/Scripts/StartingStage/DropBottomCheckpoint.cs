using TMPro;
using UnityEngine;

/// <summary>Saves a safe landing at the foot of the drop and restores the bottom passage directly.</summary>
[DefaultExecutionOrder(250)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class DropBottomCheckpoint : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private string checkpointId = "DROP_BOTTOM";
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Collider2D landingFloor;
    [SerializeField] private DarkStarPassage passage;
    [SerializeField] private SpikeWallEnemyChase chaseWall;
    [SerializeField] private level2Camera levelCamera;
    [SerializeField] private LineRenderer outline;
    [SerializeField] private TMP_Text savedLabel;
    [SerializeField] private Color unsavedColor = new Color32(47, 155, 236, 255);
    [SerializeField] private Color savedColor = new Color32(255, 211, 77, 255);
    private float savedUntil;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        savedLabel.enabled = false;
    }

    private void Start()
    {
        if (player.IsCurrentCheckpoint(checkpointId))
        {
            // EndChase may request a focus shot; bottom framing must be applied afterwards.
            if (chaseWall != null) chaseWall.EndChase();
            passage.RestoreAtBottom(player.CurrentBody);
            levelCamera.RestoreDarkPassage();
        }
        RefreshVisual();
    }

    private void OnTriggerEnter2D(Collider2D other) => TrySave(other);
    private void OnTriggerStay2D(Collider2D other) => TrySave(other);

    private void TrySave(Collider2D other)
    {
        if (player.IsDead || player.IsRestarting || Time.timeScale <= 0f ||
            player.IsCurrentCheckpoint(checkpointId) || other.attachedRigidbody == null ||
            !other.attachedRigidbody.TryGetComponent(out PossessableBody body) ||
            body != player.CurrentBody || !body.IsControlled || body.Form != BodyForm.Star ||
            !body.IsGrounded() || body.BodyCollider == null ||
            Mathf.Abs(body.BodyCollider.bounds.min.y - landingFloor.bounds.max.y) > .2f) return;
        player.SetCheckpoint(checkpointId, spawnPosition, showFeedback: false);
        savedUntil = Time.unscaledTime + 2.5f;
        RefreshVisual();
    }

    private void LateUpdate() => RefreshVisual();

    private void RefreshVisual()
    {
        Color color = player.IsCurrentCheckpoint(checkpointId) ? savedColor : unsavedColor;
        outline.startColor = outline.endColor = color;
        savedLabel.enabled = Time.unscaledTime < savedUntil;
    }
}
