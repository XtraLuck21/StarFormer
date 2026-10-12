using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelNavigationUI : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Mainmenu");
    }
}