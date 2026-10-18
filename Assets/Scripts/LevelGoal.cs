using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
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

        if (playerController == null || requiredGate == null ||
            string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("Assign the player controller, gate, and next scene name.", this);
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
            playerController.IsDead || Time.timeScale <= 0f || requiredGate.activeSelf)
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

        // Any controlled form can finish Level 1 after its final gate opens.
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
        AnalyticsManager.Instance?.TrackLevelComplete(playerBody);
        LevelTransitionUI.ShowAndLoad(nextSceneName, nextLevelLabel, playerController, transitionSeconds);
    }
}
