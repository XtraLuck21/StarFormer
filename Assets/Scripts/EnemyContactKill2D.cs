using UnityEngine;

public class EnemyContactKill2D : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerDeath2D player =
            collision.collider.GetComponentInParent<PlayerDeath2D>();

        if (player != null)
        {
            player.Die();
        }
    }
}