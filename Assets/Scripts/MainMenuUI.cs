using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void OpenTutorial()
    {
        LoadLevel("Tutorial");
    }

    public void OpenLevel1()
    {
        LoadLevel("Level01");
    }

    private void LoadLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}