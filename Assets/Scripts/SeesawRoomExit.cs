using System.Collections;
using UnityEngine;

/// <summary>All four gears reveal an elevator to the lower-left Smile Face entrance.</summary>
[DefaultExecutionOrder(150)]
public sealed class SeesawRoomExit : MonoBehaviour
{
    public const string SmileCheckpointId = "SMILE_FACE_ENTRY";
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawGear2D[] gears;
    [SerializeField] private SeesawBouncyFinale finale;
    [SerializeField] private BoxCollider2D returnBarrier;
    [SerializeField] private level2Camera roomCamera;
    [SerializeField] private SpriteRenderer sourceVisual;
    [SerializeField] private Collider2D arrivalSurface;
    [SerializeField] private LineRenderer entryCheckpointOutline;
    [SerializeField] private BoxCollider2D roomCeiling;
    [SerializeField] private float arrivalX = 28.03f;
    [SerializeField] private Vector2 platformLocalPosition = new Vector2(.21f, 2.8f);
    [SerializeField] private Vector2 platformSize = new Vector2(2.8f, .35f);
    [SerializeField, Min(.1f)] private float shakeSeconds = 2f;
    [SerializeField, Min(0f)] private float shakeStrength = .13f;
    [SerializeField, Min(.1f)] private float revealSeconds = .9f;
    [SerializeField, Min(.1f)] private float rideSeconds = 7f;
    [SerializeField, Min(.1f)] private float boardingSeconds = .35f;
    public bool CompletionStarted { get; private set; }
    public bool IsReady { get; private set; }
    public bool HasTransferred { get; private set; }
    public bool IsTransferring { get; private set; }
    public float RevealProgress { get; private set; }
    public float RideProgress { get; private set; }
    public BoxCollider2D Surface { get; private set; }

    private GameObject platform;
    private Rigidbody2D platformBody;
    private GameObject portalCues;
    private Vector2 platformRest;
    private float completionAge;
    private PossessableBody travellingBody;
    private BouncyBallAbility travellingAbility;
    private bool controllerWasEnabled, abilityWasEnabled, bodyWasSimulated;
    private float travellingHalfHeight;
    private float roomExitHeight;
    private SeesawBallShellExit shellEffect;

    private void Awake()
    {
        platform = new GameObject("Central Smile Face Elevator");
        platform.transform.SetParent(transform, false);
        platformRest = transform.TransformPoint(platformLocalPosition);
        platformBody = platform.AddComponent<Rigidbody2D>();
        platformBody.bodyType = RigidbodyType2D.Kinematic;
        platformBody.gravityScale = 0f;
        Surface = platform.AddComponent<BoxCollider2D>();
        Surface.size = platformSize;
        Surface.enabled = false;
        var deck = new GameObject("Elevator Platform Surface");
        deck.transform.SetParent(platform.transform, false);
        deck.transform.localScale = new Vector3(platformSize.x / sourceVisual.sprite.bounds.size.x,
            platformSize.y / sourceVisual.sprite.bounds.size.y, 1f);
        var renderer = deck.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceVisual.sprite;
        renderer.sharedMaterial = sourceVisual.sharedMaterial;
        renderer.sortingLayerID = sourceVisual.sortingLayerID;
        renderer.sortingOrder = 4;
        renderer.color = new Color32(154, 120, 220, 255);
        BuildLiftCues();
        platform.SetActive(false);
    }

    private void Start()
    {
        RefreshEntryCheckpoint();
        if (!player.IsCurrentCheckpoint(SmileCheckpointId) && !SmileFaceFinalRoute.IsRouteCheckpoint(player)) return;
        HasTransferred = true;
        roomCamera.BeginRemainingFollow();
    }

    private void RefreshEntryCheckpoint()
    {
        if (entryCheckpointOutline == null) return;
        bool saved = player.IsCurrentCheckpoint(SmileCheckpointId) || SmileFaceFinalRoute.IsRouteCheckpoint(player);
        entryCheckpointOutline.startColor = entryCheckpointOutline.endColor = saved
            ? new Color32(255, 211, 77, 255) : new Color32(47, 155, 236, 255);
    }

    private bool AllGearsRunning()
    {
        if (gears == null || gears.Length != 4) return false;
        foreach (SeesawGear2D gear in gears)
            if (gear == null || !gear.IsRunning) return false;
        return true;
    }

