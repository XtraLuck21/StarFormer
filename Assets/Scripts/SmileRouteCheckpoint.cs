using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SmileRouteCheckpoint : MonoBehaviour
{
    [SerializeField] private SmileFaceFinalRoute route;
    [SerializeField] private bool upper;
    [SerializeField] private bool foreheadEntry;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private SpriteRenderer marker;
    public bool WasSaved { get; private set; }
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void Start() => RefreshMarker();
    public void RefreshMarker()
    {
        WasSaved = route.IsCheckpointActive(upper, foreheadEntry);
        if (WasSaved && marker != null) marker.color = new Color(.35f, 1f, .65f);
    }
    private void OnTriggerEnter2D(Collider2D other) => Save(other);
    private void OnTriggerStay2D(Collider2D other) => Save(other);
    private void Save(Collider2D other)
    {
        if (other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PossessableBody body) || !body.IsControlled) return;
        if (foreheadEntry) route.EnterForeheadCamera();
        route.SaveCheckpoint(upper, spawnPosition, foreheadEntry);
        RefreshMarker();
    }
}
