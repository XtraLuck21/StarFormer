using TMPro;
using UnityEngine;

public class LevelDeathUI : MonoBehaviour
{
    [SerializeField]
    private PlayerPossessionController playerController;

    [SerializeField]
    private TMP_Text statusText;

    private bool deathShown;

    private void Start()
    {
        if (playerController == null || statusText == null)
        {
            Debug.LogError(
                "Assign the player controller and status text.",
                this
            );

            enabled = false;
            return;
        }

        statusText.text = "";
    }

    private void Update()
    {
        if (deathShown || !playerController.IsDead)
        {
            return;
        }

        deathShown = true;

        statusText.text = "You died.\nRestarting...";
    }
}
