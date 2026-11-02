using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class IntroBouncyBallGoal : MonoBehaviour
{
    [SerializeField]
    private PlayerPossessionController playerController;

    [SerializeField]
    private string nextSceneName = "Level01";

    [SerializeField]
    private string nextLevelLabel = "Level 1";

    [SerializeField, Min(0.1f)]
    private float transitionSeconds = 1.5f;

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

        // 只有玩家控制的星星或弹力球可以通关。
        if (body.Form != BodyForm.Star &&
            body.Form != BodyForm.BouncyBall)
        {
            return;
        }

        // 确认进入终点的是当前玩家的碰撞框。
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

        // 防止重复触发。
        isLoading = true;

        // 复用项目已有的过关提示和场景跳转。
        LevelTransitionUI.ShowAndLoad(
            nextSceneName,
            nextLevelLabel,
            playerController,
            transitionSeconds
        );
    }
}