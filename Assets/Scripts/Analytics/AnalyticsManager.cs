using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AnalyticsManager : MonoBehaviour
{
    private const string SettingsResourceName = "AnalyticsSettings";
    private const string PlayerIdKey = "starformer.analytics.player_id.v1";

    public static AnalyticsManager Instance { get; private set; }

    private AnalyticsSettings settings;
    private string playerId;
    private string sessionId;

    private bool runActive;
    private bool runPaused;
    private bool awaitingDeathRespawn;
    private string activeLevelName;
    private int activeSceneHandle;
    private string levelRunId;
    private string attemptId;
    private string checkpointId;
    private string currentForm;
    private float levelActiveSeconds;
    private float attemptActiveSeconds;
    private float currentFormActiveSeconds;
    private float snapshotElapsedSeconds;
    private int deathCount;
    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition;
    private bool pageActive = true;
    private bool warnedMissingEndpoint;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StarFormerAnalytics_Send(
        string endpoint,
        string payload);

    [DllImport("__Internal")]
    private static extern void StarFormerAnalytics_Flush();
#endif

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject analyticsObject = new GameObject("AnalyticsManager");
        analyticsObject.AddComponent<AnalyticsManager>();
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

        settings = Resources.Load<AnalyticsSettings>(SettingsResourceName);
        if (settings == null)
        {
            settings = AnalyticsSettings.CreateRuntimeDefaults();
            Debug.LogWarning(
                "AnalyticsSettings.asset was not found. Using runtime test defaults.",
                this
            );
        }

        playerId = LoadOrCreatePlayerId();
        sessionId = NewId();
        SceneManager.sceneLoaded += HandleSceneLoaded;

        EmitEvent(
            "session_start",
            controlledFormOverride: "",
            hasPosition: false,
            details: "page_loaded"
        );
    }

    private void Update()
    {
        if (!runActive || runPaused || Time.timeScale <= 0f ||
            !pageActive || !Application.isFocused)
        {
            return;
        }

        float activeDelta = Time.unscaledDeltaTime;
        levelActiveSeconds += activeDelta;
        attemptActiveSeconds += activeDelta;
        currentFormActiveSeconds += activeDelta;
        snapshotElapsedSeconds += activeDelta;

        if (snapshotElapsedSeconds >= settings.SnapshotIntervalSeconds)
        {
            snapshotElapsedSeconds = 0f;
            EmitSnapshot("interval");
        }
    }

    public void BeginLevel(PossessableBody controlledBody, string savedCheckpointId)
    {
        if (controlledBody == null || settings == null)
        {
            return;
        }

        Scene scene = controlledBody.gameObject.scene;
        if (!settings.TracksScene(scene.name))
        {
            return;
        }

        string formName = controlledBody.Form.ToString();
        Vector3 position = controlledBody.Body != null
            ? (Vector3)controlledBody.Body.position
            : controlledBody.transform.position;
        string normalizedCheckpoint = NormalizeCheckpoint(savedCheckpointId);

        if (runActive && activeLevelName == scene.name &&
            activeSceneHandle != scene.handle && !awaitingDeathRespawn)
        {
            EndRun("restart", "unclassified_scene_reload");
        }

        if (!runActive || activeLevelName != scene.name)
        {
            StartNewRun(scene, formName, position, normalizedCheckpoint);
            return;
        }

        activeSceneHandle = scene.handle;
        UpdateLastPosition(position);

        if (awaitingDeathRespawn)
        {
            awaitingDeathRespawn = false;
            runPaused = false;
            checkpointId = normalizedCheckpoint;
            currentForm = formName;
            currentFormActiveSeconds = 0f;
            StartNewAttempt("death_respawn");
            return;
        }

        runPaused = false;
        if (string.IsNullOrEmpty(currentForm))
        {
            currentForm = formName;
            currentFormActiveSeconds = 0f;
        }
        else if (!string.Equals(currentForm, formName,
                     StringComparison.Ordinal))
        {
            ChangeControlledForm(
                currentForm,
                formName,
                position,
                "scene_start_recovery"
            );
        }
    }

    public void TrackCheckpoint(
        string newCheckpointId,
        PossessableBody controlledBody)
    {
        if (!runActive || runPaused ||
            string.IsNullOrWhiteSpace(newCheckpointId))
        {
            return;
        }

        string normalized = NormalizeCheckpoint(newCheckpointId);
        if (string.Equals(checkpointId, normalized, StringComparison.Ordinal))
        {
            return;
        }

        if (controlledBody != null)
        {
            UpdateLastPosition(controlledBody.Body != null
                ? (Vector3)controlledBody.Body.position
                : controlledBody.transform.position);
        }

        checkpointId = normalized;
        EmitEvent(
            "checkpoint_reached",
            startReason: "checkpoint_reached",
            details: normalized
        );

        StartNewAttempt("checkpoint_reached");
    }

    public void ChangeControlledForm(
        BodyForm previousForm,
        BodyForm nextForm,
        Vector3 position,
        string reason)
    {
        ChangeControlledForm(
            previousForm.ToString(),
            nextForm.ToString(),
            position,
            reason
        );
    }

    public void TrackDeath(
        PossessableBody controlledBody,
        AnalyticsDeathCause cause)
    {
        if (!runActive || runPaused || controlledBody == null)
        {
            return;
        }

        string deadForm = controlledBody.Form.ToString();
        Vector3 position = controlledBody.Body != null
            ? (Vector3)controlledBody.Body.position
            : controlledBody.transform.position;
        UpdateLastPosition(position);

        if (string.IsNullOrEmpty(currentForm))
        {
            currentForm = deadForm;
        }

        FlushCurrentForm("death");
        deathCount++;

        EmitEvent(
            "death",
            controlledFormOverride: deadForm,
            deathCause: AnalyticsValueNames.DeathCause(cause),
            hasPosition: true,
            result: "death",
            endReason: "death"
        );

        currentForm = "";
        currentFormActiveSeconds = 0f;
        runPaused = true;
        awaitingDeathRespawn = true;
        snapshotElapsedSeconds = 0f;
        FlushTransport();
    }

    public void TrackLevelComplete(PossessableBody controlledBody)
    {
        if (!runActive || runPaused)
        {
            return;
        }

        if (controlledBody != null)
        {
            UpdateLastPosition(controlledBody.Body != null
                ? (Vector3)controlledBody.Body.position
                : controlledBody.transform.position);
        }

        FlushCurrentForm("level_complete");
        EmitEvent(
            "level_complete",
            result: "complete",
            endReason: "level_complete",
            hasPosition: hasLastKnownPosition
        );
        ClearRunState();
        FlushTransport();
    }

    public void TrackManualRestart()
    {
        EndRun("restart", "manual_restart");
    }

    public void TrackAbandoned(string reason = "return_to_menu")
    {
        EndRun("abandoned", reason);
    }

    public void FlushNow()
    {
        if (runActive)
        {
            EmitSnapshot("manual_flush");
        }

        FlushTransport();
    }

    private void StartNewRun(
        Scene scene,
        string formName,
        Vector3 position,
        string startingCheckpoint)
    {
        activeLevelName = scene.name;
        activeSceneHandle = scene.handle;
        levelRunId = NewId();
        checkpointId = startingCheckpoint;
        currentForm = formName;
        levelActiveSeconds = 0f;
        attemptActiveSeconds = 0f;
        currentFormActiveSeconds = 0f;
        snapshotElapsedSeconds = 0f;
        deathCount = 0;
        runActive = true;
        runPaused = false;
        awaitingDeathRespawn = false;
        UpdateLastPosition(position);

        EmitEvent(
            "level_start",
            controlledFormOverride: formName,
            startReason: "scene_start",
            hasPosition: true
        );
        StartNewAttempt("level_start");
    }

    private void StartNewAttempt(string reason)
    {
        attemptId = NewId();
        attemptActiveSeconds = 0f;

        EmitEvent(
            "attempt_start",
            startReason: reason,
            hasPosition: hasLastKnownPosition
        );
    }

    private void ChangeControlledForm(
        string previousForm,
        string nextForm,
        Vector3 position,
        string reason)
    {
        if (!runActive || runPaused || string.IsNullOrWhiteSpace(nextForm))
        {
            return;
        }

        UpdateLastPosition(position);
        string recordedPrevious = string.IsNullOrWhiteSpace(currentForm)
            ? previousForm
            : currentForm;

        FlushCurrentForm(reason);
        currentForm = nextForm;
        currentFormActiveSeconds = 0f;

        EmitEvent(
            "form_change",
            controlledFormOverride: nextForm,
            previousForm: recordedPrevious,
            startReason: reason,
            hasPosition: true
        );
    }

    private void FlushCurrentForm(string endReason)
    {
        if (!runActive || string.IsNullOrWhiteSpace(currentForm) ||
            currentFormActiveSeconds <= 0f)
        {
            return;
        }

        EmitEvent(
            "form_duration",
            controlledFormOverride: currentForm,
            durationSeconds: currentFormActiveSeconds,
            endReason: endReason,
            hasPosition: hasLastKnownPosition
        );
        currentFormActiveSeconds = 0f;
    }

    private void EndRun(string result, string reason)
    {
        if (!runActive)
        {
            return;
        }

        FlushCurrentForm(reason);
        EmitEvent(
            "level_end",
            result: result,
            endReason: reason,
            hasPosition: hasLastKnownPosition
        );
        ClearRunState();
        FlushTransport();
    }

    private void ClearRunState()
    {
        runActive = false;
        runPaused = false;
        awaitingDeathRespawn = false;
        activeLevelName = "";
        activeSceneHandle = 0;
        levelRunId = "";
        attemptId = "";
        checkpointId = "";
        currentForm = "";
        levelActiveSeconds = 0f;
        attemptActiveSeconds = 0f;
        currentFormActiveSeconds = 0f;
        snapshotElapsedSeconds = 0f;
        deathCount = 0;
        hasLastKnownPosition = false;
    }

    private void EmitSnapshot(string reason)
    {
        if (!runActive)
        {
            return;
        }

        EmitEvent(
            "analytics_snapshot",
            durationSeconds: currentFormActiveSeconds,
            endReason: reason,
            hasPosition: hasLastKnownPosition,
            details: "Open form segment; do not sum snapshots with form_duration."
        );
    }

    private void EmitEvent(
        string eventType,
        string controlledFormOverride = null,
        string previousForm = "",
        string deathCause = "",
        float durationSeconds = 0f,
        string startReason = "",
        string endReason = "",
        string result = "",
        bool? hasPosition = null,
        string details = "")
    {
        if (settings == null || !settings.CollectionEnabled)
        {
            return;
        }

        bool includePosition = hasPosition ?? hasLastKnownPosition;
        AnalyticsEvent analyticsEvent = new AnalyticsEvent
        {
            environment = settings.Environment,
            client_timestamp = DateTime.UtcNow.ToString("O"),
            event_id = NewId(),
            event_type = eventType,
            player_id = playerId ?? "",
            session_id = sessionId ?? "",
            level_run_id = levelRunId ?? "",
            attempt_id = attemptId ?? "",
            level_name = activeLevelName ?? "",
            checkpoint_id = checkpointId ?? "",
            controlled_form = controlledFormOverride ?? currentForm ?? "",
            previous_form = previousForm ?? "",
            death_cause = deathCause ?? "",
            position_x = includePosition ? lastKnownPosition.x : 0f,
            position_y = includePosition ? lastKnownPosition.y : 0f,
            has_position = includePosition,
            duration_seconds = RoundSeconds(durationSeconds),
            attempt_active_seconds = RoundSeconds(attemptActiveSeconds),
            level_active_seconds = RoundSeconds(levelActiveSeconds),
            death_count = deathCount,
            start_reason = startReason ?? "",
            end_reason = endReason ?? "",
            result = result ?? "",
            build_version = settings.BuildVersion,
            details = details ?? ""
        };

        string payload = JsonUtility.ToJson(analyticsEvent);
        SendPayload(payload);
    }

    private void SendPayload(string payload)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(settings.EndpointUrl))
        {
            if (!warnedMissingEndpoint)
            {
                warnedMissingEndpoint = true;
                Debug.LogWarning(
                    "Analytics collection is enabled, but the Web App endpoint is blank.",
                    this
                );
            }

            return;
        }

        StarFormerAnalytics_Send(settings.EndpointUrl, payload);
