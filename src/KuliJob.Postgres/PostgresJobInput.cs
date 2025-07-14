namespace KuliJob.Postgres;

internal class PostgresJobInput
{
    public Guid id { get; set; }
    public string job_name { get; set; } = null!;
    public string job_data { get; set; } = null!;
    public required Guid job_state_id { get; set; }
    public required JobState job_state { get; set; }
    public DateTimeOffset start_after { get; set; }
    public DateTimeOffset created_on { get; set; }
    public string? state_message { get; set; }
    public DateTimeOffset state_created_at { get; set; }
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
            Id = id,
            JobName = job_name,
            JobState = job_state,
            JobData = job_data,
            CreatedOn = created_on,
            StartAfter = start_after,
            JobStateId = job_state_id,
            StateMessage = state_message,
            StateCreatedAt = state_created_at,
            RetryCount = retry_count,
            RetryDelayMs = retry_delay,
            RetryMaxCount = retry_max_count,
            Priority = priority,
            Queue = queue,
            ServerName = server_name,
        };
    }
}
