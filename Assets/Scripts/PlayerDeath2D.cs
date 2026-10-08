using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath2D : MonoBehaviour
{
    private bool isDead;

    public void Die()
    {
        if (isDead) return;
        isDead = true;

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