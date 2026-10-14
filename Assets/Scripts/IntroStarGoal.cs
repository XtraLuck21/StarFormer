using UnityEngine;
using UnityEngine.SceneManagement;

// Only the controlled, grounded Star can finish the bubble introduction.
[RequireComponent(typeof(BoxCollider2D))]
public class IntroStarGoal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private string nextSceneName = "Level01";

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

        PossessableBody star = playerController.CurrentBody;
        if (star == null || !star.isActiveAndEnabled || !star.IsControlled ||
            star.Form != BodyForm.Star || other.attachedRigidbody == null ||
            other.attachedRigidbody != star.Body || !star.IsGrounded())
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
        PlayerPossessionController.ResetCheckpointSession();
        SceneManager.LoadScene(nextSceneName);
    }
}
