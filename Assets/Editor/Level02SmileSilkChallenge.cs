using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adds a Spider silk challenge to Level02's Smile Face right-side climb. Safe to run repeatedly.
//
// Route: the door Spider climbs the narrow shaft between the face and the right wall. A strip of
// pins covers every climbable face across one band of the shaft, so climbing alone cannot pass it.
// Spider silks onto a hanging timetable board in the middle of the shaft, then silks from the board
// to the wall above the pins and climbs on to the right-head checkpoint.
public static class Level02SmileSilkChallenge
{
    private const string ScenePath = "Assets/Scenes/Level02.unity";
    private const string SpikePrefabPath = "Assets/Form/SpikeHazard.prefab";
    private const string RootName = "Smile Silk Challenge";

    // World-space layout, from the Smile Face shaft between wall (40) and wall (48).
    private const float ShaftProbeX = 60.1f;          // a point inside the open shaft
    private const float ScanLeftX = 55.0f;            // leftmost face that receives pins
    private const float PinBottom = 11.5f, PinTop = 14.1f;
    private const float PinSize = 0.32f;
    private const float BoardY = 12.85f;
    private static readonly Vector2 BoardSize = new Vector2(1.7f, 0.55f);

    [MenuItem("StarFormer/Build Level02 Smile Silk Challenge")]
    public static void BuildFromMenu()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }
        Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Level02 Smile silk challenge built. Playtest from the SMILE_SPIDER_BASE checkpoint.");
    }

    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);
            BuildFromMenu();
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static void Apply()
    {
        var player = Object.FindAnyObjectByType<PlayerPossessionController>();
        var route = Object.FindAnyObjectByType<SmileFaceFinalRoute>();
        if (player == null || route == null)
            throw new System.InvalidOperationException("Level02 needs PlayerPossessionController and SmileFaceFinalRoute.");
        var spider = (PossessableBody)new SerializedObject(route).FindProperty("spider").objectReferenceValue;
        if (spider == null) throw new System.InvalidOperationException("SmileFaceFinalRoute has no Spider assigned.");
        int climbMask = new SerializedObject(spider.GetComponent<HexagonSurfaceMovement>())
            .FindProperty("surfaceMask").intValue;
        float silkRange = spider.GetComponent<SpiderSilkAbility>().Range;
        var spiderCircle = spider.GetComponent<CircleCollider2D>();
        float spiderRadius = spiderCircle != null
            ? spiderCircle.radius * Mathf.Max(Mathf.Abs(spider.transform.lossyScale.x), Mathf.Abs(spider.transform.lossyScale.y))
            : 0.35f;

        // Rebuild from scratch so repeated runs never stack objects.
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == RootName) Object.DestroyImmediate(root);
        Physics2D.SyncTransforms();

        // The probes skip pins (SpikeHazard) and the board is created after them,
        // so the root stays active and renderer bounds stay valid for seating pins.
        var challenge = new GameObject(RootName).transform;
        var gate = Object.FindAnyObjectByType<SmileDoorSpiderGate>();
        Transform returnWall = gate != null
            ? (Transform)new SerializedObject(gate).FindProperty("returnWall").objectReferenceValue : null;

        // 1. Find the shaft's two climbing faces at the board height.
        Vector2 boardProbe = new Vector2(ShaftProbeX, BoardY);
        RaycastHit2D left = Probe(boardProbe, Vector2.left, 4f, climbMask);
        RaycastHit2D right = Probe(boardProbe, Vector2.right, 4f, climbMask);
        if (left.collider == null || right.collider == null)
            throw new System.InvalidOperationException(
                $"Could not find both shaft walls from {boardProbe}. Adjust ShaftProbeX in Level02SmileSilkChallenge.");
        Debug.Log($"Smile silk shaft: left face {left.collider.name} at x={left.point.x:F2}, " +
                  $"right face {right.collider.name} at x={right.point.x:F2}.");

        // 2. Pins on every climbable face crossing the band, both directions.
        var pins = new GameObject("Silk Climb Pins").transform;
        pins.SetParent(challenge, false);
        // Pins on the return gate appear and rise with its wall, which is hidden until Spider is possessed.
        var gatePins = new GameObject("Silk Climb Pins On Return Wall", typeof(FollowTargetVisibility)).transform;
        gatePins.SetParent(challenge, false);
        if (returnWall != null)
        {
            var follow = new SerializedObject(gatePins.GetComponent<FollowTargetVisibility>());
            follow.FindProperty("target").objectReferenceValue = returnWall;
            follow.FindProperty("targetRestPosition").vector3Value = returnWall.position;
            follow.ApplyModifiedPropertiesWithoutUndo();
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePrefabPath);
        int pinCount = 0;
        // The return wall is hidden until Spider is possessed, but Spider can climb it afterwards.
        // Show it (and the gate blocker) during the scan so its face receives pins too.
        bool wallWasActive = returnWall != null && returnWall.gameObject.activeSelf;
        BoxCollider2D gateBlocker = gate != null ? gate.GetComponent<BoxCollider2D>() : null;
        bool blockerWasEnabled = gateBlocker != null && gateBlocker.enabled;
        if (returnWall != null) returnWall.gameObject.SetActive(true);
        if (gateBlocker != null) gateBlocker.enabled = true;
        Physics2D.SyncTransforms();
        // Left-facing faces are scanned only from the gate's shaft side, never the teeth-room side.
        float rightwardScanStart = ScanLeftX;
        if (gate != null && gate.TryGetComponent(out BoxCollider2D gateBox))
        {
            Vector2 gateCenter = gate.transform.TransformPoint(gateBox.offset);
            float gateHalfWidth = Mathf.Abs(gateBox.size.x * gate.transform.lossyScale.x) * 0.5f;
            rightwardScanStart = Mathf.Max(ScanLeftX, gateCenter.x + gateHalfWidth + 0.01f);
        }
        for (float y = PinBottom + PinSize * 0.5f; y < PinTop; y += PinSize)
        {
            RaycastHit2D wall = Probe(new Vector2(ShaftProbeX, y), Vector2.right, 4f, climbMask);
            if (wall.collider == null) continue;
            var faces = new List<RaycastHit2D> { wall };
            float scanRight = wall.point.x - 0.01f;
            faces.AddRange(ProbeAll(new Vector2(scanRight, y), Vector2.left, scanRight - ScanLeftX, climbMask));
            faces.AddRange(ProbeAll(new Vector2(rightwardScanStart, y), Vector2.right,
                scanRight - rightwardScanStart, climbMask));
            var placed = new List<Vector2>();
            foreach (RaycastHit2D face in faces)
            {
                // Overlapping colliders (gate blocker and its wall) share one face; pin it once.
                bool duplicate = false;
                foreach (Vector2 other in placed)
                    if (Vector2.Distance(other, face.point) < 0.08f) duplicate = true;
                if (duplicate) continue;
                placed.Add(face.point);
                bool onGate = (gate != null && face.collider.gameObject == gate.gameObject) ||
                    (returnWall != null && face.collider.transform.IsChildOf(returnWall));
                PlacePin(prefab, onGate ? gatePins : pins, face, player, pinCount);
                pinCount++;
            }
        }

        if (returnWall != null) returnWall.gameObject.SetActive(wallWasActive);
        if (gateBlocker != null) gateBlocker.enabled = blockerWasEnabled;
        Physics2D.SyncTransforms();

        // The gate's blocker is disabled in the saved scene (it switches on when Spider is possessed),
        // so edit-mode raycasts cannot see it. Pin its shaft-side face from the box geometry instead.
        if (gate != null && gatePins.childCount == 0 && gate.TryGetComponent(out BoxCollider2D blockerBox))
        {
            Vector2 boxCenter = gate.transform.TransformPoint(blockerBox.offset);
            Vector2 half = Vector2.Scale(blockerBox.size, gate.transform.lossyScale) * 0.5f;
            half = new Vector2(Mathf.Abs(half.x), Mathf.Abs(half.y));
            for (float y = PinBottom + PinSize * 0.5f; y < PinTop; y += PinSize)
            {
                if (y < boxCenter.y - half.y || y > boxCenter.y + half.y) continue;
                PlacePin(prefab, gatePins, new Vector2(boxCenter.x + half.x, y), Vector2.right, player, pinCount);
                pinCount++;
            }
        }

        if (gate != null && gatePins.childCount == 0)
            Debug.LogWarning("No pins were placed on the Spider return gate. If Spider can climb that gate, " +
                             "it can skip the board; check the gate's collider and layer.");

        // 3. The hanging board, centered in the shaft.
        float boardX = (left.point.x + right.point.x) * 0.5f;
        var board = new GameObject("Smile Silk Timetable Board", typeof(BoxCollider2D), typeof(SmileSilkAnchorBoard));
        board.layer = right.collider.gameObject.layer;
        board.transform.SetParent(challenge, false);
        board.transform.position = new Vector3(boardX, BoardY, 0f);
        board.GetComponent<BoxCollider2D>().size = BoardSize;
        Rect boardRect = new Rect(boardX - BoardSize.x / 2f, BoardY - BoardSize.y / 2f, BoardSize.x, BoardSize.y);

        // 4. Report reach and bypass distances, so the layout can be judged before playtesting.
        Vector2 belowRight = new Vector2(right.point.x - spiderRadius, PinBottom - spiderRadius - 0.1f);
        Vector2 aboveRight = new Vector2(right.point.x, PinTop + 0.3f);
        Vector2 aboveLeft = new Vector2(left.point.x, PinTop + 0.3f);
        Vector2 belowLeft = new Vector2(left.point.x + spiderRadius, PinBottom - spiderRadius - 0.1f);
        float toBoard = Vector2.Distance(belowRight, ClosestOnRect(boardRect, belowRight));
        Vector2 boardTop = new Vector2(boardRect.xMax - 0.2f, boardRect.yMax + spiderRadius);
        float boardToWall = Vector2.Distance(boardTop, aboveRight);
        float bypass = Mathf.Min(Vector2.Distance(belowRight, aboveLeft), Vector2.Distance(belowLeft, aboveRight));
        Debug.Log($"Smile silk challenge: {pinCount} pins, silk range {silkRange:F2}. " +
                  $"Wall to board {toBoard:F2}, board to upper wall {boardToWall:F2}, shortest bypass {bypass:F2}.");
        if (toBoard > silkRange * 0.9f || boardToWall > silkRange * 0.9f)
            Debug.LogWarning("The board is near the edge of silk range. Move BoardY or shrink the pin band.");
        if (bypass < silkRange + 0.3f)
            Debug.LogWarning("Spider may silk across the shaft without the board. Raise PinTop or lower PinBottom.");

        // 5. World-space lessons, reusing IntroSpider's silk hint.
        SilkHint(challenge, "Smile Silk Hint Board", player, belowRight,
            new Vector2(boardRect.xMax - 0.25f, boardRect.yMin), new Vector2(-1.4f, -0.4f), true,
            "Pins! Shoot silk at the board", "Pins! Hold the web button, drag to aim",
            Rect.MinMaxRect(left.point.x, PinBottom - 2.2f, right.point.x, PinBottom));
        SilkHint(challenge, "Smile Silk Hint Up", player, boardTop, aboveRight, new Vector2(-1.6f, 0.6f), false,
            "Swing past the pins", "Swing past the pins",
            Rect.MinMaxRect(boardRect.xMin - 0.2f, boardRect.yMax - 0.1f, boardRect.xMax + 0.2f, boardRect.yMax + 1f));
    }

    private static bool IsClimbFace(RaycastHit2D hit, int mask) =>
        hit.collider != null && !hit.collider.isTrigger && hit.distance > 0.0001f &&
        (mask & (1 << hit.collider.gameObject.layer)) != 0 &&
        hit.collider.GetComponentInParent<PossessableBody>() == null &&
        hit.collider.GetComponentInParent<SpikeHazard>() == null;

    private static RaycastHit2D Probe(Vector2 origin, Vector2 direction, float distance, int mask)
    {
        RaycastHit2D best = default;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, direction, distance))
            if (IsClimbFace(hit, mask) && (best.collider == null || hit.distance < best.distance)) best = hit;
        return best;
    }

    private static IEnumerable<RaycastHit2D> ProbeAll(Vector2 origin, Vector2 direction, float distance, int mask)
    {
        if (distance <= 0f) yield break;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, direction, distance))
            if (IsClimbFace(hit, mask)) yield return hit;
    }

    private static void PlacePin(GameObject prefab, Transform parent, RaycastHit2D face,
        PlayerPossessionController player, int index) => PlacePin(prefab, parent, face.point, face.normal, player, index);

    private static void PlacePin(GameObject prefab, Transform parent, Vector2 point, Vector2 normal,
        PlayerPossessionController player, int index)
    {
        var pin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        pin.name = index == 0 ? "Silk Climb Pin" : $"Silk Climb Pin ({index})";
        pin.transform.SetPositionAndRotation(point, Quaternion.FromToRotation(Vector3.up, normal));
        // Keep the prefab's proportions; make the visible height along the pin's axis PinSize.
        Vector3 scale = prefab.transform.localScale;
        var sprite = pin.GetComponent<SpriteRenderer>();
        float spriteHeight = sprite != null && sprite.sprite != null ? sprite.sprite.bounds.size.y : 1f;
        float currentHeight = Mathf.Max(0.0001f, Mathf.Abs(scale.y) * spriteHeight);
        pin.transform.localScale = scale * (PinSize / currentHeight);
        // Seat the pin's base on the wall face whatever the sprite pivot is.
        var renderer = pin.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Bounds b = renderer.bounds;
            float deepest = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 corner = new Vector2(i % 2 == 0 ? b.min.x : b.max.x, i < 2 ? b.min.y : b.max.y);
                deepest = Mathf.Min(deepest, Vector2.Dot(corner - point, normal));
            }
            // Bounds are axis-aligned, which is exact for the horizontal normals this scan produces.
            pin.transform.position += (Vector3)(normal * -deepest);
        }
        var serialized = new SerializedObject(pin.GetComponent<SpikeHazard>());
        serialized.FindProperty("playerController").objectReferenceValue = player;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Vector2 ClosestOnRect(Rect rect, Vector2 point) =>
        new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));

    private static void SilkHint(Transform parent, string name, PlayerPossessionController player, Vector2 launch,
        Vector2 anchor, Vector2 promptOffset, bool showControls, string message, string mobileMessage, Rect area)
    {
        var root = new GameObject(name, typeof(IntroSpiderSilkHint)).transform;
        root.SetParent(parent, false);
        Vector2 center = (launch + anchor) * 0.5f;
        root.position = center;
        var hint = new SerializedObject(root.GetComponent<IntroSpiderSilkHint>());
        hint.FindProperty("playerController").objectReferenceValue = player;
        hint.FindProperty("launchPoint").vector2Value = launch - center;
        hint.FindProperty("anchorPoint").vector2Value = anchor - center;
        hint.FindProperty("promptOffset").vector2Value = promptOffset;
        hint.FindProperty("showControls").boolValue = showControls;
        hint.FindProperty("message").stringValue = message;
        hint.FindProperty("mobileMessage").stringValue = mobileMessage;
        hint.FindProperty("activationAreaSize").vector2Value = area.size;
        hint.FindProperty("activationAreaOffset").vector2Value = area.center - center;
        hint.ApplyModifiedPropertiesWithoutUndo();
    }
}
