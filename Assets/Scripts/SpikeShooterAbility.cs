using UnityEngine;

[RequireComponent(typeof(PossessableBody))]
public class SpikeShooterAbility : MonoBehaviour
{
    // The projectile prefab to spawn for each shot.
    [SerializeField] private SpikeProjectile spikePrefab;

    // The visible spikes on the wall, fired in list order.
    // Each spike flies in the direction its tip points (its local up).
    [SerializeField] private Transform[] spikeSlots = new Transform[4];

    [SerializeField] private float spikeSpeed = 12f;

    private PossessableBody possessable;
    private int shotsFired;

    public int RemainingSpikes => spikeSlots.Length - shotsFired;

    private void Awake()
    {
        possessable = GetComponent<PossessableBody>();

        bool slotsValid = spikeSlots != null && spikeSlots.Length > 0;

        if (slotsValid)
        {
            foreach (Transform slot in spikeSlots)
            {
                if (slot == null || slot == transform)
                {
                    slotsValid = false;
                    break;
                }
            }
        }

        if (possessable.Form != BodyForm.SpikeWall ||
            spikePrefab == null ||
            !slotsValid)
        {
            Debug.LogError(
                "SpikeShooterAbility requires a SpikeWall body, " +
                "a spike prefab, and separate spike slot objects.",
                this
            );

            enabled = false;
        }
    }

    // Called by PlayerPossessionController when Space is pressed.
    public bool TryFire()
    {
        if (!isActiveAndEnabled ||
            !possessable.IsControlled ||
            RemainingSpikes <= 0)
        {
            return false;
        }

        Transform slot = spikeSlots[shotsFired];
        shotsFired++;

        SpikeProjectile spike = Instantiate(
            spikePrefab,
            slot.position,
            slot.rotation
        );

        spike.Launch((Vector2)slot.up * spikeSpeed, possessable.Body);

        // The fired spike leaves the wall.
        slot.gameObject.SetActive(false);

        Debug.Log($"Spike fired. {RemainingSpikes} left.", this);
        return true;
    }

    private void OnValidate()
    {
        spikeSpeed = Mathf.Max(0.1f, spikeSpeed);
    }
}
