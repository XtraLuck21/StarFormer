using UnityEngine;

public class FanAirflowVisual : MonoBehaviour
{
    [SerializeField] private FanBlower blower;
    [SerializeField] private int maxDots = 28;
    [SerializeField] private float dotSize = 0.12f;
    [SerializeField] private float emitInterval = 0.06f;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float streamWidth = 1.2f;

    private class Dot
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float distance;
        public float y;
        public int dir;
        public bool active;
    }

    private Dot[] dots;
    private float emitTimer;

    private Sprite whiteDotSprite;
    private Texture2D whiteDotTexture;

    private void Awake()
    {
        if (blower == null)
        {
            blower = GetComponentInParent<FanBlower>();
        }

        whiteDotTexture = CreateDotTexture(32);
        whiteDotTexture.name = "RuntimeAirflowDotTexture";
        whiteDotTexture.hideFlags = HideFlags.HideAndDontSave;

        whiteDotSprite = Sprite.Create(
            whiteDotTexture,
            new Rect(0f, 0f, 32f, 32f),
            new Vector2(0.5f, 0.5f),
            32f
        );

        whiteDotSprite.name = "RuntimeAirflowDotSprite";
        whiteDotSprite.hideFlags = HideFlags.HideAndDontSave;

        dots = new Dot[maxDots];

        for (int i = 0; i < maxDots; i++)
        {
            GameObject dotObject = new GameObject($"AirDot{i}");
            dotObject.transform.SetParent(transform, false);

            SpriteRenderer renderer = dotObject.AddComponent<SpriteRenderer>();
            renderer.sprite = whiteDotSprite;
            renderer.sortingOrder = 4;
            renderer.enabled = false;

            dots[i] = new Dot
            {
                transform = dotObject.transform,
                renderer = renderer
            };
        }
    }

    private void Update()
    {
        bool isOn = blower != null && blower.IsBlowing;
        float range = blower != null ? blower.MaxRange : 1f;
        int dir = blower != null ? blower.Facing : 1;

        if (isOn)
        {
            emitTimer += Time.deltaTime;

            while (emitTimer >= emitInterval)
            {
                emitTimer -= emitInterval;
                EmitDot(range, dir);
            }
        }

        for (int i = 0; i < dots.Length; i++)
        {
            Dot dot = dots[i];

            if (!dot.active)
            {
                continue;
            }

            dot.distance += moveSpeed * Time.deltaTime;

            if (dot.distance >= range)
            {
                dot.active = false;
                dot.renderer.enabled = false;
                continue;
            }

            const float start = 0.35f;

            dot.transform.localPosition = new Vector3(
                dot.dir * (start + dot.distance),
                dot.y,
                0f
            );

            // Fade out as the dot approaches max range.
            float t = dot.distance / range;
            float alpha = Mathf.Lerp(
                0.9f,
                0f,
                Mathf.SmoothStep(0.5f, 1f, t)
            );

            dot.renderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private void EmitDot(float range, int dir)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            Dot dot = dots[i];

            if (dot.active)
            {
                continue;
            }

            dot.active = true;
            dot.dir = dir;
            dot.distance = Random.Range(0f, 0.25f);
            dot.y = Random.Range(-0.5f, 0.5f) * streamWidth;
            dot.renderer.enabled = true;

            const float start = 0.35f;

            dot.transform.localPosition = new Vector3(
                dir * (start + dot.distance),
                dot.y,
                0f
            );

            dot.transform.localScale = Vector3.one * dotSize;
            dot.renderer.color = new Color(1f, 1f, 1f, 0.9f);
            return;
        }
    }

    // Soft-edged white circle.
    private static Texture2D CreateDotTexture(int resolution)
    {
        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        Color32[] pixels = new Color32[resolution * resolution];
        float radius = resolution * 0.5f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                byte alpha = 0;

                if (dist <= 0.72f)
                {
                    alpha = 255;
                }
                else if (dist < 1f)
                {
                    float fade = 1f - (dist - 0.72f) / 0.28f;
                    alpha = (byte)(255f * fade);
                }

                pixels[y * resolution + x] =
                    new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private void OnDestroy()
    {
        if (whiteDotSprite != null)
        {
            Destroy(whiteDotSprite);
        }

        if (whiteDotTexture != null)
        {
            Destroy(whiteDotTexture);
        }
    }
}
