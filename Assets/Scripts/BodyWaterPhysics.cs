using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D), typeof(PossessableBody))]
public class BodyWaterPhysics : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float waterLinearDamping = 2f;

    private Rigidbody2D body;
    private PossessableBody possessable;

    private float originalGravityScale;
    private float originalLinearDamping;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        possessable = GetComponent<PossessableBody>();

        originalGravityScale = body.gravityScale;
        originalLinearDamping = body.linearDamping;
    }

    private void OnEnable()
    {
        RefreshWaterPhysics();
    }

    private void FixedUpdate()
    {
        RefreshWaterPhysics();
    }

    public void RefreshWaterPhysics()
    {
        if (!isActiveAndEnabled ||
            body == null ||
            possessable == null)
        {
            return;
        }

        if (!possessable.IsInWater)
        {
            RestoreAirPhysics();
            return;
        }

        bool isBubble = possessable.Form == BodyForm.Bubble;

        // Stars and boxes retain gravity. Bubbles suspend in water.
        body.gravityScale = isBubble ? 0f : originalGravityScale;

        // An unpossessed bubble is a stable target in the water. It stays at
        // its authored position until the player touches and possesses it.
        if (isBubble && !possessable.IsControlled)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        // Controlled swimming handles acceleration and braking itself.
        body.linearDamping = isBubble && possessable.IsControlled
            ? 0f
            : waterLinearDamping;
    }

    private void RestoreAirPhysics()
    {
        body.gravityScale = originalGravityScale;
        body.linearDamping = originalLinearDamping;
    }

    private void OnDisable()
    {
        if (body != null)
        {
            RestoreAirPhysics();
        }
    }
}
