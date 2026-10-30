using TMPro;
using UnityEngine;

public class TutorialIntroUI : MonoBehaviour
{
    [SerializeField] private PossessableBody tutorialBox;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text instructionsText;
    [SerializeField] private PlayerPossessionController playerController;

    private bool deathShown;

    public bool CanComplete =>
        !isCompleted &&
        playerController != null &&
        !playerController.IsDead;

    private bool possessionIntroduced;
    private bool isCompleted;

    private void Start()
    {
        if (tutorialBox == null ||
            titleText == null ||
            instructionsText == null ||
            playerController == null)
        {
            Debug.LogError(
                "Assign the tutorial box, title, instructions, and player controller.",
                this
            );

            enabled = false;
            return;
        }

        titleText.text = "You are a star.";
        instructionsText.text =
            "Move: A / D\nJump: Space\nRestart menu: R";
    }

    private void Update()
    {
        if (playerController.IsDead)
        {
            if (!deathShown)
            {
                deathShown = true;
                titleText.text = "You died.";
                instructionsText.text =
                    "Retrying...\nPress R for restart options.";
            }

            return;
        }

        if (isCompleted ||
            possessionIntroduced ||
            !tutorialBox.IsControlled)
        {
            return;
        }

        possessionIntroduced = true;

        titleText.text = "Now you are a StarFormer.";
        instructionsText.text =
            "Touch special objects to become them.\n\n" +
            "Move: A / D\n" +
            "Grow: Hold W\n" +
            "Shrink: Hold S\n" +
            "Restart menu: R";
    }

    public void ShowCompletion()
    {
        isCompleted = true;

        titleText.text = "Now get ready to transform!";
        instructionsText.text =
            "More forms. More possibilities.\n\n" +
            "Press R for restart options.";
    }
}
