using System.Linq.Expressions;

namespace KuliJob;

public partial interface IQueueJob
{
    Task IsStarted { get; }
    Task<string> Enqueue<T>(JobDataMap? data = null, QueueOptions? queueOptions = null)
    {
        return Enqueue(typeof(T).Name, data, queueOptions);
    }
    Task<string> Enqueue<T>(DateTimeOffset startAfter, JobDataMap? data = null, QueueOptions? queueOptions = null)
    {
        return Enqueue(typeof(T).Name, startAfter, data, queueOptions);
    }
    Task<string> Enqueue(string jobName, JobDataMap? data = null, QueueOptions? queueOptions = null)
    {
        return Enqueue(jobName, DateTimeOffset.UtcNow, data, queueOptions);
    }
    Task<string> Enqueue(string jobName, DateTimeOffset startAfter, JobDataMap? data = null, QueueOptions? queueOptions = null);
    Task<string> Enqueue(Expression<Action> expression, DateTimeOffset startAfter, QueueOptions? queueOptions = null);
    Task<string> Enqueue(Expression<Action> expression, QueueOptions? queueOptions = null)
    {
        return Enqueue(expression, DateTimeOffset.UtcNow, queueOptions);
    }
    Task<string> Enqueue(Expression<Func<Task>> expression, DateTimeOffset startAfter, QueueOptions? queueOptions = null);
    Task<string> Enqueue(Expression<Func<Task>> expression, QueueOptions? queueOptions = null)
    {
        return Enqueue(expression, DateTimeOffset.UtcNow, queueOptions);
    }
    Task<string> Enqueue<T>(Expression<Func<T, Task>> expression, DateTimeOffset startAfter, QueueOptions? queueOptions = null);
    Task<string> Enqueue<T>(Expression<Func<T, Task>> expression, QueueOptions? queueOptions = null)
    {
        return Enqueue(expression, DateTimeOffset.UtcNow, queueOptions);
    }
    Task CancelJob(string jobId);
    Task ResumeJob(string jobId);
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
