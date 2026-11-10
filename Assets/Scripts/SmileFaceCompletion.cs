using UnityEngine;

/// <summary>The three-tooth reveal: a connected grin, opening eyes and a full-room view.</summary>
[DefaultExecutionOrder(140)]
public sealed class SmileFaceCompletion : MonoBehaviour
{
    [SerializeField] private TeethTriggeredDoor toothDoor;
    [SerializeField] private SpriteRenderer face;
    [SerializeField] private PlayerPossessionController player;
    [SerializeField] private level2Camera roomCamera;
    [SerializeField] private Shader eyeShader;
    [SerializeField] private Renderer levelTitle;
    [SerializeField, Min(.1f)] private float transitionSeconds = 2.6f;
    public bool HasStarted { get; private set; }
    public float Progress { get; private set; }
    public Bounds RoomBounds { get; private set; }
    public Vector2 LeftLook { get; private set; }
    public Vector2 RightLook { get; private set; }

    private Material originalFaceMaterial, eyeMaterial, mouthMaterial;
    private Mesh mouthMesh;
    private Transform mouth;
    private SpriteRenderer[] toothVisuals;
    private Color[] toothColors;
    private float age;
    private bool enteredForehead;
    private bool titleWasVisible;

    public void RestoreCompletedPose() => age = transitionSeconds;

    private void Awake()
    {
        RoomBounds = face.bounds;
        foreach (Collider2D surface in GetComponentsInChildren<Collider2D>())
            if (surface.enabled && !surface.isTrigger) {
                Bounds bounds = RoomBounds;
                bounds.Encapsulate(surface.bounds);
                RoomBounds = bounds;
            }
        var teeth = GetComponentsInChildren<RatchetingCeiling>();
        toothVisuals = new SpriteRenderer[teeth.Length];
        toothColors = new Color[teeth.Length];
        for (int i = 0; i < teeth.Length; i++) {
            toothVisuals[i] = teeth[i].GetComponent<SpriteRenderer>();
            toothColors[i] = toothVisuals[i].color;
        }
        titleWasVisible = levelTitle != null && levelTitle.enabled;
        originalFaceMaterial = face.sharedMaterial;
        eyeMaterial = new Material(eyeShader);
        face.sharedMaterial = eyeMaterial;
        BuildMouth();
    }

    private Vector3 PixelToLocal(Vector2 pixel)
    {
        Sprite sprite = face.sprite;
        return (pixel - sprite.rect.position - sprite.pivot) / sprite.pixelsPerUnit;
    }

    private Vector3 EyeWorld(Vector2 uv)
    {
        return face.transform.TransformPoint(PixelToLocal(new Vector2(uv.x * face.sprite.texture.width,
            uv.y * face.sprite.texture.height)));
    }

