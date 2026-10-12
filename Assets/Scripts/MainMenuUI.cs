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

    public void OpenLevel2()
    {
        LoadLevel("Level02");
    }

    private void LoadLevel(string sceneName)
    {
        // 从主菜单进关卡一律是全新一局，清掉上一局残留的检查点。
        PlayerPossessionController.ResetCheckpointSession();

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}