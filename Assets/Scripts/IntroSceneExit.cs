using UnityEngine;
using UnityEngine.SceneManagement;

// Place on a scene exit; forward routes can require an opened flag gate.
[RequireComponent(typeof(BoxCollider2D))]
public class IntroSceneExit : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private IntroFlagGate requiredFlagGate;
    [SerializeField] private bool requireFlagGate = true;
    [SerializeField] private string nextSceneName = "IntroBubble";

    [SerializeField] private bool returnToPreviousExit;
    [SerializeField] private PossessableBody returnBody;
    [SerializeField] private Vector2 returnPosition;
    [SerializeField] private float returnBoxSize = 0.5f;

    private static string pendingReturnScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetReturnState()
    {
        pendingReturnScene = null;
    }

    // Consumed once during player initialization, so a later restart is a fresh run.
    public static bool TryRestoreReturn(PlayerPossessionController controller,
        out PossessableBody body)
    {
        body = null;
        if (pendingReturnScene != controller.gameObject.scene.name)
            return false;

        pendingReturnScene = null;
        foreach (IntroSceneExit exit in FindObjectsByType<IntroSceneExit>(FindObjectsSortMode.None))
        {
            if (exit.playerController != controller || exit.returnBody == null ||
                !exit.returnBody.isActiveAndEnabled || !exit.returnBody.IsReady)
                continue;

            body = exit.returnBody;
            if (body.TryGetComponent(out BoxResizeAbility resize))
                resize.RestoreSceneEntrySize(exit.returnBoxSize);

            body.transform.position = new Vector3(exit.returnPosition.x,
                exit.returnPosition.y, body.transform.position.z);
            body.Body.position = exit.returnPosition;
            body.Body.linearVelocity = Vector2.zero;
            body.Body.angularVelocity = 0f;
            exit.requiredFlagGate?.Open();
            Physics2D.SyncTransforms();
            return true;
        }

        Debug.LogError("The return scene needs an exit with a configured return body.", controller);
        return false;
    }

    private bool isLoading;
    private bool reportedMissingScene;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (playerController == null || (requireFlagGate && requiredFlagGate == null) ||
            string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("IntroSceneExit requires a player controller, next scene name, and a flag gate when gate checking is enabled.", this);
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
            (requireFlagGate && (requiredFlagGate == null || !requiredFlagGate.IsOpen)))
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
        pendingReturnScene = returnToPreviousExit ? nextSceneName : null;
        PlayerPossessionController.ResetCheckpointSession();
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}