    private void BuildMouth()
    {
        var go = new GameObject("Connected Smile");
        go.transform.SetParent(face.transform, false);
        mouth = go.transform;
        // Join only the envelope of the four painted grey teeth. The surrounding
        // black mouth, including its original thick upper/lower border, stays visible.
        Vector2[] outline = {
            new Vector2(513, 402),
            new Vector2(515, 378),
            new Vector2(516, 368),
            new Vector2(520, 335),
            new Vector2(521, 330),
            new Vector2(525, 313),
            new Vector2(529, 297),
            new Vector2(537, 273),
            new Vector2(541, 263),
            new Vector2(549, 247),
            new Vector2(553, 240),
            new Vector2(556, 235),
            new Vector2(560, 229),
            new Vector2(563, 225),
            new Vector2(567, 220),
            new Vector2(580, 207),
            new Vector2(584, 204),
            new Vector2(586, 203),
            new Vector2(657, 169),
            new Vector2(663, 167),
            new Vector2(671, 165),
            new Vector2(686, 162),
            new Vector2(692, 161),
            new Vector2(700, 160),
            new Vector2(710, 160),
            new Vector2(803, 161),
            new Vector2(811, 162),
            new Vector2(817, 163),
            new Vector2(822, 164),
            new Vector2(834, 167),
            new Vector2(911, 202),
            new Vector2(916, 205),
            new Vector2(919, 207),
            new Vector2(923, 210),
            new Vector2(935, 222),
            new Vector2(939, 227),
            new Vector2(942, 231),
            new Vector2(946, 237),
            new Vector2(951, 245),
            new Vector2(960, 263),
            new Vector2(963, 270),
            new Vector2(965, 275),
            new Vector2(973, 299),
            new Vector2(975, 306),
            new Vector2(977, 314),
            new Vector2(979, 324),
            new Vector2(981, 336),
            new Vector2(984, 356),
            new Vector2(986, 370),
            new Vector2(987, 379),
            new Vector2(988, 389),
            new Vector2(989, 401),
            new Vector2(989, 411),
            new Vector2(987, 413),
            new Vector2(514, 413),
            new Vector2(513, 412)
        };
        Vector3 center = PixelToLocal(new Vector2(750, 285));
        mouth.localPosition = center;
        var vertices = new Vector3[outline.Length + 1];
        var uvs = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var triangles = new int[outline.Length * 3];
        colors[0] = Color.white;
        Texture2D texture = face.sprite.texture;
        uvs[0] = new Vector2(684f / texture.width, (texture.height - 737f) / texture.height);
        for (int i = 0; i < outline.Length; i++) {
            vertices[i + 1] = PixelToLocal(outline[i]) - center;
            colors[i + 1] = Color.white;
            // Reuse the existing tooth's soft grey texture across the connected grin.
            uvs[i + 1] = new Vector2(Mathf.Lerp(664f, 704f, Mathf.InverseLerp(513f, 989f, outline[i].x)) / texture.width,
                (texture.height - Mathf.Lerp(805f, 660f, Mathf.InverseLerp(158f, 414f, outline[i].y))) / texture.height);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % outline.Length + 1;
        }
        mouthMesh = new Mesh { name = "Continuous Smile Fill", vertices = vertices, uv = uvs, colors = colors, triangles = triangles };
        go.AddComponent<MeshFilter>().sharedMesh = mouthMesh;
        var renderer = go.AddComponent<MeshRenderer>();
        mouthMaterial = new Material(Shader.Find("Sprites/Default"));
        mouthMaterial.mainTexture = texture;
        mouthMaterial.color = new Color(1f, .995f, .995f, 0f);
        renderer.sharedMaterial = mouthMaterial;
        renderer.sortingLayerID = face.sortingLayerID;
        renderer.sortingOrder = face.sortingOrder + 1;
        go.SetActive(false);
    }

    private Vector2 LookAtPlayer(Vector2 uv)
    {
        Vector2 direction = (Vector2)player.CurrentBody.transform.position - (Vector2)EyeWorld(uv - Vector2.up * .015f);
        direction = Vector2.ClampMagnitude(direction / 8f, 1f);
        return new Vector2(direction.x * .010f, direction.y * .016f);
    }

