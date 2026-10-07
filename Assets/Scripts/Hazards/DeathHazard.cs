using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DeathHazard : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ControllableBody body = other.GetComponentInParent<ControllableBody>();

        if (body != null)
        {
            body.KillIfControlled();
        }
    }
}
