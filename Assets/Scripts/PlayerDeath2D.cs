using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath2D : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    private bool isDead;

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerPossessionController>();
        }

        if (playerController != null && playerController.CurrentBody != null)
        {
            playerController.KillControlledBody(
                playerController.CurrentBody,
                AnalyticsDeathCause.Other,
                0f
            );
            return;
        }

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex < 0)
        {
            UnityEngine.Debug.LogError(
                "Please first add the current scene to the Scene List in the Build Profiles.");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }
}
