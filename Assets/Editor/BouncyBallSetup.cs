using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BouncyBallSetup
{
    private const string ScenePath = "Assets/Scenes/Level01.unity";
    private const string ArtPath = "Assets/Art/BouncyBall.png";
    private const string MaterialPath =
        "Assets/Form/BouncyBallSurface.physicsMaterial2D";
    private const string PrefabPath = "Assets/Form/BouncyBall.prefab";

    [MenuItem("StarFormer/Build Bouncy Ball Feature")]
    public static void BuildFeature()
    {
        CreateBallArt();
        PhysicsMaterial2D surfaceMaterial = CreateSurfaceMaterial();
        GameObject prefab = CreatePrefab(surfaceMaterial);
        PlaceInLevel(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Built BouncyBall prefab and placed lower/upper instances in Level01."
        );
    }

    private static void CreateBallArt()
    {
        const int resolution = 128;
        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 fill = new Color32(154, 120, 220, 255);
        Color32 outline = new Color32(74, 49, 126, 255);

        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        Color32[] pixels = new Color32[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = (x + 0.5f) / resolution - 0.5f;
                float normalizedY = (y + 0.5f) / resolution - 0.5f;
                float distance = Mathf.Sqrt(
                    normalizedX * normalizedX +
                    normalizedY * normalizedY
                );

                Color32 color = transparent;

                if (distance <= 0.48f)
                {
                    color = distance >= 0.42f ? outline : fill;
                }

                pixels[y * resolution + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        File.WriteAllBytes(ArtPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(ArtPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer =
            (TextureImporter)AssetImporter.GetAtPath(ArtPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = resolution;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static PhysicsMaterial2D CreateSurfaceMaterial()
    {
        PhysicsMaterial2D material =
            AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);

        if (material == null)
        {
            material = new PhysicsMaterial2D("BouncyBallSurface");
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.friction = 0.4f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreatePrefab(PhysicsMaterial2D surfaceMaterial)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        GameObject root = new GameObject("BouncyBall");

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.48f;
        collider.sharedMaterial = surfaceMaterial;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.mass = 1.2f;
        body.gravityScale = 3f;
        body.linearDamping = 0f;
        body.angularDamping = 0.05f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        GameObject starMark = new GameObject("StarMark");
        starMark.transform.SetParent(root.transform, false);
        starMark.transform.localScale = Vector3.one * 0.55f;
        starMark.AddComponent<SpriteRenderer>();
        starMark.AddComponent<StarVisual>();
        starMark.SetActive(false);

        PossessableBody possessable = root.AddComponent<PossessableBody>();
        SerializedObject serializedBody = new SerializedObject(possessable);
        serializedBody.FindProperty("form").enumValueIndex =
            (int)BodyForm.BouncyBall;
        serializedBody.FindProperty("controlVisual").objectReferenceValue =
            starMark;
        serializedBody.FindProperty("moveSpeed").floatValue = 7.5f;
        serializedBody.FindProperty("jumpSpeed").floatValue = 16.9f;
        serializedBody.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<BodyWaterPhysics>();

        FormGlowHalo halo = root.AddComponent<FormGlowHalo>();
        SerializedObject serializedGlow = new SerializedObject(halo);
        serializedGlow.FindProperty("haloColor").colorValue =
            new Color(154f / 255f, 120f / 255f, 220f / 255f, 1f);
        serializedGlow.FindProperty("shape").enumValueIndex =
            (int)FormHaloShape.Auto;
        serializedGlow.FindProperty("gap").floatValue = 0.05f;
        serializedGlow.FindProperty("glowThickness").floatValue = 0.6f;
        serializedGlow.FindProperty("pulseSpeed").floatValue = 2.5f;
        serializedGlow.FindProperty("minAlpha").floatValue = 0.2f;
        serializedGlow.FindProperty("maxAlpha").floatValue = 0.7f;
        serializedGlow.ApplyModifiedPropertiesWithoutUndo();

        BouncyBallAbility ability = root.AddComponent<BouncyBallAbility>();
        SerializedObject serializedAbility = new SerializedObject(ability);
        serializedAbility.FindProperty("defaultBounceHeight").floatValue = 20.25f;
        serializedAbility.FindProperty("defaultSecondsToApex").floatValue = 2.4f;
        serializedAbility.FindProperty("compressionTempoExponent").floatValue = 1.1f;
        serializedAbility.FindProperty("minimumSecondsToApex").floatValue = 0.18f;
        serializedAbility.FindProperty("ceilingStrikeDistance").floatValue = 0.15f;
        serializedAbility.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void PlaceInLevel(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(
            ScenePath,
            OpenSceneMode.Single
        );

        GameObject oldGroup = GameObject.Find("BouncyBalls");
        if (oldGroup != null)
        {
            Object.DestroyImmediate(oldGroup);
        }

        GameObject group = new GameObject("BouncyBalls");

        CreateInstance(
            prefab,
            group.transform,
            "BouncyBall_Lower",
            new Vector3(20.75f, 0.55f, 0f)
        );

        CreateInstance(
            prefab,
            group.transform,
            "BouncyBall_Upper",
            new Vector3(39.5f, 9.55f, 0f)
        );

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = group;
    }

    private static void CreateInstance(
        GameObject prefab,
        Transform parent,
        string instanceName,
        Vector3 worldPosition)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = instanceName;
        instance.transform.SetParent(parent, true);
        instance.transform.position = worldPosition;
    }
}
