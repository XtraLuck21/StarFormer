using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ControllableBody body = other.GetComponentInParent<ControllableBody>();

        if (body != null && PlayerControlManager.Instance != null)
        {
            PlayerControlManager.Instance.CompleteLevel(body);
        }
    }
}
