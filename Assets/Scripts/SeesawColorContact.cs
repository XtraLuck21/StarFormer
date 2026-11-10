using UnityEngine;
using UnityEngine.Events;

/// <summary>A one-use colored contact on the board, paired with one room button.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SeesawColorContact : MonoBehaviour
{
    [SerializeField] private BoxCollider2D matchingButton;
    [SerializeField] private UnityEvent onPressed = new UnityEvent();

    public bool IsPressed { get; private set; }
    private BoxCollider2D sensor;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        sensor.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryPress(other);
    private void OnTriggerStay2D(Collider2D other) => TryPress(other);

    private void TryPress(Collider2D other)
    {
        if (IsPressed || !isActiveAndEnabled || other != matchingButton ||
            matchingButton == null || !matchingButton.isActiveAndEnabled) return;

        RestorePressed();
    }

    public void RestorePressed()
    {
        if (IsPressed) return;

        IsPressed = true;
        // Disable contact immediately, then run any configured button action once.
        sensor.enabled = false;
        try
        {
            onPressed.Invoke();
        }
        finally
        {
            matchingButton.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
