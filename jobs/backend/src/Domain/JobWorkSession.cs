namespace HotelOS.Jobs.Domain;

/// <summary>
/// One stretch of work on a job — S4: start, pause, resume, stop. PAUSED lives
/// here and is never a job status (S2 D2). The timer on the Work tab counts the
/// running row.
/// </summary>
public class JobWorkSession
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? PausedAt { get; set; }

    public string? PauseReason { get; set; }

    public DateTimeOffset? ResumedAt { get; set; }

    public DateTimeOffset? StoppedAt { get; set; }

    /// <summary>Seconds actually worked, excluding the pause; final once stopped.</summary>
    public long WorkedSeconds { get; set; }

    public bool IsRunning => StoppedAt is null && (PausedAt is null || ResumedAt is not null);

    public bool IsPaused => StoppedAt is null && PausedAt is not null && ResumedAt is null;

    /// <summary>End the session now, keeping what was worked (the pause excluded).</summary>
    public void Stop(DateTimeOffset now)
    {
        WorkedSeconds = WorkedSecondsAt(now);
        StoppedAt = now;
    }

    /// <summary>Worked time as of <paramref name="now"/>, live for a running row.</summary>
    public long WorkedSecondsAt(DateTimeOffset now)
    {
        if (StoppedAt is not null) return WorkedSeconds;

        var end = IsPaused ? PausedAt!.Value : now;
        var paused = PausedAt is { } p && ResumedAt is { } r ? (r - p).TotalSeconds : 0;
        return (long)Math.Max(0, (end - StartedAt).TotalSeconds - paused);
    }
}
