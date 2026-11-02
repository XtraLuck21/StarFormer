using UnityEngine;

/// <summary>Close the Bubble room entrance once; keep it shut after returning to Star.</summary>
public class IntroSpiderEntranceSeal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private GameObject entranceBarrier;

    private bool closed;

    private void Awake()
    {
        if (playerController == null || entranceBarrier == null)
        {
            Debug.LogError("Assign the IntroSpider player and entrance barrier.", this);
            enabled = false;
            return;
        }

        entranceBarrier.SetActive(false);
    }

    private void LateUpdate()
    {
        if (closed || playerController.IsDead || Time.timeScale <= 0f) return;

        PossessableBody body = playerController.CurrentBody;
        if (body == null || !body.isActiveAndEnabled || body.Form != BodyForm.Bubble) return;

        closed = true;
        entranceBarrier.SetActive(true);
    }
}
