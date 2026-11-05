using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [Tooltip("Optional gate that must open before this goal can be reached.")]
    [SerializeField] private GameObject requiredGate;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string nextSceneName = "Level02";

    [SerializeField] private string nextLevelLabel = "Level 2";
    [SerializeField, Min(0.1f)] private float transitionSeconds = 1.5f;

    private bool isCompleted;
    private bool reportedMissingScene;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (playerController == null ||
            string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("Assign the player controller and next scene name.", this);
            enabled = false;
            return;
        }

        if (statusText != null) statusText.text = "";
    }

    private void OnTriggerEnter2D(Collider2D other) => CheckGoal(other);
    private void OnTriggerStay2D(Collider2D other) => CheckGoal(other);

    private void CheckGoal(Collider2D other)
    {
        if (!isActiveAndEnabled || isCompleted ||
            playerController.IsDead || Time.timeScale <= 0f || (requiredGate != null && requiredGate.activeSelf))
        {
            return;
        }

        Rigidbody2D body = other.attachedRigidbody;
        if (body == null ||
            !body.TryGetComponent(out PossessableBody playerBody) ||
            !playerBody.IsControlled || playerController.CurrentBody != playerBody)
        {
            return;
        }

        // Any controlled form can finish once the optional gate opens.
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (!reportedMissingScene)
            {
                Debug.LogError($"Add and enable scene '{nextSceneName}' in the active Build Profile's Scene List.", this);
                reportedMissingScene = true;
            }
            return;
        }

        isCompleted = true;
        ShapeClosetProgress.RecordCompletion(gameObject.scene.name);
        AnalyticsManager.Instance?.TrackLevelComplete(playerBody);
        LevelTransitionUI.ShowAndLoad(nextSceneName, nextLevelLabel, playerController, transitionSeconds);
    }
}
