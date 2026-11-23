using System.Collections.Generic;
using UnityEngine;

/// <summary>Room-local transmission, direct passage switch and ratchet feedback. No shared form rules.</summary>
[ExecuteAlways]
public sealed class SeesawMachineRoom : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private SeesawRoomStage stage;
    [SerializeField] private SeesawOverloadSequence loading;
    [SerializeField] private SeesawBouncyFinale finale;
    [SerializeField] private SeesawRoomExit lift;
    [SerializeField] private SeesawRoomSupplies supplies;
    [SerializeField] private SeesawGear2D[] gears;
    [SerializeField] private Transform[] contacts;
    [SerializeField] private Transform frame;
    [SerializeField] private BoxCollider2D impactSurface;
    [SerializeField] private Transform bridgeHinge;
    [SerializeField] private BoxCollider2D bridgeSurface;
    [SerializeField] private Transform[] maintenanceGates;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<GameObject> drawings = new List<GameObject>();
    private readonly LineRenderer[] belts = new LineRenderer[2];
    private readonly Transform[] lamps = new Transform[4];
    private Material material;
    private SeesawLiftAssembly previewCarriage;
    private Vector3[] climbGateRest;
    [SerializeField] private Transform[] climbGates;
    private Vector3[] gateRest;
    private float bridgeProgress, viewAge;
    private bool bridgeRequested, passageSeen, cameraInitialized;
    private float passageOverviewSeconds, cameraSize;
    private Vector3 cameraPosition;
    private int cameraFrame = -1;
    private static readonly Color Ink = new Color32(57, 65, 65, 255);
    private static readonly Color Metal = new Color32(207, 208, 184, 255);
    private static readonly Color Timber = new Color32(213, 166, 103, 255);
    public Collider2D ImpactSurface => impactSurface;
    public bool IsBridgeReady => bridgeProgress >= 1f;
    public bool ReferencesReady => player != null && stage != null && loading != null && finale != null &&
        lift != null && supplies != null && gears != null && gears.Length == 4 && contacts != null &&
        contacts.Length == 4 && frame != null && impactSurface != null && bridgeHinge != null && bridgeSurface != null &&
        maintenanceGates != null && maintenanceGates.Length == 2;

    private void OnEnable()
    {
        if (!ReferencesReady) return;
        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave };
        BuildFrame();
        climbGateRest = new Vector3[climbGates == null ? 0 : climbGates.Length];
        for (int i = 0; i < climbGateRest.Length; i++) climbGateRest[i] = climbGates[i].position;
        if (!Application.isPlaying) return;
        bridgeProgress = viewAge = 0f;
        bridgeRequested = passageSeen = cameraInitialized = false;
        passageOverviewSeconds = 0f;
        cameraFrame = -1;
        gateRest = new Vector3[maintenanceGates.Length];
        for (int i = 0; i < gateRest.Length; i++) gateRest[i] = maintenanceGates[i].position;
        bridgeSurface.enabled = false;
    }

    private Transform Draw(SchoolSceneDrawing drawing, string name, Transform parent, int order)
    {
        GameObject item = drawing.Build(name, parent, material, order, out Mesh mesh);
        meshes.Add(mesh);
        drawings.Add(item);
        return item.transform;
    }

    private void BuildFrame()
    {
        if (!Application.isPlaying)
        {
            var preview = new GameObject("Complete carriage preview") { hideFlags = HideFlags.HideAndDontSave };
            preview.transform.SetParent(transform,false);
            preview.transform.position = lift.LoadingPosition;
            drawings.Add(preview);
            previewCarriage = new SeesawLiftAssembly(preview.transform,gears,lift.PlatformSize,
                gears[0].GetComponent<SpriteRenderer>().sortingLayerID);
        }
        for (int i = 0; i < 4; i++)
        {
            Color paint = gears[i].GetComponent<SpriteRenderer>().color;
            var lamp = new SchoolSceneDrawing();
            lamp.Circle(Vector2.zero, .16f, paint);
            lamps[i] = Draw(lamp, "Drive connected indicator", frame, 8);
            lamps[i].position = gears[i].transform.position + Vector3.down * .9f;
        }
        for(int i=0;i<2;i++)
        {
            var go=new GameObject("Short elevator hinge "+i) {hideFlags=HideFlags.HideAndDontSave};
            go.transform.SetParent(transform,false);
            drawings.Add(go);
            var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=material;
            line.useWorldSpace=true;
            line.positionCount=3;
            line.widthMultiplier=.055f;
            line.sortingOrder=-9;
            belts[i]=line;
        }
        UpdateHinges();
        var rack = new SchoolSceneDrawing();
        rack.Rect(0f, .7f, .18f, 1.3f, Metal);
        for (int i = 0; i < 4; i++) rack.Rect(.16f, .15f + i * .3f, .22f, .09f, Ink);
        Draw(rack, "Four-tooth piston rack", impactSurface.transform, 4);
    }

    public void OpenMaintenancePassage(float progress)
    {
        if (gateRest == null) return;
        for (int i = 0; i < maintenanceGates.Length; i++)
        {
            // Retract into the roof, not down into the tunnel's walkable space.
            maintenanceGates[i].position=gateRest[i]+Vector3.up*(.9f*progress);
            foreach(var renderer in maintenanceGates[i].GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color color=renderer.color;color.a=1f-progress;renderer.color=color;
                renderer.enabled=progress<.999f;
            }
        }
    }

    public void OpenClimbRoute(float progress)
    {
        if (climbGateRest == null) return;
        for (int i = 0; i < climbGateRest.Length; i++)
            climbGates[i].position = climbGateRest[i] + Vector3.down * (3.2f * progress);
    }

    private void UpdateHinges()
    {
        for(int i=0;i<2;i++)
        {
            var line=belts[i];
            // Short local arms detach as the carriage departs, never spanning the room.
            line.enabled=!Application.isPlaying || !lift.IsTransferring && !lift.HasTransferred;
            float side=i==0?-1f:1f;
            line.SetPosition(0,gears[i].transform.position);
            line.SetPosition(1,gears[i+2].transform.position);
            line.SetPosition(2,lift.LoadingPosition+new Vector3(side*lift.PlatformSize.x*.5f,1.55f,0f));
            float alpha=gears[i].IsRunning && gears[i+2].IsRunning ? .2f : .12f;
            line.startColor=line.endColor=new Color(Ink.r,Ink.g,Ink.b,alpha);
        }
    }

    public void BeginBridge() => bridgeRequested = true;

    private void Update()
    {
        if (!Application.isPlaying || !ReferencesReady || player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        if (loading.IsPassageOpen && !passageSeen)
        {
            passageSeen = true;
            passageOverviewSeconds = 1.8f;
        }
        passageOverviewSeconds = Mathf.Max(0f, passageOverviewSeconds - Time.deltaTime);
        UpdateHinges();
        for (int i = 0; i < 4; i++)
        {
            lamps[i].localScale = Vector3.one * (gears[i].IsRunning ? 1f : .45f);
        }
        if (!bridgeRequested || IsBridgeReady) return;
        bridgeProgress = Mathf.MoveTowards(bridgeProgress, 1f, Time.deltaTime / 1.2f);
        bridgeHinge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(80f, 0f, Mathf.SmoothStep(0f, 1f, bridgeProgress)));
        if (IsBridgeReady) bridgeSurface.enabled = true;
    }

    public bool TryGetCameraFrame(Camera camera, Rect overview, float z, out Vector3 position, out float size)
    {
        position = default;
        size = 0f;
        if (!Application.isPlaying || !ReferencesReady || !supplies.IsRoomActive || lift.IsTransferring) return false;
        bool nextFrame = cameraFrame != Time.frameCount;
        if (nextFrame) viewAge += Time.deltaTime;
        Rect view = overview;
        if (viewAge > 2.6f && passageOverviewSeconds <= 0f && player.CurrentBody != null)
        {
            Vector2 focus = player.CurrentBody.transform.position;
            if (!stage.IsUnlocked || stage.IsRevealing)
                view = new Rect(10f, -25.5f, 35f, 15f);
            else if (player.CurrentBody.Form == BodyForm.Spider && (focus.y > -17f || !loading.IsPassageOpen))
                view = new Rect(Mathf.Clamp(focus.x - 14f, 10f, 17f), Mathf.Clamp(focus.y - 8f, -25.5f, -18f), 28f, 17f);
            else
                view = new Rect(12f, Mathf.Clamp(focus.y - 10f, -27.5f, -20f), 33f, 17.5f);
        }
        float aspect = Mathf.Clamp(camera.aspect, .1f, 16f / 9f);
        Vector3 target = new Vector3(view.center.x, view.center.y, z);
        float targetSize = Mathf.Max(view.height * .5f, view.width / (2f * aspect));
        if (!cameraInitialized)
        {
            cameraInitialized = true;
            cameraPosition = target;
            cameraSize = targetSize;
        }
        else if (nextFrame)
        {
            float blend = 1f - Mathf.Exp(-5f * Time.deltaTime);
            cameraPosition = Vector3.Lerp(cameraPosition, target, blend);
            cameraSize = Mathf.Lerp(cameraSize, targetSize, blend);
        }
        cameraFrame = Time.frameCount;
        position = cameraPosition;
        size = cameraSize;
        // Keep the current body visible even during a high bounce or a phase pan.
        if (player.CurrentBody != null)
        {
            Vector3 current = player.CurrentBody.transform.position;
            size = Mathf.Max(size, Mathf.Abs(current.y-position.y)+1.3f,
                (Mathf.Abs(current.x-position.x)+1.3f)/aspect);
        }
        return true;
    }

    private void OnDisable()
    {
        previewCarriage?.Dispose();
        previewCarriage = null;
        foreach (GameObject item in drawings) SchoolSceneDrawing.Release(item);
        foreach (Mesh mesh in meshes) SchoolSceneDrawing.Release(mesh);
        drawings.Clear();
        meshes.Clear();
        SchoolSceneDrawing.Release(material);
    }
}
