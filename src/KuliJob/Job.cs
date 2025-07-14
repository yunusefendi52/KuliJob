using System.ComponentModel.DataAnnotations.Schema;

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

    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobName { get; set; } = null!;
    public string? JobData { get; set; } = null!;
    public Guid? JobStateId { get; set; }
    public JobState JobState { get; set; } = JobState.Created;
    public DateTimeOffset StartAfter { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public DateTimeOffset? CancelledOn { get; set; }
    public DateTimeOffset? FailedOn { get; set; }
    public string? StateMessage { get; set; }
    public DateTimeOffset StateCreatedAt { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int RetryMaxCount { get; set; } = 2;
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    public string? Queue { get; set; }
    public string? ServerName { get; set; }
    public string? ThrottleKey { get; set; }
    public int ThrottleSeconds { get; set; }
}

public class JobStateEntry
{
    public required Guid Id { get; set; }
    public required Guid JobId { get; set; }
    // internal JobInputDb? JobInputDb { get; set; }
    public JobState JobState { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

[Table("server")]
public class JobServerEntry
{
    public required string Id { get; set; }
    [Column(TypeName = "jsonb")]
    public string? Data { get; set; }
    public DateTimeOffset LastHeartbeat { get; set; }
}
