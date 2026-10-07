using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RollingPrototypeBuilder
{
    private const string ScenePath =
        "Assets/Scenes/RollingBallPrototype.unity";

    private static readonly Color SkyColor =
        new Color(0.64f, 0.84f, 0.94f, 1f);
    private static readonly Color TerrainColor =
        new Color(0.66f, 0.55f, 0.44f, 1f);
    private static readonly Color AccentGreen =
        new Color(0.35f, 0.67f, 0.37f, 1f);
    private static readonly Color BallPurple =
        new Color(0.62f, 0.50f, 0.86f, 1f);
    static RollingPrototypeBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("StarFormer/Rebuild Rolling Ball Prototype")]
    public static void BuildPrototype()
    {
        EnsureFolders();

        Sprite square = CreateSquareSprite();
        Sprite circle = CreateCircleSprite();
        Sprite star = CreateStarSprite();
        Sprite spike = CreateSpikeSprite();

        GameObject platformPrefab = CreatePlatformPrefab(square);
        GameObject spikePrefab = CreateSpikePrefab(spike);
        GameObject starPrefab = CreateStarPrefab(star);
        GameObject ballPrefab = CreateBallPrefab(circle, star);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();

        GameObject startGround = InstantiatePrefab(platformPrefab, "StartGround");
        startGround.transform.position = new Vector3(-9.2f, 3f, 0f);
        startGround.transform.localScale = new Vector3(5.8f, 0.35f, 1f);

        GameObject rollingSlope = InstantiatePrefab(platformPrefab, "RollingSlope");
        rollingSlope.transform.position = new Vector3(-4.4f, 2.12f, 0f);
        rollingSlope.transform.localScale = new Vector3(4.6f, 0.35f, 1f);
        rollingSlope.transform.rotation = Quaternion.Euler(0f, 0f, -25f);

        GameObject wallApproach = InstantiatePrefab(
            platformPrefab,
            "WallApproach"
        );
        wallApproach.transform.position = new Vector3(-1.3f, 0.98f, 0f);
        wallApproach.transform.localScale = new Vector3(2.4f, 0.35f, 1f);

        GameObject bounceWall = InstantiatePrefab(platformPrefab, "BounceWall");
        bounceWall.transform.position = new Vector3(0.05f, 2.1f, 0f);
        bounceWall.transform.localScale = new Vector3(0.35f, 2.25f, 1f);

        GameObject wallLanding = InstantiatePrefab(
            platformPrefab,
            "WallLanding"
        );
        wallLanding.transform.position = new Vector3(1.25f, 0.98f, 0f);
        wallLanding.transform.localScale = new Vector3(2.2f, 0.35f, 1f);

        GameObject spikeFloor = InstantiatePrefab(platformPrefab, "SpikeFloor");
        spikeFloor.transform.position = new Vector3(5.2f, 0.1f, 0f);
        spikeFloor.transform.localScale = new Vector3(5.7f, 0.35f, 1f);

        GameObject goalGround = InstantiatePrefab(platformPrefab, "GoalGround");
        goalGround.transform.position = new Vector3(10.1f, 0.98f, 0f);
        goalGround.transform.localScale = new Vector3(4.1f, 0.35f, 1f);

        float[] spikePositions =
        {
            3.2f,
            5.2f,
            7.2f
        };

        for (int i = 0; i < spikePositions.Length; i++)
        {
            GameObject spikeObject = InstantiatePrefab(
                spikePrefab,
                $"Spike{i + 1:00}"
            );
            spikeObject.transform.position = new Vector3(
                spikePositions[i],
                0.775f,
                -0.1f
            );
            spikeObject.transform.localScale = new Vector3(0.65f, 1f, 1f);
        }

        GameObject starObject = InstantiatePrefab(starPrefab, "StarPlayer");
        starObject.transform.position = new Vector3(-10.8f, 3.62f, 0f);

        GameObject ballObject = InstantiatePrefab(ballPrefab, "RollingBallBody");
        ballObject.transform.position = new Vector3(-6.85f, 3.68f, 0f);

        PlayerControlManager manager = CreateGameManager(
            starObject.GetComponent<ControllableBody>()
        );

        CreateGoal(square);
        CreateFallBoundary();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CapturePreview();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = ballObject;

        Debug.Log("Rolling ball prototype scene and prefabs built successfully.");
    }

    [MenuItem("StarFormer/Export Rolling Ball Prototype Package")]
    public static void ExportPrototypePackage()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(
            projectRoot,
            "StarFormer-RollingBall-Prototype.unitypackage"
        );

        string[] assetPaths =
        {
            "Assets/Art",
            "Assets/Physics",
            "Assets/Prefabs",
            "Assets/Scripts",
            ScenePath
        };

        AssetDatabase.ExportPackage(
            assetPaths,
            outputPath,
            ExportPackageOptions.Recurse
        );

        Debug.Log($"Exported rolling ball package to {outputPath}");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            return;
        }

        BuildPrototype();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Characters");
        EnsureFolder("Assets/Prefabs", "Environment");
        EnsureFolder("Assets/Prefabs", "Hazards");
        EnsureFolder("Assets/Prefabs", "Systems");
        EnsureFolder("Assets", "Physics");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Sprite CreateSquareSprite()
    {
        return CreateSprite("Square", 64, (x, y, size) => Color.white);
    }

    private static Sprite CreateCircleSprite()
    {
        return CreateSprite("Ball", 128, (x, y, size) =>
        {
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
            float distance = Vector2.Distance(point, center) / (size * 0.5f);

            if (distance > 0.98f)
            {
                return Color.clear;
            }

            if (distance > 0.82f)
            {
                return new Color(0.56f, 0.54f, 0.63f, 1f);
            }

            float highlight = Mathf.Clamp01(
                1f - Vector2.Distance(
                    point,
                    new Vector2(size * 0.35f, size * 0.68f)
                ) / (size * 0.7f)
            );

            return Color.Lerp(
                new Color(0.88f, 0.86f, 0.93f, 1f),
                Color.white,
                highlight * 0.28f
            );
        }, true);
    }

    private static Sprite CreateStarSprite()
    {
        Vector2[] points = new Vector2[10];

        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f - i * 36f) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? 0.46f : 0.21f;
            points[i] = new Vector2(
                0.5f + Mathf.Cos(angle) * radius,
                0.5f + Mathf.Sin(angle) * radius
            );
        }

        return CreateSprite("Star", 128, (x, y, size) =>
        {
            Vector2 point = new Vector2(
                (x + 0.5f) / size,
                (y + 0.5f) / size
            );

            return PointInPolygon(point, points)
                ? new Color(1f, 0.8f, 0.12f, 1f)
                : Color.clear;
        });
    }

    private static Sprite CreateSpikeSprite()
    {
        return CreateSprite("Spike", 128, (x, y, size) =>
        {
            float normalizedX = (x + 0.5f) / size;
            float normalizedY = (y + 0.5f) / size;
            float halfWidth = (1f - normalizedY) * 0.5f;
            bool inside =
                normalizedY >= 0.04f &&
                Mathf.Abs(normalizedX - 0.5f) <= halfWidth;

            if (!inside)
            {
                return Color.clear;
            }

            return TerrainColor;
        }, true);
    }

    private static Sprite CreateSprite(
        string name,
        int size,
        Func<int, int, int, Color> pixelFunction,
        bool regenerate = false)
    {
        string assetPath = $"Assets/Art/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (existing != null && !regenerate)
        {
            return existing;
        }

        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = pixelFunction(x, y, size);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string fullPath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            assetPath
        );

        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static GameObject CreatePlatformPrefab(Sprite sprite)
    {
        const string path = "Assets/Prefabs/Environment/Platform.prefab";
        AssetDatabase.DeleteAsset(path);

        GameObject root = new GameObject("Platform");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = TerrainColor;
        root.AddComponent<BoxCollider2D>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateSpikePrefab(Sprite sprite)
    {
        const string path = "Assets/Prefabs/Hazards/Spike.prefab";
        AssetDatabase.DeleteAsset(path);

        GameObject root = new GameObject("Spike");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 5;

        PolygonCollider2D collider = root.AddComponent<PolygonCollider2D>();
        collider.points = new[]
        {
            new Vector2(-0.48f, -0.48f),
            new Vector2(0.48f, -0.48f),
            new Vector2(0f, 0.46f)
        };
        collider.isTrigger = true;
        root.AddComponent<DeathHazard>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateStarPrefab(Sprite starSprite)
    {
        const string path = "Assets/Prefabs/Characters/Star.prefab";
        AssetDatabase.DeleteAsset(path);

        GameObject root = new GameObject("StarPlayer");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = starSprite;
        renderer.sortingOrder = 10;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.68f, 0.82f);

        StarController controller = root.AddComponent<StarController>();
        controller.ConfigureBody(false, true, null);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateBallPrefab(
        Sprite ballSprite,
        Sprite starSprite)
    {
        const string path = "Assets/Prefabs/Characters/RollingBall.prefab";
        AssetDatabase.DeleteAsset(path);

        GameObject root = new GameObject("RollingBallBody");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = ballSprite;
        renderer.color = BallPurple;
        renderer.sortingOrder = 10;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.mass = 1.2f;
        body.gravityScale = 2f;
        body.linearDamping = 0.15f;
        body.angularDamping = 0.1f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.48f;

        PhysicsMaterial2D material = CreateBallMaterial();
        collider.sharedMaterial = material;

        RollingBallController controller =
            root.AddComponent<RollingBallController>();

        GameObject marker = CreateMarker(root.transform, starSprite);
        controller.ConfigureBody(true, false, marker);
        controller.ConfigureRenderer(renderer);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static PhysicsMaterial2D CreateBallMaterial()
    {
        const string path = "Assets/Physics/BallSurface.physicsMaterial2D";
        PhysicsMaterial2D material =
            AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);

        if (material == null)
        {
            material = new PhysicsMaterial2D("BallSurface")
            {
                friction = 0.65f,
                bounciness = 0f
            };
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    private static GameObject CreateMarker(Transform parent, Sprite sprite)
    {
        GameObject marker = new GameObject("StarMark");
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = Vector3.one * 0.34f;

        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 20;
        marker.SetActive(false);
        return marker;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.backgroundColor = SkyColor;
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0f, 1.5f, -10f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CapturePreview()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        const int width = 1280;
        const int height = 720;
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        RenderTexture previous = RenderTexture.active;

        camera.targetTexture = renderTexture;
        camera.Render();
        RenderTexture.active = renderTexture;

        Texture2D preview = new Texture2D(
            width,
            height,
            TextureFormat.RGB24,
            false
        );

        preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        preview.Apply();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        File.WriteAllBytes(
            Path.Combine(projectRoot, "PrototypePreview.png"),
            preview.EncodeToPNG()
        );

        camera.targetTexture = null;
        RenderTexture.active = previous;
        renderTexture.Release();
        UnityEngine.Object.DestroyImmediate(renderTexture);
        UnityEngine.Object.DestroyImmediate(preview);
    }

    private static PlayerControlManager CreateGameManager(
        ControllableBody initialBody)
    {
        GameObject root = new GameObject("PlayerControl");
        PlayerControlManager manager = root.AddComponent<PlayerControlManager>();
        manager.ConfigureInitialBody(initialBody);

        PrototypeHUD hud = root.AddComponent<PrototypeHUD>();
        hud.Configure(manager);
        return manager;
    }

    private static void CreateGoal(Sprite square)
    {
        GameObject goal = new GameObject("GoalFlag");
        goal.transform.position = new Vector3(10.5f, 2.405f, 0f);

        BoxCollider2D collider = goal.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.2f, 2.5f);
        collider.isTrigger = true;
        goal.AddComponent<GoalZone>();

        CreateFlagPart(
            goal.transform,
            square,
            "Flag Pole",
            new Vector3(-0.7f, 0f, 0f),
            new Vector3(0.14f, 2.5f, 1f)
        );
        CreateFlagPart(
            goal.transform,
            square,
            "Flag Cloth",
            new Vector3(0.05f, 0.87f, -0.01f),
            new Vector3(1.5f, 0.76f, 1f)
        );
    }

    private static void CreateFlagPart(
        Transform parent,
        Sprite sprite,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = AccentGreen;
        renderer.sortingOrder = 2;
    }

    private static void CreateFallBoundary()
    {
        GameObject boundary = new GameObject("Fall Death Boundary");
        boundary.transform.position = new Vector3(0f, -5.5f, 0f);

        BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(40f, 2f);
        collider.isTrigger = true;
        boundary.AddComponent<DeathHazard>();
    }

    private static GameObject InstantiatePrefab(
        GameObject prefab,
        string objectName)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = objectName;
        return instance;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1;
             i < polygon.Length;
             j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            if ((a.y > point.y) == (b.y > point.y))
            {
                continue;
            }

            float crossingX =
                a.x + (point.y - a.y) * (b.x - a.x) / (b.y - a.y);

            if (point.x < crossingX)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
