public enum AnalyticsDeathCause
{
    Unknown,
    Spike,
    UnderwaterHazard,
    MovingSpike,
    SpikeWall,
    OutOfBoundsLow,
    OutOfBoundsHigh,
    Other
}

public static class AnalyticsValueNames
{
    public const string StartCheckpoint = "START";

    public static string DeathCause(AnalyticsDeathCause cause)
    {
        return cause.ToString();
    }
}
