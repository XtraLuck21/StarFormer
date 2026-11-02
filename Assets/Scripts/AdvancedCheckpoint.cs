using TMPro;
using UnityEngine;

// A non-solid, fixed footprint. The outline remains after the player moves away.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class AdvancedCheckpoint : MonoBehaviour
{
    [SerializeField] private AdvancedCheckpointController controller;
    [SerializeField, Range(1, 3)] private int index = 1;
    [SerializeField] private PossessableBody respawnBody;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private bool requireLanding;
    [SerializeField] private float floorY;
    [SerializeField] private LineRenderer outline;
    [SerializeField] private TMP_Text savedLabel;
    [SerializeField] private Color unsavedColor = new Color32(47, 155, 236, 255);
    [SerializeField] private Color savedColor = new Color32(255, 211, 77, 255);

    private float savedUntil;
    public int Index => index;
    public PossessableBody RespawnBody => respawnBody;
    public Vector3 SpawnPosition => spawnPosition;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        savedLabel.enabled = false;
    }

    private void Start() => RefreshVisual();
    private void Update() => RefreshVisual();

    private void RefreshVisual()
    {
        Color color = controller.WasReached(index) ? savedColor : unsavedColor;
        outline.startColor = outline.endColor = color;
        savedLabel.enabled = Time.time < savedUntil;
    }

    private void OnTriggerEnter2D(Collider2D other) => TrySave(other);
    private void OnTriggerStay2D(Collider2D other) => TrySave(other);

    private void TrySave(Collider2D other)
    {
        if (other.attachedRigidbody == null ||
            !other.attachedRigidbody.TryGetComponent(out PossessableBody body)) return;
        // The upper choice is only complete after Star/Box reaches solid left ground.
        if (requireLanding && ((body.Form != BodyForm.Star && body.Form != BodyForm.Box) ||
            !body.IsGrounded() || Mathf.Abs(body.BodyCollider.bounds.min.y - floorY) > .2f)) return;
        if (!controller.TryActivate(this, body)) return;
        savedUntil = Time.time + 2.5f;
        RefreshVisual();
    }
}
