using SQLite;

namespace KuliJob.Sqlite;

[Table("job")]
internal class SqliteJobInput
{
    [PrimaryKey]
    public string Id { get; set; } = null!;
    [NotNull]
    [Indexed]
    public string JobName { get; set; } = null!;
    [NotNull]
    public string JobData { get; set; } = null!;
    [Indexed]
    public JobState JobState { get; set; }
    [Indexed]
    public DateTimeOffset StartAfter { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public DateTimeOffset? CancelledOn { get; set; }
    public DateTimeOffset? FailedOn { get; set; }
    public string? FailedMessage { get; set; }
    [Indexed]
    public DateTimeOffset CreatedOn { get; set; }
    public int RetryMaxCount { get; set; }
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    [MaxLength(32)]
    public string? Queue { get; set; }
    public string? ServerName { get; set; }
}

internal static class JobInputMapper
{
    public static Job ToJobInput(this SqliteJobInput sqliteJobInput)
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
        };
    }

    public static SqliteJobInput ToSqliteJobInput(this Job jobInput)
    {
        return new SqliteJobInput
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
        };
    }
}
