using UnityEngine;

public class RotateToRight : MonoBehaviour
{
    [SerializeField] private GameObject door1;

    private bool rotated;

    private void Update()
    {
        if (rotated || door1 == null)
        {
            return;
        }

        if (!door1.activeSelf)
        {
            transform.Rotate(0f, 0f, 180f, Space.Self);
            rotated = true;
        }
    }
}