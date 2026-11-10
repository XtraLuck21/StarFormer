using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ChaseTrapdoor : MonoBehaviour
{
    [SerializeField] private SpikeWallEnemyChase spikeWall;
    [SerializeField] private level2Camera levelCamera;
    [SerializeField] private DarkStarPassage darkPassage;
    [Header("Star Drop Animation")]
    [SerializeField, Min(0f)] private float fallSpinSpeed = 240f;
    [SerializeField, Min(0.01f)] private float spinAcceleration = 480f;
    [SerializeField, Min(0.01f)] private float landingSettleDuration = 0.25f;
    private bool opened;
    private Transform spinningVisual;
    private Quaternion restingRotation;

    private void OnCollisionEnter2D(Collision2D collision) => CheckLanding(collision);
    private void OnCollisionStay2D(Collision2D collision) => CheckLanding(collision);

    private void CheckLanding(Collision2D collision)
    {
        Rigidbody2D body = collision.rigidbody;
        if (opened || body == null ||
            !body.TryGetComponent<PossessableBody>(out var playerBody) ||
            !playerBody.IsControlled || playerBody.Owner.IsDead ||
            playerBody.Owner.CurrentBody != playerBody)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= -0.5f)
            {
                continue;
            }

            opened = true;
            GetComponent<BoxCollider2D>().enabled = false;
            if (TryGetComponent<SpriteRenderer>(out var visual))
            {
                visual.enabled = false;
            }
            if (spikeWall != null)
            {
                spikeWall.EndChase();
            }
            // Override the chase-end focus shot so the fall remains visible.
            if (levelCamera != null)
            {
                levelCamera.BeginTrapdoorDrop();
            }
            if (playerBody.Form == BodyForm.Star)
            {
                if (darkPassage != null)
                {
                    darkPassage.BeginDescent(playerBody);
                }
                StartCoroutine(AnimateStarDrop(playerBody));
            }
            return;
        }
    }

    private IEnumerator AnimateStarDrop(PossessableBody star)
    {
        StarVisual art = star.GetComponentInChildren<StarVisual>(true);
        if (art == null || art.transform == star.transform)
        {
            yield break;
        }

        // Animate only the artwork; keep the rigidbody and collider upright.
        spinningVisual = art.transform;
        restingRotation = spinningVisual.localRotation;
        float initialY = star.Body.position.y;
        float angle = 0f;
        float speed = 0f;
        bool airborne = false;

        while (star != null && star.IsControlled && !star.Owner.IsDead &&
            spinningVisual != null)
        {
            // The original support contact can survive until the next physics step.
            if (!airborne)
            {
                airborne = star.Body.linearVelocity.y < -0.3f ||
                    star.Body.position.y < initialY - 0.1f;
            }
            if (airborne && star.IsGrounded())
            {
                break;
            }
            if (airborne)
            {
                speed = Mathf.MoveTowards(speed, fallSpinSpeed,
                    spinAcceleration * Time.deltaTime);
                angle = Mathf.Repeat(angle - speed * Time.deltaTime, 360f);
                spinningVisual.localRotation = restingRotation *
                    Quaternion.AngleAxis(angle, Vector3.forward);
            }
            yield return null;
        }

        if (spinningVisual == null)
        {
            yield break;
        }
        Quaternion landingRotation = spinningVisual.localRotation;
        float elapsed = 0f;
        while (spinningVisual != null && elapsed < landingSettleDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / landingSettleDuration));
            spinningVisual.localRotation = Quaternion.Slerp(
                landingRotation, restingRotation, progress);
            yield return null;
        }
        RestoreVisual();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreVisual();
    }

    private void RestoreVisual()
    {
        if (spinningVisual != null)
        {
            spinningVisual.localRotation = restingRotation;
        }
        spinningVisual = null;
    }
}
