namespace KuliJob.Storages;

public interface IJobStorage
{
    Task<JobInput?> GetJobByState(string jobId, JobState jobState);
    Task CancelJobById(string jobId);
    Task CompleteJobById(JobInput jobInput);
    Task FailJobById(JobInput jobInput, string failedMessage);
    IAsyncEnumerable<(JobInput JobInput, bool Success)> FetchNextJob(CancellationToken cancellationToken = default);
    Task InsertJob(JobInput jobInput);
    Task StartStorage();
    Task ResumeJob(string jobId);
}
