using UnityEngine;
using UnityEngine.SceneManagement;

// Place beyond the opened flag gate, on the continuation of the floor.
[RequireComponent(typeof(BoxCollider2D))]
public class IntroSceneExit : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private IntroFlagGate requiredFlagGate;
    [SerializeField] private string nextSceneName = "IntroBubble";

    private bool isLoading;
    private bool reportedMissingScene;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (playerController == null || requiredFlagGate == null ||
            string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("IntroSceneExit requires a player controller, flag gate, and next scene name.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryExit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryExit(other);
    }

    private void TryExit(Collider2D other)
    {
        if (!isActiveAndEnabled || isLoading || Time.timeScale <= 0f ||
            playerController == null || playerController.IsDead ||
            requiredFlagGate == null || !requiredFlagGate.IsOpen)
        {
            return;
        }

        PossessableBody controlledBody = playerController.CurrentBody;
        if (controlledBody == null || other.attachedRigidbody == null ||
            other.attachedRigidbody != controlledBody.Body)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (!reportedMissingScene)
            {
                Debug.LogError($"Add and enable scene '{nextSceneName}' in the active Build Profile's Scene List before using this exit.", this);
                reportedMissingScene = true;
            }
            return;
        }

        isLoading = true;
        PlayerPossessionController.ResetCheckpointSession();
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}
