namespace KuliJob;

public interface IJobScheduler
{
    Task IsStarted { get; }
    Task<string> ScheduleJob(string jobName, string data);
}
