using System.Collections;
using UnityEngine;

// 看不见的踩踏触发区：被控制的身体走进来时，依次激活指定物体。
[RequireComponent(typeof(Collider2D))]
public class HiddenTriggerZone : MonoBehaviour
{
    [Header("Targets")]
    // 要激活的物体，按列表顺序出场。把四根红刺拖进来。
    [SerializeField] private GameObject[] targets = new GameObject[4];

    // 勾上：开场自动把上面的物体全部隐藏，省得手动一个个取消勾选。
    [SerializeField] private bool hideTargetsAtStart = true;

    [Header("Timing")]
    // 踩到之后等几秒才开始。
    [SerializeField, Min(0f)] private float firstDelay = 0f;

    // 每个物体之间隔几秒，0 = 同时出现。
    [SerializeField, Min(0f)] private float delayBetween = 0.4f;

    [Header("Conditions")]
    // 勾上：只有玩家当前控制的身体才能触发，别的物体路过不算。
    [SerializeField] private bool requireControlledBody = true;

    [SerializeField] private bool triggerOnce = true;

    [Header("Editor")]
    // 勾上：按 Play 后自动隐藏自己的贴图，编辑时仍然看得见。
    [SerializeField] private bool hideSpriteOnPlay = true;

    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0f, 0.35f);

    private Collider2D zone;
    private bool hasFired;

    private void Awake()
    {
        zone = GetComponent<Collider2D>();

        // 只感应，不阻挡玩家走路。
        zone.isTrigger = true;

        if (hideSpriteOnPlay &&
            TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.enabled = false;
        }

        if (!hideTargetsAtStart)
        {
            return;
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasFired && triggerOnce)
        {
            return;
        }

        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null ||
            !otherBody.TryGetComponent(out PossessableBody possessableBody))
        {
            return;
        }

        if (requireControlledBody && !possessableBody.IsControlled)
        {
            return;
        }

        hasFired = true;
        StartCoroutine(ActivateTargets());

        Debug.Log($"{name} triggered by {otherBody.name}.", this);
    }

    private IEnumerator ActivateTargets()
    {
        if (firstDelay > 0f)
        {
            yield return new WaitForSeconds(firstDelay);
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                target.SetActive(true);
            }

            if (delayBetween > 0f)
            {
                yield return new WaitForSeconds(delayBetween);
            }
        }
    }

    // 在 Scene 窗口里把触发区画出来，方便摆位置。
    private void OnDrawGizmos()
    {
        Collider2D preview = GetComponent<Collider2D>();

        if (preview == null)
        {
            return;
        }

        Bounds bounds = preview.bounds;

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}