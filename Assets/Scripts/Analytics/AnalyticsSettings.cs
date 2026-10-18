using UnityEngine;

[CreateAssetMenu(
    fileName = "AnalyticsSettings",
    menuName = "StarFormer/Analytics Settings")]
public sealed class AnalyticsSettings : ScriptableObject
{
    [SerializeField] private bool collectionEnabled = true;

    [Tooltip("Google Apps Script Web App /exec URL. Leave blank until the test endpoint is ready.")]
    [SerializeField] private string endpointUrl = "";

    [SerializeField] private string environment = "test";
    [SerializeField] private string buildVersion = "e457e87";
    [SerializeField] private bool logPayloadsInEditor = true;
    [SerializeField, Min(5f)] private float snapshotIntervalSeconds = 30f;
    [SerializeField] private string[] trackedScenes = { "Level01", "Level02" };

    public bool CollectionEnabled => collectionEnabled;
    public string EndpointUrl => endpointUrl == null ? "" : endpointUrl.Trim();
    public string Environment => string.IsNullOrWhiteSpace(environment)
        ? "test"
        : environment.Trim();
    public string BuildVersion => string.IsNullOrWhiteSpace(buildVersion)
        ? Application.version
        : buildVersion.Trim();
    public bool LogPayloadsInEditor => logPayloadsInEditor;
    public float SnapshotIntervalSeconds => Mathf.Max(5f, snapshotIntervalSeconds);

    public bool TracksScene(string sceneName)
    {
        if (trackedScenes == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        foreach (string trackedScene in trackedScenes)
        {
            if (string.Equals(trackedScene, sceneName,
                    System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static AnalyticsSettings CreateRuntimeDefaults()
    {
        AnalyticsSettings settings = CreateInstance<AnalyticsSettings>();
        settings.hideFlags = HideFlags.HideAndDontSave;
        return settings;
    }
}
