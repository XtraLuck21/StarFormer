using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelNavigationUI : MonoBehaviour
{
    public void RestartLevel()
    {
        int sceneIndex = gameObject.scene.buildIndex;
        if (sceneIndex < 0)
        {
            Debug.LogError("Add the current scene to the enabled build scene list before restarting.", this);
            return;
        }

        // Match R: restart the whole level rather than resume at a checkpoint.
        PlayerPossessionController.ResetCheckpointSession();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Mainmenu");
    }
}