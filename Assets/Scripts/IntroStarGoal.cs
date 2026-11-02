using UnityEngine;

// IntroBubble requires a grounded Star; other intro goals can accept any controlled form.
[RequireComponent(typeof(BoxCollider2D))]
public class IntroStarGoal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private string nextSceneName = "Level01";

    [SerializeField] private string nextLevelLabel = "Level 1";
    [SerializeField, Min(0.1f)] private float transitionSeconds = 1.5f;
    [SerializeField] private bool requireGroundedStar = true;

    private bool isLoading;
    private bool reportedMissingScene;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (playerController == null || string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("IntroStarGoal requires a player controller and next scene name.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryComplete(other);
    private void OnTriggerStay2D(Collider2D other) => TryComplete(other);

    private void TryComplete(Collider2D other)
    {
        if (!isActiveAndEnabled || isLoading || Time.timeScale <= 0f ||
            playerController == null || playerController.IsDead)
        {
            return;
        }

        PossessableBody playerBody = playerController.CurrentBody;
        if (playerBody == null || !playerBody.isActiveAndEnabled || !playerBody.IsControlled ||
            other.attachedRigidbody == null || other.attachedRigidbody != playerBody.Body ||
            (requireGroundedStar && (playerBody.Form != BodyForm.Star || !playerBody.IsGrounded())))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (!reportedMissingScene)
            {
                Debug.LogError($"Add and enable scene '{nextSceneName}' in the active Build Profile's Scene List before using this goal.", this);
                reportedMissingScene = true;
            }
            return;
        }

        isLoading = true;
        LevelTransitionUI.ShowAndLoad(nextSceneName, nextLevelLabel, playerController, transitionSeconds);
    }
}
