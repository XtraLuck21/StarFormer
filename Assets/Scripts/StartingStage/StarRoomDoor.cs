using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class StarRoomDoor : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private DarkStarPassage darkPassage;
    [SerializeField] private level2Camera levelCamera;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Collider2D arrivalSurface;
    [SerializeField] private SeesawRoomSupplies roomSupplies;
    [SerializeField] private SpriteRenderer transitionVeil;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;
    [SerializeField, Min(0f)] private float arrivalClearance = 0.2f;
    [SerializeField, Min(0f)] private float arrivalHopSpeed = 3.5f;

    private bool busy;
    private bool used;
    private bool controllerWasEnabled;
    private RigidbodyType2D previousBodyType;
    private PossessableBody travellingStar;

    public bool HasTeleported => used;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }
        if (transitionVeil != null)
        {
            transitionVeil.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryEnter(other);
    private void OnTriggerStay2D(Collider2D other) => TryEnter(other);

    private void TryEnter(Collider2D other)
    {
        if (busy || used || player == null || player.IsDead ||
            arrivalSurface == null || levelCamera == null || Time.timeScale <= 0f)
        {
            return;
        }
        PossessableBody star = player.CurrentBody;
        if (star == null || star.Form != BodyForm.Star || !star.IsControlled ||
            other.attachedRigidbody != star.Body)
        {
            return;
        }
        StartCoroutine(JumpToRoom(star));
    }

    private IEnumerator JumpToRoom(PossessableBody star)
    {
        busy = true;
        travellingStar = star;
        controllerWasEnabled = player.enabled;
        previousBodyType = star.Body.bodyType;
        float halfHeight = star.BodyCollider != null
            ? star.BodyCollider.bounds.extents.y : 0.42f;
        player.enabled = false;
        star.Body.linearVelocity = Vector2.zero;
        star.Body.angularVelocity = 0f;
        star.Body.bodyType = RigidbodyType2D.Kinematic;

        yield return Fade(0f, 1f);
        if (star == null || player == null || player.IsDead)
        {
            RestoreControl();
            yield break;
        }

        if (darkPassage != null)
        {
            darkPassage.CompletePassage();
        }
        if (roomSupplies != null) roomSupplies.EnterRoom();
        // Query the authored board, not its parent/pivot at the fulcrum below.
        Bounds bounds = arrivalSurface.bounds;
        Vector2 point = arrivalSurface.ClosestPoint(new Vector2(
            bounds.center.x, bounds.max.y + 2f));
        Vector2 arrivalPosition = point + Vector2.up * (halfHeight + arrivalClearance);
        if (roomSupplies != null)
            arrivalPosition = roomSupplies.ClearArrivalPosition(arrivalPosition, halfHeight, arrivalClearance);
        // Keep the body registered with physics while moving both poses.
        star.transform.position = new Vector3(arrivalPosition.x, arrivalPosition.y,
            star.transform.position.z);
        star.Body.position = arrivalPosition;
        Physics2D.SyncTransforms();
        levelCamera.BeginSeesawRoom(arrivalSurface);
        used = true;

        yield return Fade(1f, 0f);
        if (star != null)
        {
            star.Body.bodyType = previousBodyType;
            star.Body.linearVelocity = Vector2.up * arrivalHopSpeed;
            Physics2D.SyncTransforms();
        }
        RestoreControl();
    }

    private IEnumerator Fade(float from, float to)
    {
        float age = 0f;
        while (age < fadeDuration)
        {
            age += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(age / fadeDuration))));
            yield return null;
        }
        SetFade(to);
    }

    private void SetFade(float opacity)
    {
        if (transitionVeil == null)
        {
            return;
        }
        transitionVeil.enabled = opacity > 0f;
        transitionVeil.color = new Color(0f, 0f, 0f, opacity);
        if (viewCamera != null)
        {
            transitionVeil.transform.position = new Vector3(
                viewCamera.transform.position.x, viewCamera.transform.position.y, 0f);
            transitionVeil.transform.localScale = new Vector3(
                viewCamera.orthographicSize * viewCamera.aspect * 2f + 2f,
                viewCamera.orthographicSize * 2f + 2f, 1f);
        }
    }

    private void RestoreControl()
    {
        if (travellingStar != null)
        {
            travellingStar.Body.bodyType = previousBodyType;
        }
        if (player != null)
        {
            player.enabled = controllerWasEnabled;
        }
        travellingStar = null;
        busy = false;
        SetFade(0f);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (busy)
        {
            RestoreControl();
        }
    }
}
