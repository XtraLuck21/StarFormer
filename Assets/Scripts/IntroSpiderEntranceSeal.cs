using UnityEngine;

/// <summary>Close the final room after the controlled body has fully crossed its entrance.</summary>
public class IntroSpiderEntranceSeal : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController playerController;
    [SerializeField] private GameObject entranceBarrier;
    [SerializeField] private Collider2D roomFloor;
    [SerializeField] private Collider2D roomRoof;

    private bool closed;
    private Bounds entranceBounds;

    private void Awake()
    {
        if (playerController == null || entranceBarrier == null || roomFloor == null || roomRoof == null ||
            !entranceBarrier.TryGetComponent(out Collider2D entranceCollider))
        {
            Debug.LogError("Assign the IntroSpider player, entrance barrier, room floor and roof.", this);
            enabled = false;
            return;
        }

        Physics2D.SyncTransforms();
        entranceBounds = entranceCollider.bounds;
        entranceBarrier.SetActive(false);
    }

    private void LateUpdate()
    {
        if (closed || playerController.IsDead || playerController.IsRestarting || Time.timeScale <= 0f) return;

        PossessableBody body = playerController.CurrentBody;
        if (body == null || !body.isActiveAndEnabled || !body.IsControlled || body.BodyCollider == null) return;
        Bounds bodyBounds = body.BodyCollider.bounds;
        // Wait until the whole body is past the door, below the roof and above the floor.
        if (bodyBounds.max.x > entranceBounds.min.x - .02f ||
            bodyBounds.min.x < roomFloor.bounds.min.x ||
            bodyBounds.min.y < roomFloor.bounds.max.y - .1f ||
            bodyBounds.max.y > roomRoof.bounds.min.y + .1f) return;

        closed = true;
        entranceBarrier.SetActive(true);
    }
}
