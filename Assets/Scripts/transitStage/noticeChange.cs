using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class noticeChange : MonoBehaviour
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

        // 门被关闭对象（解锁）后，把右箭头换成左箭头。
        if (!door1.activeSelf)
        {
            text.text = text.text.Replace("Not this way", "← all the way");
            changed = true;
        }
    }
}