using UnityEngine;

/// <summary>
/// Shows the assigned text the first time a possessable body enters,
/// then leaves it on permanently.
/// </summary>
public class ShowTextOnceTrigger : MonoBehaviour
{
    [SerializeField] private GameObject textObject;

    private bool hasShown;

    private void Awake()
    {
        if (textObject != null)
        {
            textObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasShown || textObject == null)
        {
            return;
        }

        if (other.GetComponentInParent<PossessableBody>() == null)
        {
            return;
        }

        hasShown = true;
        textObject.SetActive(true);
    }
}