    private void Update()
    {
        RefreshEntryCheckpoint();
        if (HasTransferred || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        if (!CompletionStarted)
        {
            if (!AllGearsRunning()) return;
            CompletionStarted = true;
            roomCamera.ShakeSeesawRoom(shakeSeconds, shakeStrength);
        }
        completionAge += Time.deltaTime;
        if (IsReady || completionAge < shakeSeconds) return;
        platform.SetActive(true);
        RevealProgress = Mathf.Clamp01((completionAge - shakeSeconds) / Mathf.Max(.1f, revealSeconds));
        platformBody.position = platformRest + Vector2.down *
            Mathf.Lerp(1.2f, 0f, Mathf.SmoothStep(0f, 1f, RevealProgress));
        if (RevealProgress < 1f || !finale.IsLeftBridgeReady) return;
        IsReady = true;
        Surface.enabled = true;
        portalCues.SetActive(true);
        // The final ball must now be able to leave its bounce chamber for the central platform.
        returnBarrier.enabled = false;
    }

    private void FixedUpdate()
    {
        if (!IsReady || HasTransferred || IsTransferring || !player.enabled || player.IsDead ||
            player.IsRestarting || Time.timeScale <= 0f) return;
        PossessableBody body = player.CurrentBody;
        if (body == null || !body.IsControlled || body.BodyCollider == null) return;
        Bounds feet = body.BodyCollider.bounds;
        Bounds deck = Surface.bounds;
        if (feet.min.y < deck.max.y - .1f || feet.min.y > deck.max.y + .2f ||
            feet.max.x < deck.min.x + .15f || feet.min.x > deck.max.x - .15f) return;
        StartCoroutine(Transfer(body));
    }

    private IEnumerator Transfer(PossessableBody body)
    {
        IsTransferring = true;
        travellingBody = body;
        controllerWasEnabled = player.enabled;
        bodyWasSimulated = body.Body.simulated;
        travellingHalfHeight = body.BodyCollider.bounds.extents.y;
        travellingAbility = body.GetComponent<BouncyBallAbility>();
        if (travellingAbility != null)
        {
            abilityWasEnabled = travellingAbility.enabled;
            travellingAbility.enabled = false;
        }
        if (body.TryGetComponent(out HexagonSurfaceMovement spider)) spider.Release();
        player.ClearInputForRestartMenu();
        player.enabled = false;
        body.Body.linearVelocity = Vector2.zero;
        body.Body.angularVelocity = 0f;
        body.Body.simulated = false;
        Surface.enabled = false;
        portalCues.SetActive(false);
        OpenElevatorShaft();
        roomCamera.BeginElevatorRide(body.transform);

        Vector2 riderStart = body.Body.position;
        Vector2 seatedPosition = platformBody.position + Vector2.up *
            (platformSize.y * .5f + travellingHalfHeight + .08f);
        float boardingAge = 0f;
        while (boardingAge < boardingSeconds)
        {
            if (body == null || player.IsDead || player.IsRestarting) { RestoreControl(); yield break; }
            boardingAge += Time.deltaTime;
            PlaceRider(Vector2.Lerp(riderStart, seatedPosition,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(boardingAge / Mathf.Max(.1f, boardingSeconds)))));
            yield return null;
        }

        Vector2 liftFrom = platformBody.position;
        Vector2 liftTo = new Vector2(Mathf.Clamp(arrivalX, arrivalSurface.bounds.min.x + platformSize.x * .5f,
            arrivalSurface.bounds.max.x - platformSize.x * .5f), arrivalSurface.bounds.max.y - platformSize.y * .5f);
        float rideAge = 0f;
        float shellAge = 0f;
        bool openingShell = false;
        bool releasedShell = false;
        bool boardedAsBall = body.Form == BodyForm.BouncyBall;
        while (rideAge < rideSeconds)
        {
            if (travellingBody == null || player.IsDead || player.IsRestarting) { RestoreControl(); yield break; }
            rideAge += Time.deltaTime;
            RideProgress = Mathf.Clamp01(rideAge / Mathf.Max(.1f, rideSeconds));
            Vector2 deckPosition = Vector2.Lerp(liftFrom, liftTo, Mathf.SmoothStep(0f, 1f, RideProgress));
            platformBody.position = deckPosition;
            platform.transform.position = new Vector3(deckPosition.x, deckPosition.y, platform.transform.position.z);
            PlaceRider(deckPosition + Vector2.up * (platformSize.y * .5f + travellingHalfHeight + .08f));
            if (boardedAsBall && !openingShell && deckPosition.y + platformSize.y * .5f >= roomExitHeight)
            {
                openingShell = true;
                shellEffect = SeesawBallShellExit.Create(travellingBody, platform.transform);
            }
            if (openingShell)
            {
                shellAge += Time.deltaTime;
                if (shellEffect != null) shellEffect.SetAge(shellAge);
                if (!releasedShell && shellAge >= SeesawBallShellExit.CrackSeconds &&
                    player.ReleaseBouncyShellForElevator(travellingBody, out PossessableBody star))
                {
                    releasedShell = true;
                    travellingBody = star;
                    travellingAbility = null;
                    bodyWasSimulated = star.Body.simulated;
                    Physics2D.SyncTransforms();
                    travellingHalfHeight = star.BodyCollider.bounds.extents.y;
                    star.Body.simulated = false;
                    PlaceRider(deckPosition + Vector2.up * (platformSize.y * .5f + travellingHalfHeight + .08f));
                    roomCamera.RetargetElevatorRider(star.transform);
                }
                if (shellEffect != null && shellAge >= SeesawBallShellExit.Duration)
                {
                    Destroy(shellEffect.gameObject);
                    shellEffect = null;
                }
            }
            yield return null;
        }
        platformBody.position = liftTo;
        PlaceRider(liftTo + Vector2.up * (platformSize.y * .5f + travellingHalfHeight + .08f));
        roomCamera.BeginRemainingFollow();
        // The authored lower-left floor supports the rider when the elevator docks.
        player.SetCheckpoint(SmileCheckpointId, new Vector3(liftTo.x, arrivalSurface.bounds.max.y + .65f, 0f), false);
        HasTransferred = true;
        RestoreControl();
    }

