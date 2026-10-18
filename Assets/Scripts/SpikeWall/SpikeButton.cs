using UnityEngine;

public enum ButtonMode
{
    // 打中一次就永久开启，不能再关。
    OneShot,

    // 每打中一次翻转一次状态：开 → 关 → 开。
    Toggle,

    // 刺钉在上面时保持开启，刺消失（换弹）后自动关闭。
    // 这个模式要求按钮的 Collider 2D 不勾 Is Trigger。
    HoldWhileStuck
}

[RequireComponent(typeof(Collider2D))]
public class SpikeButton : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private ButtonMode mode = ButtonMode.OneShot;

    // 这个按钮控制的门，可以填多扇。
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

    // 由 SpikeProjectile 在命中时调用。
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

        // 刺被销毁后，Unity 会让这个引用等于 null。
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
