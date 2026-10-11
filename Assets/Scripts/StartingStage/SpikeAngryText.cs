using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class SpikeAngryText : MonoBehaviour
{
    [Header("Shake")]
    [SerializeField, Min(0f)] private float shakeAmount = 0.06f;
    [SerializeField, Min(0f)] private float shakeSpeed = 35f;

    [Header("Visibility")]
    [SerializeField] private bool hideAtStart = true;

    private TMP_Text text;
    private Vector3 originalPosition;
    private float originalAlpha;
    private float shakeTime;
    private bool shaking;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        originalPosition = transform.localPosition;
        originalAlpha = text.alpha;

        if (hideAtStart)
        {
            text.alpha = 0f;
        }
    }

    public void ShowAngry()
    {
        text.alpha = originalAlpha;
        shakeTime = 0f;
        shaking = true;
    }

    public void HideAngry()
    {
        shaking = false;
        text.alpha = 0f;
        transform.localPosition = originalPosition;
    }

    private void LateUpdate()
    {
        if (!shaking)
        {
            return;
        }

        shakeTime += Time.deltaTime;

        float offsetX =
            Mathf.Sin(shakeTime * shakeSpeed) * shakeAmount;

        float offsetY =
            Mathf.Sin(shakeTime * shakeSpeed * 1.3f) *
            shakeAmount * 0.5f;

        transform.localPosition =
            originalPosition + new Vector3(offsetX, offsetY, 0f);
    }

    private void OnDisable()
    {
        shaking = false;
        transform.localPosition = originalPosition;
    }
}