    private void PlaceRider(Vector2 position)
    {
        travellingBody.transform.position = new Vector3(position.x, position.y, travellingBody.transform.position.z);
        travellingBody.Body.position = position;
    }

    private void OpenElevatorShaft()
    {
        Bounds ceiling = roomCeiling.bounds;
        roomExitHeight = ceiling.max.y;
        var renderer = roomCeiling.GetComponent<SpriteRenderer>();
        float halfShaft = platformSize.x * .5f + .25f;
        float shaftLeft = platformRest.x - halfShaft;
        float shaftRight = platformRest.x + halfShaft;
        CreateCeilingSide("Ceiling Left of Elevator", ceiling.min.x, shaftLeft, ceiling, renderer);
        CreateCeilingSide("Ceiling Right of Elevator", shaftRight, ceiling.max.x, ceiling, renderer);
        roomCeiling.enabled = false;
        renderer.enabled = false;
    }

    private void CreateCeilingSide(string name, float minX, float maxX, Bounds ceiling, SpriteRenderer original)
    {
        if (maxX <= minX) return;
        var side = new GameObject(name);
        side.transform.SetParent(transform, false);
        side.transform.position = new Vector3((minX + maxX) * .5f, ceiling.center.y, original.transform.position.z);
        side.transform.localScale = new Vector3((maxX - minX) / original.sprite.bounds.size.x,
            ceiling.size.y / original.sprite.bounds.size.y, 1f);
        var visual = side.AddComponent<SpriteRenderer>();
        visual.sprite = original.sprite;
        visual.sharedMaterial = original.sharedMaterial;
        visual.color = original.color;
        visual.sortingLayerID = original.sortingLayerID;
        visual.sortingOrder = original.sortingOrder;
        var collider = side.AddComponent<BoxCollider2D>();
        collider.size = original.sprite.bounds.size;
    }

    private void RestoreControl()
    {
        if (shellEffect != null) Destroy(shellEffect.gameObject);
        shellEffect = null;
        if (travellingBody != null)
        {
            travellingBody.Body.linearVelocity = Vector2.zero;
            travellingBody.Body.angularVelocity = 0f;
            travellingBody.Body.simulated = bodyWasSimulated;
        }
        if (travellingAbility != null) travellingAbility.enabled = abilityWasEnabled;
        if (player != null)
        {
            player.ClearInputForRestartMenu();
            player.enabled = controllerWasEnabled;
        }
        travellingBody = null;
        travellingAbility = null;
        IsTransferring = false;
    }

    private void BuildLiftCues()
    {
        portalCues = new GameObject("Smile Face Elevator Cues");
        portalCues.transform.SetParent(platform.transform, false);
        for (int i = 0; i < 3; i++)
        {
            var chevron = new GameObject("Elevator Up Arrow");
            chevron.transform.SetParent(portalCues.transform, false);
            var line = chevron.AddComponent<LineRenderer>();
            line.sharedMaterial = sourceVisual.sharedMaterial;
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.widthMultiplier = .045f;
            line.startColor = line.endColor = new Color(.8f, .65f, 1f);
            line.sortingOrder = 8;
            float y = .5f + i * .3f;
            line.SetPositions(new[] { new Vector3(-.3f, y, 0f), new Vector3(0f, y + .2f, 0f), new Vector3(.3f, y, 0f) });
        }
        portalCues.SetActive(false);
    }

    private void OnDisable()
    {
        if (!IsTransferring) return;
        StopAllCoroutines();
        RestoreControl();
    }
}
