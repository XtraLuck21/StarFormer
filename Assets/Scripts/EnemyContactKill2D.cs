using UnityEngine;

public class EnemyContactKill2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeath2D player = other.GetComponentInParent<PlayerDeath2D>();

        if (player != null)
        {
            player.Die();
        }
    }
}