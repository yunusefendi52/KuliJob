namespace KuliJob;

public partial interface IQueueJob
{
    Task IsStarted { get; }
    Task<Guid> Enqueue<T>(QueueOptions? queueOptions = null)
    {
        return Enqueue(typeof(T).Name, null, queueOptions);
    }
    Task<Guid> Enqueue<T>(JobDataMap? data = null, QueueOptions? queueOptions = null)
    {
        return Enqueue(typeof(T).Name, data, queueOptions);
    }
    Task<Guid> Enqueue<T>(DateTimeOffset startAfter, JobDataMap? data = null, QueueOptions? queueOptions = null)
    {
        return Enqueue(typeof(T).Name, startAfter, data, queueOptions);
    }
    Task<Guid> Enqueue(string jobName, JobDataMap? data = null, QueueOptions? queueOptions = null)
    {
        return Enqueue(jobName, DateTimeOffset.UtcNow, data, queueOptions);
    }
    Task<Guid> Enqueue(string jobName, DateTimeOffset startAfter, JobDataMap? data = null, QueueOptions? queueOptions = null);
    Task CancelJob(Guid jobId);
    Task ResumeJob(Guid jobId);
}

public struct QueueOptions
{
    public int RetryMaxCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    public string Queue { get; set; }
    public string? ThrottleKey { get; set; }
    public TimeSpan? ThrottleTime { get; set; }
}
