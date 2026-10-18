using System;

[Serializable]
public sealed class AnalyticsEvent
{
    public int schema_version = 1;
    public string environment;
    public string client_timestamp;
    public string event_id;
    public string event_type;
    public string player_id;
    public string session_id;
    public string level_run_id;
    public string attempt_id;
    public string level_name;
    public string checkpoint_id;
    public string controlled_form;
    public string previous_form;
    public string death_cause;
    public float position_x;
    public float position_y;
    public bool has_position;
    public float duration_seconds;
    public float attempt_active_seconds;
    public float level_active_seconds;
    public int death_count;
    public string start_reason;
    public string end_reason;
    public string result;
    public string build_version;
    public string details;
}