#else
        if (settings.LogPayloadsInEditor)
        {
            Debug.Log($"[Analytics] {payload}", this);
        }
#endif
    }

    private void FlushTransport()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(settings.EndpointUrl))
        {
            StarFormerAnalytics_Flush();
        }
#endif
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pageActive = true;

        if (runActive && !string.Equals(
                scene.name,
                activeLevelName,
                StringComparison.Ordinal))
        {
            EndRun("abandoned", "scene_changed");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        pageActive = hasFocus;
        if (!hasFocus && runActive)
        {
            EmitSnapshot("focus_lost");
            FlushTransport();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        pageActive = !pauseStatus;
        if (pauseStatus && runActive)
        {
            EmitSnapshot("application_paused");
            FlushTransport();
        }
    }

    private void OnApplicationQuit()
    {
        if (runActive)
        {
            EmitSnapshot("application_quit");
        }

        FlushTransport();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void UpdateLastPosition(Vector3 position)
    {
        lastKnownPosition = position;
        hasLastKnownPosition = true;
    }

    private string LoadOrCreatePlayerId()
    {
        string savedId = PlayerPrefs.GetString(PlayerIdKey, "");
        if (!string.IsNullOrWhiteSpace(savedId))
        {
            return savedId;
        }

        savedId = NewId();
        PlayerPrefs.SetString(PlayerIdKey, savedId);
        PlayerPrefs.Save();
        return savedId;
    }

    private static string NormalizeCheckpoint(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? AnalyticsValueNames.StartCheckpoint
            : value.Trim();
    }

    private static float RoundSeconds(float value)
    {
        return Mathf.Round(Mathf.Max(0f, value) * 1000f) / 1000f;
    }

    private static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
