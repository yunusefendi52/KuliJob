namespace KuliJob;

public interface IJobScheduler
{
    Task IsStarted { get; }
    Task<string> ScheduleJobNow(string jobName, JobDataMap data)
    {
        return ScheduleJob(jobName, data, DateTimeOffset.UtcNow);
    }
    Task<string> ScheduleJob(string jobName, JobDataMap data, DateTimeOffset startAfter);
    Task CancelJob(string jobId);
    Task ResumeJob(string jobId);
}
