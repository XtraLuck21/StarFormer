using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelNavigationUI : MonoBehaviour
{
    public void RestartLevel()
    {
        ReloadCurrentScene(clearCheckpoint: true);
    }

    public void RetryCheckpoint()
    {
        ReloadCurrentScene(clearCheckpoint: false);
    }

    private void ReloadCurrentScene(bool clearCheckpoint)
    {
        int sceneIndex = gameObject.scene.buildIndex;
        if (sceneIndex < 0)
        {
            Debug.LogError("Add the current scene to the enabled build scene list before restarting.", this);
            return;
        }

        // Both buttons are manual restarts, not deaths.
        AnalyticsManager.Instance?.TrackManualRestart();
        if (clearCheckpoint)
        {
            // Explicit full restart; R opens a menu with checkpoint and full-restart choices.
            PlayerPossessionController.ResetCheckpointSession();
        }

        // Keeping checkpoint state lets the existing scene startup restore it.
        // Without an active checkpoint, startup uses the normal level entrance.
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    public void ReturnToMainMenu()
    {
        AnalyticsManager.Instance?.TrackAbandoned("return_to_menu");
        PlayerPossessionController.ResetCheckpointSession();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Mainmenu");
    }
}
