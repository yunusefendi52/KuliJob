namespace KuliJob.Postgres;

internal class PostgresJobInput
{
    public Guid id { get; set; }
    public string name { get; set; } = null!;
    public string data { get; set; } = null!;
    public JobState state { get; set; }
    public DateTimeOffset start_after { get; set; }
    public DateTimeOffset? started_on { get; set; }
    public DateTimeOffset? completed_on { get; set; }
    public DateTimeOffset? cancelled_on { get; set; }
    public DateTimeOffset? failed_on { get; set; }
    public string? failed_message { get; set; }
    public DateTimeOffset created_on { get; set; }
    public int retry_max_count { get; set; }
    public int retry_count { get; set; }
    public int retry_delay { get; set; }
    public short priority { get; set; }
    public string? queue { get; set; }
    public string? server_name { get; set; }

    public Job ToJobInput()
    {
        return new()
        {
            Id = id.ToString(),
            JobName = name,
            JobState = state,
            JobData = data,
            CancelledOn = cancelled_on,
            CompletedOn = completed_on,
            CreatedOn = created_on,
            FailedMessage = failed_message,
            FailedOn = failed_on,
            RetryCount = retry_count,
            RetryDelayMs = retry_delay,
            RetryMaxCount = retry_max_count,
            StartAfter = start_after,
            StartedOn = started_on,
            Priority = priority,
            Queue = queue,
            ServerName = server_name,
        };
    }
}
