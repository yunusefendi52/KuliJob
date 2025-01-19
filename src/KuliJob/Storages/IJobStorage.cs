namespace KuliJob.Storages;

public interface IJobStorage
{
    // Task<JobInput?> GetJobByState(string jobId, JobState jobState);
    Task<JobInput?> GetJobById(string jobId);
    Task<IEnumerable<JobInput>> GetLatestJobs(int page, int limit, JobState? jobState = null);
    Task CancelJobById(string jobId);
    Task CompleteJobById(JobInput jobInput);
    Task FailJobById(JobInput jobInput, string failedMessage);
    IAsyncEnumerable<JobInput> FetchNextJob(CancellationToken cancellationToken = default);
    Task InsertJob(JobInput jobInput);
    Task StartStorage();
    Task ResumeJob(string jobId);
    Task<JobInput> RetryJob(string jobId, int retryDelay);
}
