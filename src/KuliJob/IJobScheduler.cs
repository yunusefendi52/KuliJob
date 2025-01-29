using System.Linq.Expressions;

namespace KuliJob;

public partial interface IJobScheduler
{
    Task IsStarted { get; }
    Task<string> ScheduleJobNow<T>(JobDataMap? data = null, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJobNow(typeof(T).Name, data, scheduleOptions);
    }
    Task<string> ScheduleJob<T>(DateTimeOffset startAfter, JobDataMap? data = null, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(typeof(T).Name, startAfter, data, scheduleOptions);
    }
    Task<string> ScheduleJobNow(string jobName, JobDataMap? data = null, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(jobName, DateTimeOffset.UtcNow, data, scheduleOptions);
    }
    Task<string> ScheduleJob(string jobName, DateTimeOffset startAfter, JobDataMap? data = null, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJob(Expression<Action> expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobNow(Expression<Action> expression, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(expression, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task<string> ScheduleJob(Expression<Func<Task>> expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobNow(Expression<Func<Task>> expression, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(expression, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task<string> ScheduleJob<T>(Expression<Func<T, Task>> expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobNow<T>(Expression<Func<T, Task>> expression, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(expression, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task CancelJob(string jobId);
    Task ResumeJob(string jobId);
}

public struct ScheduleOptions
{
    public int RetryMaxCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    public string Queue { get; set; }
}
