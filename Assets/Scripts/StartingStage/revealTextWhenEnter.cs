using TMPro;
using UnityEngine;

public class RevealTextOnEnter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private PlayerPossessionController player;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float revealDistance = 3f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.5f;
    [SerializeField, Min(0f)] private float hideDelay = 1f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 1f;

    private float originalAlpha;
    private float visibility;
    private float outsideTime;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPossessionController>();
        }

        if (text == null)
        {
            Debug.LogWarning("Assign the Text reference on RevealTextOnEnter.", this);
            enabled = false;
            return;
        }

        originalAlpha = text.alpha;
        text.maxVisibleCharacters = int.MaxValue;
        text.alpha = 0f;
    }

    private void Update()
    {
        if (text == null)
        {
            return;
        }

        PossessableBody body = player != null
            ? player.CurrentBody
            : null;

        // Measure the distance from the currently controlled body to the text.
        bool isNear = body != null &&
            Vector2.Distance(
                body.transform.position,
                text.transform.position
            ) <= revealDistance;

        if (isNear)
        {
            outsideTime = 0f;

            visibility = Mathf.MoveTowards(
                visibility,
                1f,
                Time.deltaTime / Mathf.Max(0.01f, fadeInDuration)
            );
        }
        else
        {
            outsideTime += Time.deltaTime;

            if (outsideTime >= hideDelay)
            {
                visibility = Mathf.MoveTowards(
                    visibility,
                    0f,
                    Time.deltaTime / Mathf.Max(0.01f, fadeOutDuration)
                );
            }
        }

        text.alpha = originalAlpha * visibility;
    }

    private void Reset()
    {
        text = GetComponent<TMP_Text>();
    }
}