    private void Update()
    {
        if (player.IsDead || player.IsRestarting || Time.timeScale <= 0f || player.CurrentBody == null) return;
        Vector3 playerPosition = player.CurrentBody.transform.position;
        playerPosition.z = RoomBounds.center.z;
        player.CheckpointFeedback.SetTextSuppressed(RoomBounds.Contains(playerPosition));
        if (!HasStarted) {
            if (!toothDoor.IsOpen) { UpdateForeheadCamera(); return; }
            HasStarted = true;
            mouth.gameObject.SetActive(true);
            foreach (SpriteRenderer tooth in toothVisuals)
                foreach (TMPro.TMP_Text hint in tooth.GetComponentsInChildren<TMPro.TMP_Text>())
                    hint.gameObject.SetActive(false);
            if (levelTitle != null) {
                levelTitle.enabled = false;
            }
        }
        age += Time.deltaTime;
        Progress = Mathf.Clamp01(age / transitionSeconds);
        float fill = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.2f, 1.6f, age));
        // Hold the painted closed eyes for a beat, then visibly lift the lids.
        float eyeReveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, .9f, age));
        float opening = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.95f, 2.45f, age));
        mouth.localScale = new Vector3(Mathf.Lerp(.72f, 1f, fill), Mathf.Lerp(.65f, 1f, fill), 1f);
        mouthMaterial.color = new Color(1f, .995f, .995f, fill);
        for (int i = 0; i < toothVisuals.Length; i++) {
            Color color = toothColors[i];
            color.a *= 1f - fill;
            toothVisuals[i].color = color;
        }
        eyeMaterial.SetFloat("_EyeReveal", eyeReveal);
        eyeMaterial.SetFloat("_EyeOpen", opening);
        LeftLook = Vector2.Lerp(LeftLook, LookAtPlayer(new Vector2(.372f,.625f)), 1f - Mathf.Exp(-7f * Time.deltaTime));
        RightLook = Vector2.Lerp(RightLook, LookAtPlayer(new Vector2(.614f,.625f)), 1f - Mathf.Exp(-7f * Time.deltaTime));
        eyeMaterial.SetVector("_LeftLook", LeftLook);
        eyeMaterial.SetVector("_RightLook", RightLook);
        UpdateForeheadCamera();
    }

    public void EnterForeheadOverview()
    {
        if (player.CurrentBody == null || player.CurrentBody.Form == BodyForm.Box ||
            player.IsDead || player.IsRestarting || Time.timeScale <= 0f) return;
        enteredForehead = true;
        if (!roomCamera.IsSmileOverview) roomCamera.BeginSmileOverview(RoomBounds, 1f);
        if (levelTitle != null) levelTitle.enabled = false;
    }

    private void UpdateForeheadCamera()
    {
        Bounds exitBounds = RoomBounds;
        exitBounds.Expand(new Vector3(4f, 4f, 20f));
        Vector3 position = player.CurrentBody.transform.position;
        position.z = exitBounds.center.z;
        // Keep the whole child visible throughout the forehead challenge. Reaching
        // the hand's Box ends that framing while the player is still inside the room.
        Vector3 local = transform.InverseTransformPoint(player.CurrentBody.transform.position);
        bool inForehead = local.x >= 32.7f && local.x <= 60.8f && local.y >= -32.8f && local.y <= -26.5f;
        if (player.CurrentBody.Form == BodyForm.Box) enteredForehead = false;
        else if (inForehead) enteredForehead = true;
        bool wantsOverview = (HasStarted && Progress < 1f) || (enteredForehead && player.CurrentBody.Form != BodyForm.Box && exitBounds.Contains(position));
        if (wantsOverview && !roomCamera.IsSmileOverview)
            roomCamera.BeginSmileOverview(RoomBounds, HasStarted && Progress < 1f ? 1.8f : 1f);
        else if (!wantsOverview && roomCamera.IsSmileOverview)
            roomCamera.EndSmileOverview();
        if (wantsOverview && levelTitle != null) levelTitle.enabled = false;
        if (!wantsOverview && (!HasStarted || !exitBounds.Contains(position))) {
            if (levelTitle != null) levelTitle.enabled = titleWasVisible;
        }
    }

    private void OnDestroy()
    {
        if (face != null) face.sharedMaterial = originalFaceMaterial;
        if (eyeMaterial != null) Destroy(eyeMaterial);
        if (mouthMaterial != null) Destroy(mouthMaterial);
        if (mouthMesh != null) Destroy(mouthMesh);
    }

    private void OnDisable()
    {
        if (Application.isPlaying && player != null) player.CheckpointFeedback.SetTextSuppressed(false);
        if (Application.isPlaying && roomCamera != null) roomCamera.EndSmileOverview();
        if (levelTitle != null) levelTitle.enabled = titleWasVisible;
    }
}
