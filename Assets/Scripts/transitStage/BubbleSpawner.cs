using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float respawnDelay = 0.5f;

    private GameObject currentBubble;
    private float timer;

    private void Start()
    {
        SpawnBubble();
    }

private void Update()
{
    if (bubblePrefab == null)
    {
        return;
    }

    // 泡泡存在且处于激活状态，就继续等待。
    if (currentBubble != null && currentBubble.activeInHierarchy)
    {
        return;
    }

    timer += Time.deltaTime;

    if (timer >= respawnDelay)
    {
        SpawnBubble();
    }
}

    private void SpawnBubble()
    {
        if (bubblePrefab == null)
        {
            return;
        }

        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        currentBubble = Instantiate(
            bubblePrefab,
            position,
            Quaternion.identity
        );

        timer = 0f;
    }
}