using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class PlayerControlManager : MonoBehaviour
{
    [SerializeField] private ControllableBody initialBody;
    [SerializeField] private float minimumHeight = -8f;
    [SerializeField] private float maximumHeight = 14f;

    private ControllableBody currentBody;
    private ControllableBody pendingTarget;
    private float horizontalInput;
    private bool actionRequested;
    private bool restartRequested;

    public static PlayerControlManager Instance { get; private set; }
    public ControllableBody CurrentBody => currentBody;
    public bool IsComplete { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (initialBody == null)
        {
            Debug.LogError("PlayerControlManager needs an initial body.", this);
            enabled = false;
            return;
        }

        currentBody = initialBody;
        currentBody.BeginControl(this);
    }

    private void Update()
    {
        if (restartRequested)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
            return;
        }

        horizontalInput = 0f;
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            RequestRestart();
            return;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            actionRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (currentBody == null || restartRequested)
        {
            return;
        }

        if (pendingTarget != null)
        {
            TransferTo(pendingTarget);
            pendingTarget = null;
        }

        Bounds bounds = currentBody.BodyCollider.bounds;

        if (bounds.max.y < minimumHeight || bounds.min.y > maximumHeight)
        {
            RequestRestart();
            return;
        }

        currentBody.ReceiveInput(horizontalInput, actionRequested);
        currentBody.ControlFixedUpdate();
        actionRequested = false;
    }

    public void ConfigureInitialBody(ControllableBody body)
    {
        initialBody = body;
    }

    public void RequestPossession(
        ControllableBody source,
        ControllableBody target)
    {
        if (source != currentBody ||
            target == null ||
            !target.CanBePossessed ||
            pendingTarget != null)
        {
            return;
        }

        pendingTarget = target;
    }

    public void KillControlledBody(ControllableBody body)
    {
        if (body == currentBody)
        {
            RequestRestart();
        }
    }

    public void CompleteLevel(ControllableBody body)
    {
        if (body == currentBody && body is RollingBallController)
        {
            IsComplete = true;
        }
    }

    public void RequestRestart()
    {
        if (restartRequested)
        {
            return;
        }

        restartRequested = true;
        horizontalInput = 0f;
        actionRequested = false;
        pendingTarget = null;
    }

    private void TransferTo(ControllableBody target)
    {
        ControllableBody previous = currentBody;
        previous.EndControl();

        currentBody = target;
        currentBody.BeginControl(this);

        if (previous.HideAfterControlLost)
        {
            previous.gameObject.SetActive(false);
        }

        Debug.Log($"Possessed {currentBody.name}.", currentBody);
    }
}
