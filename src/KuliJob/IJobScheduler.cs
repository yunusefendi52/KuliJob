using System.Linq.Expressions;

namespace KuliJob;

public partial interface IJobScheduler
{
    Task IsStarted { get; }
    Task<string> ScheduleJobNow<T>(JobDataMap data, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJobNow(typeof(T).Name, data, scheduleOptions);
    }
    Task<string> ScheduleJob<T>(JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(typeof(T).Name, data, startAfter, scheduleOptions);
    }
    Task<string> ScheduleJobNow(string jobName, JobDataMap data, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(jobName, data, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task<string> ScheduleJob(string jobName, JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJob(Expression<Action> expression, JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobNow(Expression<Action> expression, JobDataMap data, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(expression, data, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task<string> ScheduleJob(Expression<Func<Task>> expression, JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobNow(Expression<Func<Task>> expression, JobDataMap data, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(expression, data, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task<string> ScheduleJob<T>(Expression<Func<T, Task>> expression, JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobNow<T>(Expression<Func<T, Task>> expression, JobDataMap data, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(expression, data, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task CancelJob(string jobId);
    Task ResumeJob(string jobId);
}

public struct ScheduleOptions
{
    public int RetryMaxCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
}
