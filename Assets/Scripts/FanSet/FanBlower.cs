using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PossessableBody))]
public class FanBlower : MonoBehaviour
{
    [SerializeField] private float maxRange = 4f;
    // Horizontal acceleration while blowing. Applied as Acceleration so
    // heavy obstacles still get shoved while the star cannot.
    [SerializeField] private float pushForce = 22f;
    [SerializeField] private float airflowWidth = 1.2f;

    public bool IsBlowing => isBlowing;
    public float MaxRange => maxRange;
    public float AirflowWidth => airflowWidth;
    public int Facing => facing;

    private PossessableBody possessable;
    private Rigidbody2D body;

    private bool isBlowing;
    private int facing = 1;

    private readonly List<Collider2D> overlaps =
        new List<Collider2D>(16);

    private void Awake()
    {
        possessable = GetComponent<PossessableBody>();
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!possessable.IsControlled)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            facing = -1;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            facing = 1;
        }

        if (keyboard.wKey.wasPressedThisFrame)
        {
            isBlowing = !isBlowing;
        }
    }

    private void FixedUpdate()
    {
        if (!isBlowing)
        {
            return;
        }

        Vector2 origin = body.position;
        Vector2 dir = new Vector2(facing, 0f);
        Vector2 center = origin + dir * (maxRange * 0.5f);
        Vector2 size = new Vector2(maxRange, airflowWidth);

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(
            Physics2D.GetLayerCollisionMask(gameObject.layer)
        );
        filter.useTriggers = false;

        overlaps.Clear();
        Physics2D.OverlapBox(center, size, 0f, filter, overlaps);

        foreach (Collider2D other in overlaps)
        {
            Rigidbody2D target = other.attachedRigidbody;

            if (target == null || target == body)
            {
                continue;
            }

            // Airflow only pushes plain obstacles, never possessable bodies.
            if (target.TryGetComponent(out PossessableBody _))
            {
                continue;
            }

            Vector2 point = other.bounds.center;
            float along = Vector2.Dot(point - origin, dir);

            if (along < 0f || along > maxRange)
            {
                continue;
            }

            // Keep a force floor so far objects still slide instead of
            // stalling against ground friction as the falloff hits zero.
            float falloff = 1f - along / maxRange;
            falloff = 0.4f + 0.6f * falloff;

            // ForceMode2D has no Acceleration. Scaling by mass makes the
            // resulting acceleration mass-independent, so wind still moves
            // heavy obstacles while the star cannot shove them.
            target.AddForce(
                dir * pushForce * falloff * target.mass,
                ForceMode2D.Force
            );
        }
    }

    public void ToggleBlow()
    {
        isBlowing = !isBlowing;
    }

    public void SetFacing(int direction)
    {
        if (direction != 0)
        {
            facing = direction > 0 ? 1 : -1;
        }
    }
}
