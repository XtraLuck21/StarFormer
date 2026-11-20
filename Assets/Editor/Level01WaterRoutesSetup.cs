using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class Level01WaterRoutesSetup
{
    const string BubbleAsset = "Assets/Form/Level01WaterBubble.prefab";
    [MenuItem("StarFormer/Level01/Apply Water Routes")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        var box = GameObject.Find("BoxA_Start"); if (box != null) Object.DestroyImmediate(box);
        var oldHint = GameObject.Find("WaterCeilingRestartHint"); if (oldHint != null) Object.DestroyImmediate(oldHint);
        var oldRoot = GameObject.Find("Water Route Additions"); if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        var root = new GameObject("Water Route Additions").transform;
        // 1.30 units of clear height over the 1.50 floor for the 0.80-high Star.
        Resize("TankEntry_LowPassageWall", new Vector2(7.1f, 6.65f), new Vector2(.6f, 7.7f));
        // Lower passage is unchanged; an upper opening is now available to the ball.
        Resize("TankRightWall_UnderwaterPassage", new Vector2(16.55f, 4.95f), new Vector2(.5f, 5.1f));
        Resize("Water_DeepBasin", new Vector2(12, 2.525f), new Vector2(4, 3.45f));
        var water = GameObject.Find("Water_LeftShelf");
        var topWater = Object.Instantiate(water, root); topWater.name = "Water_DeckApproach";
        foreach (Transform child in topWater.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        Resize(topWater, new Vector2(11.1f, 4.625f), new Vector2(2.2f, .75f));
        // A flush dry deck interrupts only the top of the water; swimming stays open below.
        var deck = Object.Instantiate(GameObject.Find("TankEntry_Sill"), root); deck.name = "WaterRoute_BounceDeck";
        foreach (Transform child in deck.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        Resize(deck, new Vector2(13.1f, 4.65f), new Vector2(1.8f, .7f));
        deck.GetComponent<BoxCollider2D>().enabled = true;
        // Landing must clear the contact-lock margin so a popped Bubble can be re-entered.
        var bubble = GameObject.Find("BubbleU1_WaterEntry"); bubble.transform.position = new Vector3(9.1f, 3.2f, 0);
        Renewable(bubble);
        var backup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Form/IntroBubbleBackup.prefab"));
        backup.name = "WaterRoute_BackupBubble"; backup.transform.SetParent(root, true);
        backup.transform.position = new Vector3(15, 1.9f, 0); Renewable(backup);
        var ball = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Form/BouncyBall.prefab"));
        ball.name = "WaterRoute_FloatingBall"; ball.transform.SetParent(root, true);
        ball.transform.position = new Vector3(10.8f, 1.32f, 0);
        var ability = new SerializedObject(ball.GetComponent<BouncyBallAbility>());
        ability.FindProperty("defaultBounceHeight").floatValue = 5.4f;
        ability.FindProperty("defaultSecondsToApex").floatValue = 1.15f;
        ability.ApplyModifiedPropertiesWithoutUndo();
        var controller = Object.FindFirstObjectByType<PlayerPossessionController>();
        var hints = root.gameObject.AddComponent<Level01WaterRouteHints>();
        hints.player = controller; hints.view = Camera.main.GetComponent<AdvancedLevelCamera2D>();
        hints.lesson = Label(root, "Water route lesson", new Vector2(11.5f, 7.15f), new Vector2(7.7f, 1.8f), 2.5f);
        hints.recovery = Label(root, "Water route recovery", new Vector2(11.5f, 9.25f), new Vector2(7.8f, 1.6f), 2.2f);
        // Keep both routes visible, including the launch deck and top of the right divider.
        var camera = new SerializedObject(hints.view);
        camera.FindProperty("waterTransfer.center").vector2Value = new Vector2(13.8f, 5.65f);
        camera.FindProperty("waterTransfer.followRange").vector2Value = new Vector2(.5f, 0);
        camera.FindProperty("waterTransfer.size").floatValue = 5.65f;
        camera.ApplyModifiedPropertiesWithoutUndo();
        Physics2D.SyncTransforms();
        EditorSceneManager.SaveScene(scene);
        Level01WoodlandSetup.Apply();
        PolishFeedback();
        Debug.Log("LEVEL01 WATER ROUTES APPLIED: Star entry, floating ball, dry deck, upper opening, renewable bubbles and contextual hints.");
    }
    [MenuItem("StarFormer/Level01/Polish Water Feedback")]
    public static void PolishFeedback()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        var entry = GameObject.Find("BubbleU1_WaterEntry");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BubbleAsset);
        if (prefab == null)
        {
            // Capture the existing blue fill, dark outline and glow in one shared asset.
            var template = Object.Instantiate(entry);
            if (PrefabUtility.IsPartOfPrefabInstance(template))
                PrefabUtility.UnpackPrefabInstance(template, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var supply = template.GetComponent<BubbleRespawnOnPop>();
            if (supply != null) Object.DestroyImmediate(supply);
            template.name = "Level01WaterBubble";
            template.transform.position = Vector3.zero;
            prefab = PrefabUtility.SaveAsPrefabAsset(template, BubbleAsset);
            Object.DestroyImmediate(template);
        }
        ReplaceBubble(entry, prefab);
        ReplaceBubble(GameObject.Find("WaterRoute_BackupBubble"), prefab);
        var ball = new SerializedObject(GameObject.Find("WaterRoute_FloatingBall").GetComponent<BouncyBallAbility>());
        ball.FindProperty("waterRiseSpeed").floatValue = 2f;
        ball.ApplyModifiedPropertiesWithoutUndo();
        var hints = Object.FindFirstObjectByType<Level01WaterRouteHints>();
        hints.lesson.enabled = hints.recovery.enabled = false;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("WATER FEEDBACK: shared Bubble appearance, gradual flotation and compact contextual HUD saved.");
    }

    static void ReplaceBubble(GameObject previous, GameObject prefab)
    {
        Vector3 pos = previous.transform.position;
        Transform parent = previous.transform.parent;
        string name = previous.name;
        Object.DestroyImmediate(previous);
        var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        replacement.name = name; replacement.transform.SetParent(parent, true);
        replacement.transform.position = pos;
        var supply = replacement.AddComponent<BubbleRespawnOnPop>();
        var so = new SerializedObject(supply);
        so.FindProperty("bubblePrefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Renewable(GameObject bubble)
    {
        var supply = bubble.GetComponent<BubbleRespawnOnPop>() ?? bubble.AddComponent<BubbleRespawnOnPop>();
        var so = new SerializedObject(supply);
        so.FindProperty("bubblePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Form/IntroBubbleBackup.prefab");
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Resize(string name, Vector2 pos, Vector2 size) => Resize(GameObject.Find(name), pos, size);
    static void Resize(GameObject obj, Vector2 pos, Vector2 size)
    {
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var col = obj.GetComponent<BoxCollider2D>(); col.offset = Vector2.zero; col.size = Vector2.one;
    }
    static TMP_Text Label(Transform parent, string name, Vector2 pos, Vector2 size, float font)
    {
        var obj = new GameObject(name, typeof(TextMeshPro)); obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(pos.x, pos.y, -.1f);
        var text = obj.GetComponent<TextMeshPro>();
        text.font = GameObject.Find("LevelOneSubtitle").GetComponent<TMP_Text>().font;
        text.fontSize = font; text.rectTransform.sizeDelta = size;
        text.alignment = TextAlignmentOptions.Center; text.color = new Color32(7, 15, 12, 255);
        text.GetComponent<MeshRenderer>().sortingOrder = 30; text.text = "";
        return text;
    }
}
