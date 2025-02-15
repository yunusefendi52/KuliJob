using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KuliJob.Sqlite;

[Table("job")]
[Index(nameof(JobName))]
[Index(nameof(JobState))]
[Index(nameof(StartAfter))]
[Index(nameof(CreatedOn))]
[Index(nameof(ThrottleKey))]
internal class JobInputDb
{
    public string Id { get; set; } = null!;
    public string JobName { get; set; } = null!;
    public string JobData { get; set; } = null!;
    public JobState JobState { get; set; }
    public DateTimeOffset StartAfter { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public DateTimeOffset? CancelledOn { get; set; }
    public DateTimeOffset? FailedOn { get; set; }
    public string? FailedMessage { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int RetryMaxCount { get; set; }
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    public string? Queue { get; set; }
    public string? ServerName { get; set; }
    public string? ThrottleKey { get; set; }
    public int ThrottleSeconds { get; set; }
}

internal static class JobInputMapper
{
    public static Job ToJobInput(this JobInputDb sqliteJobInput)
    {
        return new Job
        {
            Id = sqliteJobInput.Id,
            JobName = sqliteJobInput.JobName,
            JobData = sqliteJobInput.JobData,
            JobState = sqliteJobInput.JobState,
            StartAfter = sqliteJobInput.StartAfter,
            StartedOn = sqliteJobInput.StartedOn,
            CompletedOn = sqliteJobInput.CompletedOn,
            CancelledOn = sqliteJobInput.CancelledOn,
            FailedOn = sqliteJobInput.FailedOn,
            FailedMessage = sqliteJobInput.FailedMessage,
            CreatedOn = sqliteJobInput.CreatedOn,
            RetryCount = sqliteJobInput.RetryCount,
            RetryDelayMs = sqliteJobInput.RetryDelayMs,
            RetryMaxCount = sqliteJobInput.RetryMaxCount,
            Priority = sqliteJobInput.Priority,
            Queue = sqliteJobInput.Queue,
            ServerName = sqliteJobInput.ServerName,
            ThrottleKey = sqliteJobInput.ThrottleKey,
            ThrottleSeconds = sqliteJobInput.ThrottleSeconds,
        };
    }

    public static JobInputDb ToSqliteJobInput(this Job jobInput)
    {
        return new JobInputDb
        {
            Id = jobInput.Id,
            JobName = jobInput.JobName,
            JobData = jobInput.JobData,
            JobState = jobInput.JobState,
            StartAfter = jobInput.StartAfter,
            StartedOn = jobInput.StartedOn,
            CompletedOn = jobInput.CompletedOn,
            CancelledOn = jobInput.CancelledOn,
            FailedOn = jobInput.FailedOn,
            FailedMessage = jobInput.FailedMessage,
            CreatedOn = jobInput.CreatedOn,
            RetryCount = jobInput.RetryCount,
            RetryDelayMs = jobInput.RetryDelayMs,
            RetryMaxCount = jobInput.RetryMaxCount,
            Priority = jobInput.Priority,
            Queue = jobInput.Queue,
            ServerName = jobInput.ServerName,
            ThrottleKey = jobInput.ThrottleKey,
            ThrottleSeconds = jobInput.ThrottleSeconds,
        };
    }
}
