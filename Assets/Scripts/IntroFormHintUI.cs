using System.Diagnostics;
using TMPro;
using UnityEngine;

// 仅观察玩家状态并更新文字，不修改操作和物理。
public class IntroFormHintUI : MonoBehaviour
{
    // 新选项放在末尾，保留原有场景设置。
    public enum IntroSequence { Box, Bubble, BouncyBall }

    [SerializeField]
    private IntroSequence sequence;

    [SerializeField]
    private PlayerPossessionController playerController;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text subtitleText;

    [SerializeField, Min(0f)]
    private float subtitleHoldSeconds = 4f;

    [SerializeField, Min(0.01f)]
    private float subtitleFadeSeconds = 0.75f;

    private bool hasState;
    private bool hasHosted;
    private BodyForm lastForm;
    private PossessableBody lastBody;
    private bool lastInWater;
    private float subtitleAge;

    private void OnEnable()
    {
        hasState = false;
        hasHosted = false;
        lastBody = null;
        ClearText();
    }

    private void Start()
    {
        if (playerController == null ||
            titleText == null ||
            subtitleText == null ||
            titleText == subtitleText)
        {
            UnityEngine.Debug.LogError(
                "IntroFormHintUI needs a controller and two separate TMP text objects.",
                this
            );

            enabled = false;
            return;
        }

        titleText.raycastTarget = false;
        subtitleText.raycastTarget = false;
    }

    private void LateUpdate()
    {
        if (playerController == null ||
            titleText == null ||
            subtitleText == null)
        {
            return;
        }

        PossessableBody body = playerController.CurrentBody;

        if (playerController.IsDead ||
            body == null ||
            !body.isActiveAndEnabled)
        {
            ClearText();
            hasState = false;
            return;
        }

        BodyForm form = body.Form;

        bool inWater =
            form == BodyForm.Bubble && body.IsInWater;

        bool switchedBox =
            sequence == IntroSequence.Box &&
            hasState &&
            lastForm == BodyForm.Box &&
            form == BodyForm.Box &&
            body != lastBody;

        // 换到另一个弹力球时，也重新显示副标题。
        bool switchedBall =
            hasState &&
            lastForm == BodyForm.BouncyBall &&
            form == BodyForm.BouncyBall &&
            body != lastBody;

        if (!hasState ||
            form != lastForm ||
            inWater != lastInWater ||
            switchedBox ||
            switchedBall)
        {
            bool leavingWater =
                hasState &&
                lastForm == BodyForm.Bubble &&
                lastInWater &&
                form == BodyForm.Bubble &&
                !inWater;

            ShowState(
                form,
                inWater,
                leavingWater,
                switchedBox
            );

            lastForm = form;
            lastInWater = inWater;
            hasState = true;

            if (form != BodyForm.Star)
            {
                hasHosted = true;
            }
        }

        lastBody = body;

        // 暂停游戏时，副标题的淡出计时也暂停。
        subtitleAge += Time.deltaTime;

        subtitleText.alpha = 1f - Mathf.Clamp01(
            (subtitleAge - subtitleHoldSeconds) /
            Mathf.Max(0.01f, subtitleFadeSeconds)
        );
    }

    private void ShowState(
        BodyForm form,
        bool inWater,
        bool leavingWater,
        bool switchedBox)
    {
        switch (form)
        {
            case BodyForm.Star:
                titleText.text = hasHosted
                    ? "You are a star again."
                    : "You are a star.";

                subtitleText.text = hasHosted
                    ? "Time to land."
                    : "Small feet. Big jumps.";
                break;

            case BodyForm.Box:
                titleText.text = "You are a box.";

                subtitleText.text = switchedBox
                    ? "New box. Same little star."
                    : sequence == IntroSequence.Box
                        ? "New body? Just bump into it."
                        : string.Empty;
                break;

            case BodyForm.Bubble:
                titleText.text = "You are a bubble.";

                subtitleText.text = inWater
                    ? "Underwater, every direction is yours."
                    : leavingWater
                        ? "Back in the air. Up you go!"
                        : "You float up. You steer sideways.";
                break;

            case BodyForm.BouncyBall:
                titleText.text = "You are a bouncy ball.";

                subtitleText.text =
                    "Keep bouncing. Use A and D to move.";
                break;

            default:
                ClearText();
                break;
        }

        titleText.alpha = 1f;
        subtitleText.alpha = 1f;
        subtitleAge = 0f;
    }

    private void OnDisable()
    {
        ClearText();
    }

    private void ClearText()
    {
        if (titleText != null)
        {
            titleText.text = string.Empty;
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
        }
    }
}