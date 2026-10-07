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
}