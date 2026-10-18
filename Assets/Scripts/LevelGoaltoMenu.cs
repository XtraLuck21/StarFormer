using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Reaching the goal completes the level; optionally wait for a menu button.
// Attach to the goal flag with a Collider2D marked Is Trigger.
[RequireComponent(typeof(Collider2D))]
public class LevelGoalToMenu : MonoBehaviour
{
    [Header("References")]
    // Automatically find the reference in the scene when unassigned.
    [SerializeField] private PlayerPossessionController player;

    [Header("Required Gate")]
    // Gate requirement; leave empty to allow completion without a gate check.
    [SerializeField] private GameObject requiredGate;

    // Enabled: the gate must be destroyed or inactive to allow completion.
    // Disabled: the gate must still be active (for special cases).
    [SerializeField] private bool gateMustBeGone = true;

    [Header("Status Text")]
    [SerializeField] private TMP_Text statusText;

    [SerializeField, TextArea]
    private string clearedMessage = "Level complete!";

    [SerializeField, TextArea]
    private string blockedMessage = "A gate is still closed.";

    // Seconds to display the blocked message when the requirement is not met.
    [SerializeField, Min(0f)] private float blockedMessageTime = 2f;

    [Header("Return To Menu")]
    // Main-menu scene name; must match the enabled build scene list.
    [SerializeField] private string menuSceneName = "Mainmenu";

    [Tooltip("Show Level Complete and wait for the player to choose Return to Main Menu.")]
    [SerializeField] private bool waitForMenuButton;

    // Delay between displaying completion and loading the next scene.
    [SerializeField, Min(0f)] private float returnDelay = 1.5f;

    // Invoked before switching scenes; can trigger audio, saving, or other actions.
    [SerializeField] private UnityEvent onLevelCleared = new UnityEvent();

    private bool cleared;
    private Coroutine blockedRoutine;
    private bool reportedMissingMenu;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void CheckPlayer(Collider2D other)
    {
        if (cleared || player == null || player.IsDead || Time.timeScale <= 0f)
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null ||
            !otherBody.TryGetComponent(out PossessableBody body) ||
            !body.IsControlled || player.CurrentBody != body)
        {
            return;
        }

        if (!IsGateSatisfied())
        {
            ShowBlockedMessage();
            return;
        }

        if (string.IsNullOrWhiteSpace(menuSceneName) ||
            !Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            if (!reportedMissingMenu)
            {
                Debug.LogError($"Add and enable scene '{menuSceneName}' in the Build Profile.", this);
                reportedMissingMenu = true;
            }
            return;
        }

        Clear();
    }

    // Check the gate requirement; an unassigned gate always passes.
    private bool IsGateSatisfied()
    {
        if (requiredGate == null)
        {
            return true;
        }

        // Unity treats destroyed object references as null.
        bool gateGone = !requiredGate.activeInHierarchy;

        return gateMustBeGone ? gateGone : !gateGone;
    }

    private void ShowBlockedMessage()
    {
        if (statusText == null || blockedRoutine != null)
        {
            return;
        }

        blockedRoutine = StartCoroutine(BlockedRoutine());
    }

    private IEnumerator BlockedRoutine()
    {
        statusText.text = blockedMessage;

        yield return new WaitForSeconds(blockedMessageTime);

        if (!cleared)
        {
            statusText.text = string.Empty;
        }

        blockedRoutine = null;
    }

    private void Clear()
    {
        cleared = true;
        AnalyticsManager.Instance?.TrackLevelComplete(
            player != null ? player.CurrentBody : null
        );

        if (blockedRoutine != null)
        {
            StopCoroutine(blockedRoutine);
            blockedRoutine = null;
        }

        if (statusText != null)
        {
            statusText.text = clearedMessage;
        }

        onLevelCleared.Invoke();

        if (waitForMenuButton)
            LevelTransitionUI.ShowFinalResult(menuSceneName, player);
        else
            StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        // Use realtime so the delay completes even if Time.timeScale is zero.
        yield return new WaitForSecondsRealtime(returnDelay);

        // Restore time before loading so the main menu is not paused.
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("The main-menu scene name is not assigned.", this);
            yield break;
        }

        PlayerPossessionController.ResetCheckpointSession();
        SceneManager.LoadScene(menuSceneName);
    }
}
