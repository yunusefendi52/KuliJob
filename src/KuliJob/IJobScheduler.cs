namespace KuliJob;

public interface IJobScheduler
{
    Task IsStarted { get; }
    Task<string> ScheduleJobNow<T>(string jobName, T data)
    {
        return ScheduleJob(jobName, data, DateTimeOffset.UtcNow);
    }
    Task<string> ScheduleJob<T>(string jobName, T data, DateTimeOffset startAfter);
}
