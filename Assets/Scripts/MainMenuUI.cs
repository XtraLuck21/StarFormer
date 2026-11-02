using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void OpenIntroLevels()
    {
        LoadLevel("IntroBox");
    }

    // Keep existing serialized button events working until they are reassigned.
    public void OpenTutorial()
    {
        OpenIntroLevels();
    }

    public void OpenLevel1()
    {
        LoadLevel("Level01");
    }

    public void OpenLevel2Intro()
    {
        LoadLevel("IntroSpider");
    }

    public void OpenLevel2()
    {
        LoadLevel("Level02");
    }

    private void LoadLevel(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Add and enable scene '{sceneName}' in the active Build Profile's Scene List.", this);
            return;
        }

        // Every menu selection starts a fresh checkpoint session.
        AnalyticsManager.Instance?.TrackAbandoned("menu_level_selection");
        PlayerPossessionController.ResetCheckpointSession();

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
