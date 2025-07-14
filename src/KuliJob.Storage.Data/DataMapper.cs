namespace KuliJob.Storage.Data;

internal static class DataMapper
{
    public static Job ToJobData(
        this JobInputDb jobInput,
        string? stateMessage = null,
        DateTimeOffset? stateCreatedAt = null)
    {
        return new()
        {
            Id = jobInput.Id,
            JobName = jobInput.JobName,
            JobState = jobInput.JobState,
            JobData = jobInput.JobData,
            CreatedOn = jobInput.CreatedOn,
            StartAfter = jobInput.StartAfter,
            JobStateId = jobInput.JobStateId,
            StateMessage = stateMessage,
            StateCreatedAt = stateCreatedAt ?? DateTimeOffset.MinValue,
            RetryCount = jobInput.RetryCount,
            RetryDelayMs = jobInput.RetryDelayMs,
            RetryMaxCount = jobInput.RetryMaxCount,
            Priority = jobInput.Priority,
            Queue = jobInput.Queue,
            ServerName = jobInput.ServerName,
        };
    }
}

