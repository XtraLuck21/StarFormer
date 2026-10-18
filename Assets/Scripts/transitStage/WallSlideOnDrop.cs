using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WallSlideOnDrop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PossessableBody gravityBall;

    [Header("Trigger")]
    [SerializeField] private float triggerY = -20f;

    [Header("Wall Position")]
    [SerializeField]
    private Rigidbody2D body;
    private SpriteRenderer[] wallRenderers;
    private Collider2D[] wallColliders;
    private bool shown;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Place the wall directly at its final position without a movement animation.
        body.position = (Vector2)transform.position;

        wallRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        wallColliders =
            GetComponentsInChildren<Collider2D>(true);

        SetWallVisible(false);
    }

    private void Update()
    {
        if (shown || gravityBall == null)
        {
            return;
        }

        if (gravityBall.transform.position.y <= triggerY)
        {
            shown = true;
            SetWallVisible(true);
        }
    }

    public void ShowWall()
    {
        shown = true;
        SetWallVisible(true);
    }
    private void SetWallVisible(bool visible)
    {
        foreach (SpriteRenderer wallRenderer in wallRenderers)
        {
            wallRenderer.enabled = visible;
        }

        foreach (Collider2D wallCollider in wallColliders)
        {
            wallCollider.enabled = visible;
        }

        body.simulated = visible;
    }
}