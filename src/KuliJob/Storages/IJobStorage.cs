namespace KuliJob.Storages;

public interface IJobStorage : IAsyncDisposable
{
    // Task<JobInput?> GetJobByState(string jobId, JobState jobState);
    Task<Job?> GetJobById(string jobId);
    Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null);
    Task CancelJobById(string jobId);
    Task CompleteJobById(string jobId);
    Task FailJobById(string jobId, string failedMessage);
    Task<Job?> FetchNextJob(CancellationToken cancellationToken = default);
    Task InsertJob(Job jobInput);
    Task StartStorage(CancellationToken cancellationToken = default);
    Task ResumeJob(string jobId);
    Task<Job> RetryJob(string jobId, int retryDelay);
}
