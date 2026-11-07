using UnityEngine;

/// <summary>Scene opt-in: consume the Bouncy Ball after a successful transfer to Spider.</summary>
[RequireComponent(typeof(PlayerPossessionController))]
public class ConsumeBallOnSpiderPossession : MonoBehaviour
{
    private PlayerPossessionController player;

    private void OnEnable()
    {
        player = GetComponent<PlayerPossessionController>();
        player.PossessionTransferred += OnPossessionTransferred;
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.PossessionTransferred -= OnPossessionTransferred;
        }
    }

    private void OnPossessionTransferred(PossessableBody previous, PossessableBody current)
    {
        if (previous == null || current == null ||
            previous.Form != BodyForm.BouncyBall || current.Form != BodyForm.Spider)
        {
            return;
        }

        previous.gameObject.SetActive(false);
        Destroy(previous.gameObject);
    }
}
