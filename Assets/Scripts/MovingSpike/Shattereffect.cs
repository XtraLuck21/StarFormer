using UnityEngine;

// 挂在任何「被打碎时要掉碎片」的物体上，然后在销毁前调用 Play()。
public class ShatterEffect : MonoBehaviour
{
    [Header("Pieces")]
    [SerializeField, Min(1)] private int pieceCount = 6;

    // 留空就自动用物体自己的贴图，碎片是它的缩小版。
    [SerializeField] private Sprite pieceSprite;

    [SerializeField, Min(0.01f)] private float pieceScale = 0.3f;

    [Header("Physics")]
    // 向四周飞散的速度。
    [SerializeField, Min(0f)] private float spreadSpeed = 3f;

    // 额外向上的初速度，让碎片先弹起来再落下。
    [SerializeField, Min(0f)] private float upwardBoost = 2f;

    // 翻滚速度，单位是度/秒。
    [SerializeField, Min(0f)] private float spinSpeed = 240f;

    [SerializeField, Min(0f)] private float gravityScale = 1.5f;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)] private float lifetime = 2f;

    // 消失前渐隐的秒数。
    [SerializeField, Min(0f)] private float fadeTime = 0.6f;

    public void Play()
    {
        SpriteRenderer source = GetComponentInChildren<SpriteRenderer>();

        Sprite sprite = pieceSprite != null
            ? pieceSprite
            : (source != null ? source.sprite : null);

        if (sprite == null)
        {
            Debug.LogWarning("ShatterEffect 找不到可用的贴图。", this);
            return;
        }

        Color tint = source != null ? source.color : Color.white;
        string sortingLayer = source != null ? source.sortingLayerName : "Default";
        int sortingOrder = source != null ? source.sortingOrder : 0;

        for (int i = 0; i < pieceCount; i++)
        {
            CreatePiece(sprite, tint, sortingLayer, sortingOrder);
        }
    }

    private void CreatePiece(
        Sprite sprite,
        Color tint,
        string sortingLayer,
        int sortingOrder)
    {
        // 不设父物体，这样原物体被销毁后碎片还在。
        GameObject piece = new GameObject("Debris");

        piece.transform.position =
            transform.position + (Vector3)(Random.insideUnitCircle * 0.2f);

        piece.transform.rotation =
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        piece.transform.localScale = Vector3.one * pieceScale;

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;

        Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 向上半圆随机飞散，再统一加一点向上的力。
        Vector2 direction = Random.insideUnitCircle.normalized;
        direction.y = Mathf.Abs(direction.y);

        rb.linearVelocity =
            direction * Random.Range(spreadSpeed * 0.5f, spreadSpeed)
            + Vector2.up * upwardBoost;

        rb.angularVelocity = Random.Range(-spinSpeed, spinSpeed);

        CircleCollider2D collider = piece.AddComponent<CircleCollider2D>();
        collider.radius = Mathf.Max(0.1f, sprite.bounds.extents.magnitude * 0.5f);

        piece.AddComponent<ShatterPiece>().Init(lifetime, fadeTime);
    }
}

// 自动挂在每个碎片上：计时、渐隐、销毁。
public class ShatterPiece : MonoBehaviour
{
    private SpriteRenderer renderer2D;
    private Color startColor;

    private float lifetime;
    private float fadeTime;
    private float age;

    public void Init(float pieceLifetime, float pieceFadeTime)
    {
        lifetime = pieceLifetime;
        fadeTime = pieceFadeTime;

        renderer2D = GetComponent<SpriteRenderer>();
        startColor = renderer2D != null ? renderer2D.color : Color.white;
    }

    private void Update()
    {
        age += Time.deltaTime;

        float fadeStart = lifetime - fadeTime;

        if (renderer2D != null && fadeTime > 0f && age > fadeStart)
        {
            float t = Mathf.InverseLerp(fadeStart, lifetime, age);

            Color color = startColor;
            color.a = startColor.a * (1f - t);

            renderer2D.color = color;
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}