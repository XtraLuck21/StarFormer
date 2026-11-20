using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WaterZone : MonoBehaviour
{
    private static readonly HashSet<WaterZone> activeZones =
        new HashSet<WaterZone>();

    private BoxCollider2D zoneCollider;

    private void OnEnable()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;

        activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    public static bool Contains(Vector2 worldPosition)
    {
        foreach (WaterZone zone in activeZones)
        {
            if (zone == null ||
                !zone.isActiveAndEnabled ||
                zone.zoneCollider == null ||
                !zone.zoneCollider.enabled)
            {
                continue;
            }

            if (zone.zoneCollider.OverlapPoint(worldPosition))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetSurfaceY(Bounds bodyBounds, out float surfaceY)
    {
        surfaceY = float.NegativeInfinity;
        bool found = false;

        foreach (WaterZone zone in activeZones)
        {
            if (zone == null ||
                !zone.isActiveAndEnabled ||
                zone.zoneCollider == null ||
                !zone.zoneCollider.enabled)
            {
                continue;
            }

            Bounds waterBounds = zone.zoneCollider.bounds;
            // A ball still supported by a bank must keep bouncing, even when
            // its side overlaps water. Use the bottom-center support point so
            // the behavior changes at the visible shoreline in both directions.
            bool overWater =
                bodyBounds.center.x > waterBounds.min.x &&
                bodyBounds.center.x < waterBounds.max.x;
            bool reachesWater =
                bodyBounds.min.y <= waterBounds.max.y + 0.05f &&
                bodyBounds.max.y >= waterBounds.min.y;

            if (!overWater || !reachesWater)
            {
                continue;
            }

            surfaceY = Mathf.Max(surfaceY, waterBounds.max.y);
            found = true;
        }

        return found;
    }
}
