using UnityEngine;

public class ShowSpiderAfterDoorOpen : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private GameObject spider;

    private bool shown;

    private void Awake()
    {
        if (spider != null)
        {
            spider.SetActive(false);
        }
    }

    private void Update()
    {
        if (shown || door == null || spider == null)
        {
            return;
        }

        if (!door.activeSelf)
        {
            shown = true;
            spider.SetActive(true);
        }
    }
}
