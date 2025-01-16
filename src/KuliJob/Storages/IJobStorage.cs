using System.Runtime.CompilerServices;

namespace KuliJob.Storages;

public interface IJobStorage
{
    Task<JobInput?> GetCompletedJobById(string jobId);
    Task CancelJobById(JobInput jobInput);
    Task CompleteJobById(JobInput jobInput);
    Task FailJobById(JobInput jobInput, string failedMessage);
    IAsyncEnumerable<(JobInput JobInput, bool Success)> FetchNextJob(CancellationToken cancellationToken = default);
    Task InsertJob(JobInput jobInput);
    Task StartStorage();
}
