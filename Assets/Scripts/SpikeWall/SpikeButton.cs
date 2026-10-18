using UnityEngine;

public enum ButtonMode
{
    // One hit opens the gate permanently.
    OneShot,

    // Each hit toggles the state: open, closed, open.
    Toggle,

    // Stay open while a spike is attached; close when the spike disappears during reload.
    // This mode requires a non-trigger Collider2D on the button.
    HoldWhileStuck
}

[RequireComponent(typeof(Collider2D))]
public class SpikeButton : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private ButtonMode mode = ButtonMode.OneShot;

    // Doors controlled by this button; multiple doors can be assigned.
    [SerializeField] private TriggerDoor[] doors = new TriggerDoor[1];

    [Header("Visuals")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.4f, 1f, 0.4f);

    private SpriteRenderer spriteRenderer;
    private SpikeProjectile heldSpike;

    private bool isPressed;

    public bool IsPressed => isPressed;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ApplyVisual();
    }

    // Called by SpikeProjectile on impact.
    public void OnSpikeHit(SpikeProjectile spike)
    {
        switch (mode)
        {
            case ButtonMode.OneShot:
                if (isPressed)
                {
                    return;
                }

                SetPressed(true);
                break;

            case ButtonMode.Toggle:
                SetPressed(!isPressed);
                break;

            case ButtonMode.HoldWhileStuck:
                heldSpike = spike;
                SetPressed(true);
                break;
        }

        Debug.Log($"{name} pressed: {isPressed}", this);
    }

    private void Update()
    {
        if (mode != ButtonMode.HoldWhileStuck || !isPressed)
        {
            return;
        }

        // Unity treats the destroyed spike reference as null.
        if (heldSpike == null)
        {
            SetPressed(false);
        }
    }

    private void SetPressed(bool pressed)
    {
        isPressed = pressed;

        foreach (TriggerDoor door in doors)
        {
            if (door != null)
            {
                door.SetOpen(pressed);
            }
        }

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isPressed ? pressedColor : idleColor;
        }
    }
}
