using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class IntroBouncyBallGoal : MonoBehaviour
{
    [SerializeField]
    private PlayerPossessionController playerController;

    [SerializeField]
    private string nextSceneName = "Level01";

    [SerializeField] private Sprite graduateStar;

    private bool isLoading;
    private bool reportedMissingScene;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (playerController == null ||
            string.IsNullOrWhiteSpace(nextSceneName))
        {
            UnityEngine.Debug.LogError(
                "IntroBouncyBallGoal requires a player controller " +
                "and next scene name.",
                this
            );

            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryComplete(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryComplete(other);
    }

    private void TryComplete(Collider2D other)
    {
        if (!isActiveAndEnabled ||
            isLoading ||
            Time.timeScale <= 0f ||
            playerController == null ||
            playerController.IsDead)
        {
            return;
        }

        PossessableBody body = playerController.CurrentBody;

        if (body == null ||
            !body.isActiveAndEnabled ||
            !body.IsControlled)
        {
            return;
        }

        if (body.Form != BodyForm.Star &&
            body.Form != BodyForm.BouncyBall)
        {
            return;
        }

        if (other.attachedRigidbody == null ||
            other.attachedRigidbody != body.Body)
        {
            return;
        }

        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(
            nextSceneName))
        {
            if (!reportedMissingScene)
            {
                UnityEngine.Debug.LogError(
                    $"Add and enable scene '{nextSceneName}' " +
                    "in the active Build Profile's Scene List.",
                    this
                );

                reportedMissingScene = true;
            }

            return;
        }

        isLoading = true;
        AnalyticsManager.Instance?.TrackLevelComplete(body);

        LevelTransitionUI.ShowKindergartenGraduation(nextSceneName, playerController, graduateStar);
    }
}
