using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the active Bouncy Ball checkpoint alive while Level 1 reloads after
/// a death. The object exists only for the current play session.
/// </summary>
public sealed class BouncyBallCheckpointSession : MonoBehaviour
{
    private readonly HashSet<string> reachedCheckpoints = new HashSet<string>();

    public bool HasReachedCheckpoint(string scenePath, string ballName)
    {
        return HasCheckpoint && ScenePath == scenePath && reachedCheckpoints.Contains(ballName);
    }

    public static BouncyBallCheckpointSession Instance { get; private set; }

    public bool HasCheckpoint { get; private set; }
    public string ScenePath { get; private set; }
    public string BallName { get; private set; }
    public Vector3 Position { get; private set; }

    public static BouncyBallCheckpointSession GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindFirstObjectByType<BouncyBallCheckpointSession>();

        if (Instance != null)
        {
            return Instance;
        }

        GameObject sessionObject = new GameObject(
            "BouncyBallCheckpointSession"
        );
        Instance = sessionObject.AddComponent<BouncyBallCheckpointSession>();
        DontDestroyOnLoad(sessionObject);
        return Instance;
    }

    public static void ClearCurrent()
    {
        if (Instance != null)
        {
            Instance.Clear();
        }
    }

    public void Save(string scenePath, string ballName, Vector3 position)
    {
        if (ScenePath != scenePath) reachedCheckpoints.Clear();
        reachedCheckpoints.Add(ballName);
        HasCheckpoint = true;
        ScenePath = scenePath;
        BallName = ballName;
        Position = position;
    }

    public void Clear()
    {
        reachedCheckpoints.Clear();
        HasCheckpoint = false;
        ScenePath = null;
        BallName = null;
        Position = Vector3.zero;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
