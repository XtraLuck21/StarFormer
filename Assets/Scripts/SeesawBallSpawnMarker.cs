using UnityEngine;

/// <summary>A persistent dashed outline identifies the ordinary ball supply point.</summary>
public sealed class SeesawBallSpawnMarker : MonoBehaviour
{
    private SeesawRoomSupplies supply;
    private GameObject marker;

    public void Initialize(SeesawRoomSupplies owner, SpriteRenderer ballVisual)
    {
        supply = owner;
        marker = new GameObject("Dashed Ball Spawner Marker");
        marker.transform.SetParent(transform, false);
        // The authored spawn point inherits the board's nonuniform scale.
        // Keep the marker circular in world space.
        marker.transform.SetParent(owner.transform, true);
        for (int i = 0; i < 16; i++)
        {
            var dash = new GameObject("Spawner Outline Dash");
            dash.transform.SetParent(marker.transform, false);
            var line = dash.AddComponent<LineRenderer>();
            line.sharedMaterial = ballVisual.sharedMaterial;
            line.useWorldSpace = false;
            line.positionCount = 4;
            line.widthMultiplier = .035f;
            line.startColor = line.endColor = new Color(1f, .65f, .55f, .9f);
            line.sortingLayerID = ballVisual.sortingLayerID;
            line.sortingOrder = 8;
            for (int j = 0; j < 4; j++)
            {
                float angle = (i + j / 3f * .58f) * Mathf.PI * 2f / 16;
                line.SetPosition(j, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * .62f);
            }
        }
        FollowSpawnPoint();
    }

    private void LateUpdate()
    {
        if (marker == null || supply == null) return;
        marker.SetActive(supply.IsRoomActive && !supply.FanSupplyStopped && !supply.IsPuzzleComplete);
        FollowSpawnPoint();
    }

    private void FollowSpawnPoint()
    {
        marker.transform.position = transform.position;
        marker.transform.rotation = Quaternion.identity;
        Vector3 scale = supply.transform.lossyScale;
        marker.transform.localScale = new Vector3(1f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f);
    }

    private void OnDestroy()
    {
        if (marker != null) Destroy(marker);
    }

    private void OnDisable()
    {
        if (marker != null) marker.SetActive(false);
    }
}
