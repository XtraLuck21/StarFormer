using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class RotateToRight : MonoBehaviour
{
    [SerializeField] private GameObject door1;

    private TMP_Text text;
    private bool changed;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (changed || door1 == null)
        {
            return;
        }

        // When the gate object is deactivated (unlocked), replace the right arrow with the left arrow.
        if (!door1.activeSelf)
        {
            text.text = text.text.Replace("→", "←");
            changed = true;
        }
    }
}