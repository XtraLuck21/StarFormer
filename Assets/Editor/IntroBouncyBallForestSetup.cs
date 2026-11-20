using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit, repeatable art authoring for this scene only. Adds no runtime scripts or colliders.
public static class IntroBouncyBallForestSetup
{
    const string Folder = "Assets/Art/IntroBouncyBallForest";
    const string RootName = "IntroBouncyBall Forest Art";
    static readonly string[] TerrainNames = { "Ground", "LeftBoundary", "RIghtBoundary", "ExitCeiling",
        "HighBounceLedge", "WaterExitBank", "WaterCanopy", "LowBounceCeiling", "LowBounceRoofFill", "ExitPlatform" };

    [MenuItem("StarFormer/Art/Apply IntroBouncyBall Woodland")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/IntroBouncyBall.unity")
            throw new InvalidOperationException("Open IntroBouncyBall outside Play Mode before applying its art.");
        if (!File.Exists(Folder + "/ForestBackground.png"))
            throw new FileNotFoundException("The approved forest backdrop is missing.");

        var solids = new List<BoxCollider2D>();
        foreach (string name in TerrainNames)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.TryGetComponent(out BoxCollider2D col))
                throw new InvalidOperationException("Missing expected terrain: " + name);
            solids.Add(col);
        }
        var water = GameObject.Find("WaterArea").GetComponent<BoxCollider2D>();
        Sprite soil = Soil();
        Sprite grass = Grass();
        Sprite pool = Water();
        Sprite backdrop = Import("ForestBackground.png", 128, false);
        var material = solids[0].GetComponent<SpriteRenderer>().sharedMaterial;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply IntroBouncyBall woodland art");
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == RootName) Undo.DestroyObjectImmediate(go);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create woodland art");

        Camera camera = Camera.main;
        float height = camera.orthographicSize * 2;
        float width = height * 16f / 9f;
        var bg = Add(root.transform, "Forest backdrop", backdrop,
            new Vector2(camera.transform.position.x, camera.transform.position.y), new Vector2(width, height), -100, false, material);
        // The backdrop covers the original fixed 16:9 game camera; no camera movement is added.
        bg.color = Color.white;

        foreach (var col in solids)
        {
            Bounds b = col.bounds;
            var original = col.GetComponent<SpriteRenderer>();
            Undo.RecordObject(original, "Set soil base color");
            original.color = Hex("44473B");
            // Identity transforms keep texture scale consistent regardless of the original block's scale.
            Add(root.transform, col.name + " soil", soil, b.center, b.size, 0, true, material);
            foreach (var span in ExposedTops(b, col, solids, water.bounds))
                Add(root.transform, col.name + " grass", grass,
                    new Vector2((span.x + span.y) * .5f, b.max.y - .125f),
                    new Vector2(span.y - span.x, .25f), 0, true, material);
        }
        Add(root.transform, "Pool depth bands", pool, water.bounds.center, water.bounds.size, 0, false, material);
        var edge = GameObject.Find("WaterSurfaceEdge").GetComponent<SpriteRenderer>();
        Undo.RecordObject(edge, "Set blue waterline");
        edge.color = Hex("559DAF");
        edge.sortingOrder = 2;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("WOODLAND ART: IntroBouncyBall saved. Original collider, transform, form and route settings retained.");
    }

    static SpriteRenderer Add(Transform root, string name, Sprite sprite, Vector2 pos, Vector2 size,
        int order, bool tiled, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(pos.x, pos.y, name.EndsWith("grass") ? -.02f : -.01f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = material;
        sr.sortingOrder = order;
        if (tiled)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
        }
        else go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        return sr;
    }

    static List<Vector2> ExposedTops(Bounds b, BoxCollider2D self, List<BoxCollider2D> all, Bounds water)
    {
        var spans = new List<Vector2> { new Vector2(b.min.x, b.max.x) };
        foreach (var other in all)
        {
            if (other == self) continue;
            Bounds o = other.bounds;
            if (o.min.y <= b.max.y + .005f && o.max.y > b.max.y + .005f)
                Subtract(spans, o.min.x, o.max.x);
        }
        // No grass across the bottom of the pool; the water boundary remains literal.
        if (water.min.y <= b.max.y + .005f && water.max.y > b.max.y + .005f)
            Subtract(spans, water.min.x, water.max.x);
        return spans;
    }
    static void Subtract(List<Vector2> spans, float lo, float hi)
    {
        for (int i = spans.Count - 1; i >= 0; i--)
        {
            Vector2 s = spans[i];
            if (hi <= s.x || lo >= s.y) continue;
            spans.RemoveAt(i);
            if (lo - s.x > .005f) spans.Add(new Vector2(s.x, Mathf.Min(lo, s.y)));
            if (s.y - hi > .005f) spans.Add(new Vector2(Mathf.Max(hi, s.x), s.y));
        }
    }

    static Sprite Soil()
    {
        const int n = 512;
        var pixels = new Color32[n * n];
        var stoneCenters = new[] { new Vector2(.16f,.2f), new Vector2(.66f,.35f), new Vector2(.4f,.76f), new Vector2(.84f,.87f) };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float u = x / (float)(n - 1), v = y / (float)(n - 1);
            // Broad periodic strata, not grain/noise. Opposite texture edges match.
            float strata = Mathf.Sin((v + .055f * Mathf.Sin(u * Mathf.PI * 2)) * Mathf.PI * 4);
            Color c = strata > .4f ? Hex("484A3E") : strata < -.5f ? Hex("404337") : Hex("44473B");
            float root = .50f + .10f * Mathf.Sin(u * Mathf.PI * 2);
            if (Mathf.Abs(v - root) < .008f) c = Hex("55533E");
            foreach (Vector2 center in stoneCenters)
            {
                Vector2 d = new Vector2((u-center.x)/.061f, (v-center.y)/.042f);
                // Squared-off small stones, safely away from tile seams.
                if (Mathf.Abs(d.x) < 1 && Mathf.Abs(d.y) < 1 && Mathf.Abs(d.x + d.y * .5f) < 1.15f
                    && Mathf.Abs(d.x - d.y * .7f) < 1.15f) c = Hex("5A5B4D");
            }
            pixels[y*n+x] = c;
        }
        return Save("SoilTile.png", n, n, pixels, 128, false);
    }
    static Sprite Grass()
    {
        const int w=256,h=64;
        var pixels = new Color32[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float wave = .5f + .5f * Mathf.Cos(x / 32f * Mathf.PI * 2);
            float lower = 25 + wave * 9;
            Color c = Color.clear;
            if(y >= lower) c = Hex("75933F");
            if(y >= 48) c = Hex("88A449");
            if(y >= 59) c = Hex("96AC53");
            pixels[y*w+x] = c;
        }
        // Flat top, scalloped underside: every visible pixel is below the collision top.
        return Save("GrassEdge.png",w,h,pixels,256,true);
    }
    static Sprite Water()
    {
        const int n=256;
        var pixels=new Color32[n*n];
        for(int y=0;y<n;y++) for(int x=0;x<n;x++)
        {
            float u=x/(float)(n-1),v=y/(float)(n-1);
            Color c=Hex("287FA8");
            if(v < .38f+.018f*Mathf.Sin(u*Mathf.PI*4)) c=Hex("26789E");
            if(v < .15f+.014f*Mathf.Sin(u*Mathf.PI*4)) c=Hex("246F93");
            pixels[y*n+x]=c;
        }
        return Save("PoolDepth.png",n,n,pixels,256,false);
    }
    static Sprite Save(string name,int w,int h,Color32[] pixels,float ppu,bool alpha)
    {
        var t=new Texture2D(w,h,TextureFormat.RGBA32,false);
        t.SetPixels32(pixels);t.Apply();
        File.WriteAllBytes(Folder+"/"+name,t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(Folder+"/"+name,ImportAssetOptions.ForceUpdate);
        return Import(name,ppu,alpha);
    }
    static Sprite Import(string name,float ppu,bool alpha)
    {
        string path=Folder+"/"+name;
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=ppu;
        importer.spritePivot=Vector2.one*.5f;
        var settings=new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape=false;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency=alpha;
        importer.mipmapEnabled=false;
        importer.isReadable=false;
        importer.filterMode=FilterMode.Bilinear;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out Color c);return c; }
}
