namespace KuliJob.Storages;

public interface IJobStorage : IAsyncDisposable
{
    // Task<JobInput?> GetJobByState(Guid jobId, JobState jobState);
    Task<Job?> GetJobById(Guid jobId);
    Task<Job?> GetJobByThrottle(string throttleKey);
    Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null);
    Task CancelJobById(Guid jobId);
    Task CompleteJobById(Guid jobId);
    Task FailJobById(Guid jobId, string failedMessage);
    Task<Job?> FetchNextJob(Guid? nextId, CancellationToken cancellationToken = default);
    Task InsertJob(Job jobInput);
    Task StartStorage(CancellationToken cancellationToken = default);
    Task ResumeJob(Guid jobId);
    Task RetryJob(Guid jobId, int retryDelay);
    Task<List<JobStateEntry>?> GetJobStates(Guid jobId);

    Task<List<JobServerEntry>> GetJobServers();
    Task RemoveInactiveServers();
    Task UpdateHeartbeatServer();

    Task AddOrUpdateCron(Cron cron);
    Task<IEnumerable<Cron>> GetCrons();
    Task DeleteCron(string name);

    event EventHandler<Guid>? NextJobIdNotifier;
}
