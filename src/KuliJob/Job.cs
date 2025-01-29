namespace KuliJob;

public enum JobState
{
    Created,
    Retry,
    Active,
    Completed,
    Cancelled,
    Failed,
}

public class Job
{
    public Job()
    {
        var now = DateTimeOffset.UtcNow;
        StartAfter = now;
        CreatedOn = now;
        Queue = "default";
    }

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string JobName { get; set; } = null!;
    public string JobData { get; set; } = null!;
    public JobState JobState { get; set; } = JobState.Created;
    public DateTimeOffset StartAfter { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public DateTimeOffset? CancelledOn { get; set; }
    public DateTimeOffset? FailedOn { get; set; }
    public string? FailedMessage { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int RetryMaxCount { get; set; } = 2;
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    public string? Queue { get; set; }
}