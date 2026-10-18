using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PossessableBody))]
public class SpikeShooterAbility : MonoBehaviour
{
    // The projectile prefab to spawn for each shot.
    [SerializeField] private SpikeProjectile spikePrefab;

    // The visible spikes on the wall, fired in list order.
    // They should be direct children of this wall.
    [SerializeField] private Transform[] spikeSlots = new Transform[4];

    [SerializeField] private float spikeSpeed = 12f;

    // Delay after emptying the magazine to prevent rapid Space presses from refilling it instantly.
    [SerializeField, Min(0f)] private float reloadDelay = 1.5f;

    // Enabled: reload automatically when the cooldown ends without another Space press.
    [SerializeField] private bool autoReload = false;

    // Enabled: face right initially; disabled: face left.
    // Preview the facing direction in the Scene view outside Play Mode.
    [SerializeField] private bool startFacingRight = true;

    private PossessableBody possessable;
    private int shotsFired;

    private bool facingRight;
    private float[] slotOffsetX;
    private float emptyTime;

    // Every spike this wall has fired since the last reload.
    private readonly List<SpikeProjectile> firedSpikes =
        new List<SpikeProjectile>(4);

    public int RemainingSpikes => spikeSlots.Length - shotsFired;
    public bool FacingRight => facingRight;

    // Seconds until reload is available; zero means ready. Exposed for UI progress.
    public float ReloadCooldownRemaining => RemainingSpikes > 0
        ? 0f
        : Mathf.Max(0f, emptyTime + reloadDelay - Time.time);

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
            return;
        }

        // Remember how far each spike sits from the wall's center.
        slotOffsetX = new float[spikeSlots.Length];

        for (int i = 0; i < spikeSlots.Length; i++)
        {
            slotOffsetX[i] = Mathf.Abs(spikeSlots[i].localPosition.x);
        }

        facingRight = startFacingRight;
        ApplyFacing();
    }

    // Called by PlayerPossessionController when A or D is pressed.
    public void SetFacing(bool right)
    {
        if (!isActiveAndEnabled || right == facingRight)
        {
            return;
        }

        facingRight = right;
        ApplyFacing();
    }

    // Move the spikes to the facing side and point them outward.
    // The built-in Triangle sprite points up, so -90 points right.
    private void ApplyFacing()
    {
        float side = facingRight ? 1f : -1f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, facingRight ? -90f : 90f);

        for (int i = 0; i < spikeSlots.Length; i++)
        {
            Vector3 position = spikeSlots[i].localPosition;
            position.x = slotOffsetX[i] * side;

            spikeSlots[i].localPosition = position;
            spikeSlots[i].localRotation = rotation;
        }
    }

    private void Update()
    {
        if (!autoReload ||
            RemainingSpikes > 0 ||
            ReloadCooldownRemaining > 0f)
        {
            return;
        }

        Reload();
    }

    // Called by PlayerPossessionController when Space is pressed.
    // Fires one spike, or reloads when the wall is empty.
    public bool TryFire()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "SpikeShooterAbility is disabled. Check the red error at Play start.",
                this
            );
            return false;
        }

        if (!possessable.IsControlled)
        {
            Debug.LogWarning("This wall is not possessed.", this);
            return false;
        }

        if (RemainingSpikes <= 0)
        {
            if (ReloadCooldownRemaining > 0f)
            {
                Debug.Log(
                    $"Reloading in {ReloadCooldownRemaining:F1}s.",
                    this
                );

                return false;
            }

            Reload();
            return false;
        }

        Transform slot = spikeSlots[shotsFired];
        shotsFired++;

        // Start the cooldown when the last shot is fired.
        if (RemainingSpikes <= 0)
        {
            emptyTime = Time.time;
        }

        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        SpikeProjectile spike = Instantiate(
            spikePrefab,
            slot.position,
            slot.rotation
        );

        spike.Launch(direction * spikeSpeed, possessable.Body);
        firedSpikes.Add(spike);

        // The fired spike leaves the wall.
        slot.gameObject.SetActive(false);

        Debug.Log(
            $"Spike fired {(facingRight ? "right" : "left")}. " +
            $"{RemainingSpikes} left.",
            this
        );

        return true;
    }

    // Remove every fired spike from the world and restore them on the wall.
    private void Reload()
    {
        foreach (SpikeProjectile spike in firedSpikes)
        {
            // Spikes that already vanished are null here.
            if (spike != null)
            {
                Destroy(spike.gameObject);
            }
        }

        firedSpikes.Clear();

        foreach (Transform slot in spikeSlots)
        {
            slot.gameObject.SetActive(true);
        }

        shotsFired = 0;
        ApplyFacing();

        Debug.Log("Spikes reloaded.", this);
    }

    private void OnValidate()
    {
        spikeSpeed = Mathf.Max(0.1f, spikeSpeed);

        // SetFacing handles runtime direction; this only updates the Editor preview.
        if (Application.isPlaying || spikeSlots == null)
        {
            return;
        }

        float side = startFacingRight ? 1f : -1f;

        Quaternion rotation = Quaternion.Euler(
            0f, 0f, startFacingRight ? -90f : 90f
        );

        foreach (Transform slot in spikeSlots)
        {
            if (slot == null || slot == transform)
            {
                continue;
            }

            Vector3 position = slot.localPosition;
            position.x = Mathf.Abs(position.x) * side;

            slot.localPosition = position;
            slot.localRotation = rotation;
        }
    }
}