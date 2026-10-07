using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerPossessionController : MonoBehaviour
{
    [SerializeField] private PossessableBody initialBody;
    [SerializeField] private float minimumHeight = -10f;
    [SerializeField] private float maximumHeight = 8f;
    [SerializeField] private float maximumRiseSpeed = 3f;

    private PossessableBody currentBody;
    private PossessableBody pendingTarget;
    private PossessableBody blockedPreviousBody;

    private float horizontalInput;
    private bool actionRequested;
    private bool restartRequested;

    private void Start()
    {
        if (initialBody == null ||
            !initialBody.isActiveAndEnabled ||
            !initialBody.IsReady)
        {
            Debug.LogError(
                "Assign a configured, active initial body.",
                this
            );

            enabled = false;
            return;
        }

        currentBody = initialBody;
        currentBody.SetOwner(this);
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
            actionRequested = false;
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

        Bounds bodyBounds = currentBody.BodyCollider.bounds;

        if (bodyBounds.max.y < minimumHeight ||
            bodyBounds.min.y > maximumHeight)
        {
            RequestRestart();
            return;
        }

        // Give the intentional pop action priority over a pending transfer.
        if (currentBody.Form == BodyForm.Bubble && actionRequested)
        {
            PopBubble();
            return;
        }

        UpdateContactLock();

        if (pendingTarget != null)
        {
            PossessableBody target = pendingTarget;
            pendingTarget = null;

            if (target.isActiveAndEnabled && target.IsReady)
            {
                TransferTo(target);
            }
        }

        Rigidbody2D body = currentBody.Body;

        Vector2 velocity = body.linearVelocity;

        switch (currentBody.Form)
        {
            case BodyForm.Star:
                velocity.x = horizontalInput * currentBody.MoveSpeed;

                if (actionRequested &&
                    velocity.y <= 0.1f &&
                    currentBody.IsGrounded())
                {
                    velocity.y = currentBody.JumpSpeed;
                }

                break;

            case BodyForm.Box:
                if (currentBody.IsGrounded())
                {
                    velocity.x = horizontalInput * currentBody.MoveSpeed;
                }

                break;

            case BodyForm.Bubble:
                velocity.x = horizontalInput * currentBody.MoveSpeed;
                velocity.y = Mathf.Min(velocity.y, maximumRiseSpeed);
                break;

            case BodyForm.Fan:
                if (currentBody.IsGrounded())
                {
                    velocity.x = horizontalInput * currentBody.MoveSpeed;
                }

                break;
        }

        body.linearVelocity = velocity;
        actionRequested = false;
    }

    public void RequestRestart()
    {
        if (restartRequested)
        {
            return;
        }

        if (gameObject.scene.buildIndex < 0)
        {
            Debug.LogError(
                "Add the current scene to the enabled build scene list before restarting.",
                this
            );
            return;
        }

        restartRequested = true;
        pendingTarget = null;
        horizontalInput = 0f;
        actionRequested = false;
    }

    public void KillControlledBody(PossessableBody hitBody)
    {
        if (hitBody == null || hitBody != currentBody)
        {
            return;
        }

        RequestRestart();
    }

    public void RequestPossession(
        PossessableBody source,
        PossessableBody target)
    {
        if (!isActiveAndEnabled ||
            restartRequested ||
            source != currentBody ||
            target == currentBody ||
            target == blockedPreviousBody ||
            pendingTarget != null ||
            !target.isActiveAndEnabled ||
            !target.IsReady)
        {
            return;
        }

        pendingTarget = target;
    }

    private void PopBubble()
    {
        actionRequested = false;

        if (initialBody == null ||
            !initialBody.IsReady ||
            initialBody.Form != BodyForm.Star)
        {
            Debug.LogError(
                "Popping a bubble requires a valid initial star body.",
                this
            );

            return;
        }

        PossessableBody bubble = currentBody;
        Vector2 releasePosition = bubble.Body.position;
        Vector2 releaseVelocity = bubble.Body.linearVelocity;

        // Remove upward momentum so the released star starts falling.
        releaseVelocity.y = Mathf.Min(releaseVelocity.y, 0f);

        pendingTarget = null;

        bubble.SetOwner(null);
        bubble.gameObject.SetActive(false);

        // Position the inactive star before restoring its physics and visuals.
        Transform starTransform = initialBody.transform;
        starTransform.position = new Vector3(
            releasePosition.x,
            releasePosition.y,
            starTransform.position.z
        );

        initialBody.gameObject.SetActive(true);
        initialBody.Body.position = releasePosition;
        initialBody.Body.linearVelocity = releaseVelocity;
        initialBody.Body.angularVelocity = 0f;

        currentBody = initialBody;
        currentBody.SetOwner(this);

        // Preserve the previous-body lock until actual separation.
        Debug.Log("Bubble popped. Returned to star form.", currentBody);
    }

    private void TransferTo(PossessableBody target)
    {
        PossessableBody previousBody = currentBody;

        previousBody.SetOwner(null);
        currentBody = target;
        currentBody.SetOwner(this);

        blockedPreviousBody = previousBody;

        // Do not carry a jump press into a newly possessed body.
        actionRequested = false;

        // The original star merges into its first host.
        // Other bodies remain in the world with their existing physics.
        if (previousBody.Form == BodyForm.Star)
        {
            previousBody.gameObject.SetActive(false);
        }

        Debug.Log($"Possessed {currentBody.Form}.", currentBody);
    }

    private void UpdateContactLock()
    {
        if (blockedPreviousBody == null)
        {
            return;
        }

        // Re-entry is allowed only after the two bodies separate.
        if (!blockedPreviousBody.gameObject.activeInHierarchy ||
            !currentBody.BodyCollider.IsTouching(
                blockedPreviousBody.BodyCollider))
        {
            blockedPreviousBody = null;
        }
    }
}