using System.Collections.Generic;
using UnityEngine;

/// <summary>The desk sharpener feeds four painted wooden parts into the actual riding platform.</summary>
internal sealed class SeesawLiftAssembly
{
    private static readonly Color Ink = new Color32(57, 65, 65, 255);
    private static readonly Color Timber = new Color32(213, 166, 103, 255);
    private static readonly Color Metal = new Color32(207, 208, 184, 255);
    private readonly SeesawGear2D[] gears;
    private readonly Transform[] parts = new Transform[4];
    private readonly Transform[] slots = new Transform[4];
    private readonly Vector2[] feeds = new Vector2[4];
    private readonly Vector2[] seats = new Vector2[4];
    private readonly float[] progress = new float[4];
    private readonly MeshRenderer[] renderers = new MeshRenderer[4];
    private readonly MaterialPropertyBlock tint = new MaterialPropertyBlock();
    private readonly Color[][] solidColors = new Color[4][];
    private readonly Mesh[] partMeshes = new Mesh[4];
    private int active = -1;
    private float cueAge, seatedAge;
    private static readonly Color Background = new Color(.78f, .83f, .86f, .62f);
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly Material material;
    private readonly int layer;
    private readonly AudioSource latch;
    private readonly AudioClip click;

    public SeesawLiftAssembly(Transform platform, SeesawGear2D[] gears, Vector2 size, int layer)
    {
        this.gears = gears;
        this.layer = layer;
        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = Texture2D.whiteTexture };
        Vector2 quarter = new Vector2(size.x * .5f, .94f);
        var track = new SchoolSceneDrawing();
        string[] names = { "Blue lower frame", "Green lower frame", "Yellow upper frame", "Purple upper frame" };
        for (int i = 0; i < parts.Length; i++)
        {
            gears[i].WaitingForAssemblyTurn = true;
            Color paint = gears[i].GetComponent<SpriteRenderer>().color;
            Vector2 hub = platform.InverseTransformPoint(gears[i].transform.position);
            feeds[i] = hub + Vector2.right * 1.35f;
            seats[i] = new Vector2((i % 2 - .5f) * quarter.x, (i / 2 + .5f) * quarter.y - size.y * .5f);
            var outline = new SchoolSceneDrawing();
            var art = new SchoolSceneDrawing();
            DrawFrameCorner(art, outline, quarter, size.y, i, paint);
            slots[i] = Build(outline, names[i] + " chalk outline", platform, -5);
            slots[i].localPosition = seats[i];
            parts[i] = Build(art, names[i], platform, -3);
            renderers[i] = parts[i].GetComponent<MeshRenderer>();
            partMeshes[i] = parts[i].GetComponent<MeshFilter>().sharedMesh;
            var colors = partMeshes[i].colors;
            solidColors[i] = (Color[])colors.Clone();
            for (int j = 0; j < colors.Length; j++)
            {
                float grey = colors[j].grayscale;
                colors[j] = Color.Lerp(colors[j], new Color(grey, grey, grey), .48f);
            }
            partMeshes[i].colors = colors;
            SetTint(renderers[i], Background);
            parts[i].gameObject.SetActive(false);
        }
        for (int side = -1; side <= 1; side += 2)
            track.Rect(side * (size.x * .5f + .12f), .4f, .07f, 2.1f, Metal);
        Transform guides = Build(track, "Desk lift guide rails", platform, -10);
        SetTint(guides.GetComponent<MeshRenderer>(), new Color(.85f, .85f, .85f, .32f));
        guides.SetParent(platform.parent, true);
        latch = platform.gameObject.AddComponent<AudioSource>();
        latch.playOnAwake = false;
        click = MakeLatchClick();
    }

    private Transform Build(SchoolSceneDrawing art, string name, Transform parent, int order)
    {
        GameObject item = art.Build(name, parent, material, order, out Mesh mesh);
        item.GetComponent<MeshRenderer>().sortingLayerID = layer;
        meshes.Add(mesh);
        return item.transform;
    }

    private static void Bolt(SchoolSceneDrawing art, float x, float y)
    {
        art.Circle(new Vector2(x, y), .065f, Ink);
        art.Circle(new Vector2(x, y), .046f, Metal);
        art.Rect(x, y, .052f, .014f, Ink);
    }

    private static void DrawFrameCorner(SchoolSceneDrawing art, SchoolSceneDrawing outline,
        Vector2 quarter, float deckHeight, int index, Color paint)
    {
        float side = index % 2 == 0 ? -1f : 1f;
        float outer = side * (quarter.x * .5f - .065f);
        float center = -side * quarter.x * .5f;
        float bottom = -quarter.y * .5f;
        float top = quarter.y * .5f;
        Color chalk = Color.Lerp(Ink, paint, .42f);
        void Rail(Vector2 a, Vector2 b)
        {
            art.Line(a, b, .09f, Ink);
            art.Line(a, b, .06f, paint);
            Trace(outline, chalk, a, b);
        }

        if (index < 2)
        {
            float deckY = bottom + deckHeight * .5f;
            Rail(new Vector2(outer, deckY), new Vector2(outer, top));
            art.Rect(0, deckY, quarter.x, deckHeight, Ink);
            art.Rect(0, deckY, quarter.x - .03f, deckHeight - .045f, Color.Lerp(Timber, paint, .52f));
            Bolt(art, outer * .8f, deckY);
            float edge = side * quarter.x * .5f;
            Trace(outline, chalk, new Vector2(center, bottom), new Vector2(edge, bottom),
                new Vector2(edge, bottom + deckHeight), new Vector2(center, bottom + deckHeight));
            return;
        }

        Vector2 shoulder = new Vector2(outer, top - .42f);
        Rail(new Vector2(outer, bottom), shoulder);
        Rail(shoulder, new Vector2(center, shoulder.y));
        Vector2 peak = new Vector2(center, top - .20f);
        Rail(shoulder, peak);
        float angle = side < 0 ? 90f : -90f;
        Vector2 eye = peak + Vector2.up * .09f;
        art.Arc(eye, .09f, angle, angle + 180f, .05f, paint);
        outline.Arc(eye, .09f, angle, angle + 180f, .035f, chalk);
    }

    private static void Trace(SchoolSceneDrawing art, Color ink, params Vector2[] corners)
    {
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 a = corners[i - 1], b = corners[i];
            int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / .29f));
            for (int j = 0; j < count; j++)
                art.Line(Vector2.Lerp(a, b, j / (float)count),
                    Vector2.Lerp(a, b, (j + .72f) / count), .043f, ink);
        }
    }

    public void RestoreDecks()
    {
        // SeesawCheckpoint.Start restores both contacts before SeesawRoomExit.Start.
        for (int i = 0; i < 2; i++)
            if (gears[i].IsRunning)
            {
                gears[i].WaitingForAssemblyTurn = false;
                Place(i, 1f);
            }
    }

    private void SetTint(MeshRenderer renderer, Color color)
    {
        tint.SetColor("_Color", color);
        renderer.SetPropertyBlock(tint);
    }

    public bool Advance(float seconds, SeesawBouncyFinale finale)
    {
        if (active < 0)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (progress[i] >= 1f || !gears[i].IsRunning || (i == 3 && !finale.IsLeftBridgeReady)) continue;
                active = i;
                gears[i].WaitingForAssemblyTurn = false;
                cueAge = seatedAge = 0f;
                break;
            }
        }
        if (active < 0) return progress[3] >= 1f;

        cueAge += seconds;
        float feedDelay = active < 2 && progress[1 - active] >= 1f ? 2f : .3f;
        if (cueAge < feedDelay || (active == 2 && !finale.IsBoardCracked)) return false;
        if (progress[active] < 1f)
        {
            Place(active, Mathf.MoveTowards(progress[active], 1f, seconds / 1.6f));
            if (active == 3 && progress[3] >= 1f) latch.PlayOneShot(click, .32f);
            return false;
        }
        seatedAge += seconds;
        if (active == 3)
        {
            float local = Mathf.Clamp01(seatedAge / .65f);
            for (int i = 0; i < parts.Length; i++)
            {
                if (renderers[i].sortingOrder == -3)
                {
                    partMeshes[i].colors = solidColors[i];
                    renderers[i].sortingOrder = -1;
                }
                Color lit = Color.Lerp(Background, Color.white, local);
                float pulse = Mathf.Sin(local * Mathf.PI) * .55f;
                SetTint(renderers[i], new Color(lit.r + pulse, lit.g + pulse, lit.b + pulse * .6f, lit.a));
            }
            if (local < 1f) return false;
        }
        else
        {
            const float flashSeconds = .34f;
            float pulse = Mathf.Sin(Mathf.PI * Mathf.Clamp01(seatedAge / flashSeconds));
            SetTint(renderers[active], Color.Lerp(Background, new Color(1.3f, 1.24f, 1.12f, 1f), pulse));
            if (seatedAge < flashSeconds) return false;
        }
        active = -1;
        return progress[3] >= 1f;
    }

    private void Place(int i, float t)
    {
        progress[i] = t;
        slots[i].gameObject.SetActive(t < 1f);
        Transform part = parts[i];
        part.gameObject.SetActive(true);
        Vector2 start = feeds[i], end = seats[i];
        float travel = Mathf.SmoothStep(0f, 1f, t);
        part.localPosition = Vector2.Lerp(start, end, travel) + Vector2.up * (1.2f * 4f * travel * (1f - travel));
        part.localScale = Vector3.one * Mathf.SmoothStep(.3f, 1f, t / .7f);
    }

    private static AudioClip MakeLatchClick()
    {
        const int rate = 22050;
        var samples = new float[rate / 10];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            samples[i] = Mathf.Exp(-80 * t) * (.6f * Mathf.Sin(5100 * t) + .4f * Mathf.Sin(8200 * t));
        }
        var clip = AudioClip.Create("Desk lift latch", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void Dispose()
    {
        foreach (Mesh mesh in meshes) SchoolSceneDrawing.Release(mesh);
        SchoolSceneDrawing.Release(material);
        SchoolSceneDrawing.Release(click);
    }
}
