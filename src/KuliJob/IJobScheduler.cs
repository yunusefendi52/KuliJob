using System.Linq.Expressions;

namespace KuliJob;

public interface IJobScheduler
{
    Task IsStarted { get; }
    Task<string> ScheduleJobNow(string jobName, JobDataMap data, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleJob(jobName, data, DateTimeOffset.UtcNow, scheduleOptions);
    }
    Task<string> ScheduleJob(string jobName, JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null);
    Task<string> ScheduleJobMethod(Expression<Func<Task>> methodExpression);
    Task CancelJob(string jobId);
    Task ResumeJob(string jobId);
}

public struct ScheduleOptions
{
    public int RetryMaxCount { get; set; }
    public int RetryDelayMs { get; set; }
}
