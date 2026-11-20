using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Rebuilds IntroSpider's first room around Spider silk. Safe to run repeatedly.
//
// Route: climb the start block -> silk across the cake pit to the pillar -> silk up to the
// floating island -> silk to the tall wall -> climb over the roof into the Bubble room (unchanged).
public static class IntroSpiderSilkLayout
{
    private const string ScenePath = "Assets/Scenes/IntroSpider.unity";
    private const string SpikePrefabPath = "Assets/Form/SpikeHazard.prefab";

    // World-space layout (the floor top is read from the scene).
    private static readonly Vector2 BlockCenterX = new Vector2(-4.48f, 1.04f);   // x, width
    private const float BlockHeight = 2.4f;
    private const float PillarLeft = -1.9f, PillarWidth = 1f, PillarHeight = 3.5f;
    private static readonly Rect Island = new Rect(-0.6f, 0.8f, 1.8f, 0.5f);      // x, y, width, height
    private static readonly Vector2 SpikeScale = new Vector2(0.2529613f, 0.242151f);

    [MenuItem("StarFormer/Build IntroSpider Silk Layout")]
    public static void BuildFromMenu()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("IntroSpider silk layout rebuilt.");
    }

    public static void RunBatch()
    {
        try
        {
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
        Transform structures = Find("Structures");
        Collider2D floor = Find("Platform", structures).GetComponent<Collider2D>();
        Collider2D tallWall = Find("Platform (1)", structures).GetComponent<Collider2D>();
        Physics2D.SyncTransforms();
        float floorTop = floor.bounds.max.y;
        float wallLeft = tallWall.bounds.min.x;

        // Retire the slanted climb and the ceiling cake ledge; silk replaces them.
        Find("Grounds").gameObject.SetActive(false);
        Find("Platform (4)", structures).gameObject.SetActive(false);
        Find("Platform (5)", structures).gameObject.SetActive(false);
        Find("SpiderUpReminderHint").gameObject.SetActive(false);

        // 1. Start block: climb over it, then look down at the "cake".
        Transform block = Find("Platform (3)", structures);
        SetBox(block, new Vector2(BlockCenterX.x, floorTop + BlockHeight * 0.5f), new Vector2(BlockCenterX.y, BlockHeight));
        float blockRight = BlockCenterX.x + BlockCenterX.y * 0.5f;

        // 2. Pillar across the cake pit, 3. floating island, both cloned from the block's look.
        Transform pillar = FindOrClone("SilkPillar", block, structures);
        SetBox(pillar, new Vector2(PillarLeft + PillarWidth * 0.5f, floorTop + PillarHeight * 0.5f),
            new Vector2(PillarWidth, PillarHeight));
        Transform island = FindOrClone("SilkIsland", block, structures);
        SetBox(island, Island.center, Island.size);

        // Cake-disguised spikes fill the pit between the block and the pillar.
        Transform cakeRow = Find("SpikeTrap");
        cakeRow.position = Vector3.zero;
        cakeRow.rotation = Quaternion.identity;
        var cake = new SerializedObject(cakeRow.GetComponent<IntroSpiderCake>());
        cake.FindProperty("sitsOnFloor").boolValue = true;
        cake.FindProperty("cherryCount").intValue = 4;
        cake.ApplyModifiedPropertiesWithoutUndo();
        FillSpikes(cakeRow, blockRight, PillarLeft, floorTop, player, "SpikeHazard");

        // Plain, visible spikes under the island: falling here is the only way to fail the last swings.
        Transform spikeFloor = Find("SilkSpikeFloor", null, false);
        if (spikeFloor == null) spikeFloor = new GameObject("SilkSpikeFloor").transform;
        spikeFloor.position = Vector3.zero;
        FillSpikes(spikeFloor, PillarLeft + PillarWidth, wallLeft, floorTop, player, "SilkFloorSpike");

        // Existing hints follow the new geometry.
        float blockTop = floorTop + BlockHeight;
        Transform warning = Find("SpiderSpikeWarningHint");
        warning.position = new Vector3((blockRight + PillarLeft) * 0.5f, floorTop + 0.85f, 0f);
        SetHint(warning, "activationAreaSize", new Vector2(3.2f, 3.4f));
        SetHint(warning, "activationAreaOffset", new Vector2(-1.2f, 1.2f));

        Transform intro = Find("SpiderIntroductionHint");
        var introHint = new SerializedObject(intro.GetComponent<BubbleMovementHint>());
        introHint.FindProperty("spiderClimbSurface").objectReferenceValue = block;
        var arrows = introHint.FindProperty("spiderWallArrowPositions");
        arrows.arraySize = 1;
        arrows.GetArrayElementAtIndex(0).vector2Value = new Vector2(BlockCenterX.x - BlockCenterX.y * 0.5f - 0.35f,
            floorTop + BlockHeight * 0.55f);
        introHint.ApplyModifiedPropertiesWithoutUndo();

        // Silk lessons: launch where the spider stands, anchor on the climbable target.
        const float spiderLift = 0.52f;
        Vector2 blockLaunch = new Vector2(blockRight - 0.3f, blockTop + spiderLift);
        SilkHint("SilkHintCross", player, blockLaunch, new Vector2(PillarLeft, blockLaunch.y),
            new Vector2(0f, 1.25f), true, "Hold to aim, release to shoot silk",
            "Hold the web button, drag to aim", new Rect(blockRight - 1.3f, blockTop - 0.2f, 1.6f, 1.4f));

        float pillarTop = floorTop + PillarHeight;
        Vector2 pillarLaunch = new Vector2(PillarLeft + PillarWidth * 0.5f, pillarTop + spiderLift);
        Vector2 islandAnchor = new Vector2(Island.center.x, Island.yMin);
        SilkHint("SilkHintUp", player, pillarLaunch, islandAnchor, new Vector2(1.7f, -0.6f), false,
            "Shoot up, hang on", "Shoot up, hang on",
            new Rect(PillarLeft - 0.2f, pillarTop - 0.1f, PillarWidth + 0.4f, 1.2f));

        Vector2 islandLaunch = new Vector2(Island.xMax - 0.2f, Island.yMax + spiderLift);
        SilkHint("SilkHintWall", player, islandLaunch, new Vector2(wallLeft, islandLaunch.y + 0.8f),
            new Vector2(-1.0f, 0.9f), false, "Swing to the wall", "Swing to the wall",
            new Rect(Island.xMin - 0.3f, Island.yMax - 0.05f, Island.width + 0.6f, 0.9f));
    }

    private static void SilkHint(string name, PlayerPossessionController player, Vector2 launch, Vector2 anchor,
        Vector2 promptOffset, bool showControls, string message, string mobileMessage, Rect area)
    {
        Transform root = Find(name, null, false);
        if (root == null) root = new GameObject(name, typeof(IntroSpiderSilkHint)).transform;
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

    private static void FillSpikes(Transform parent, float left, float right, float floorTop,
        PlayerPossessionController player, string baseName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePrefabPath);
        int count = Mathf.Max(1, Mathf.FloorToInt((right - left) / SpikeScale.x));
        float step = (right - left) / count;
        var spikes = new List<SpikeHazard>(parent.GetComponentsInChildren<SpikeHazard>(true));
        while (spikes.Count > count)
        {
            Object.DestroyImmediate(spikes[spikes.Count - 1].gameObject);
            spikes.RemoveAt(spikes.Count - 1);
        }
        while (spikes.Count < count)
            spikes.Add(((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).GetComponent<SpikeHazard>());

        for (int i = 0; i < count; i++)
        {
            Transform spike = spikes[i].transform;
            spike.name = i == 0 ? baseName : $"{baseName} ({i})";
            spike.rotation = Quaternion.identity;
            spike.localScale = new Vector3(SpikeScale.x, SpikeScale.y, 1f);
            spike.position = new Vector3(left + step * (i + 0.5f), floorTop, 0f);
            // Sit the sprite's bottom edge exactly on the floor whatever its pivot.
            var renderer = spike.GetComponent<SpriteRenderer>();
            if (renderer != null) spike.position += Vector3.up * (floorTop - renderer.bounds.min.y);
            var serialized = new SerializedObject(spikes[i]);
            serialized.FindProperty("playerController").objectReferenceValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetHint(Transform hint, string field, Vector2 value)
    {
        var serialized = new SerializedObject(hint.GetComponent<BubbleMovementHint>());
        serialized.FindProperty(field).vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBox(Transform box, Vector2 center, Vector2 size)
    {
        box.gameObject.SetActive(true);
        box.position = new Vector3(center.x, center.y, box.position.z);
        box.rotation = Quaternion.identity;
        Vector3 parentScale = box.parent != null ? box.parent.lossyScale : Vector3.one;
        box.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, 1f);
    }

    private static Transform FindOrClone(string name, Transform source, Transform parent)
    {
        Transform existing = Find(name, parent, false);
        if (existing != null) return existing;
        var clone = Object.Instantiate(source.gameObject, parent);
        clone.name = name;
        return clone.transform;
    }

    private static Transform Find(string name, Transform parent = null, bool required = true)
    {
        if (parent != null)
        {
            foreach (Transform child in parent)
                if (child.name == name) return child;
        }
        else
        {
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) return root.transform;
        }
        if (required) throw new System.InvalidOperationException($"IntroSpider is missing '{name}'.");
        return null;
    }
}
