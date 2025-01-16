using System.Text.Json;
using KuliJob.Storages;

namespace KuliJob;

public class JobContext
{
    public required IServiceProvider Services { get; init; }
    public required string JobName { get; set; } = null!;
    public required string JobData { get; set; } = null!;
}

public interface IJob
{
    Task Execute(JobContext context, CancellationToken cancellationToken = default);
}

public class JobConfiguration
{
    public int Worker { get; set; } = 10;
    public bool UseSqlite { get; set; }
    public int MinPollingIntervalMs { get; set; } = 500;
}

public class JobServerScheduler(
    ILogger<JobServerScheduler> logger,
    IServiceProvider serviceProvider,
    IJobStorage storage,
    JobConfiguration configuration) : IJobScheduler, IDisposable
{
    readonly CancellationTokenSource cancellation = new();

    readonly TaskCompletionSource isStarted = new();

    public Task IsStarted => isStarted.Task;

    public async Task Start()
    {
        await storage.StartStorage();
        logger.LogInformation("🔄 Job scheduler started");
        isStarted.SetResult(); ;
        await ProcessQueueAsync(cancellation.Token);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        cancellation.Dispose();
    }

    public async Task<string> ScheduleJob<T>(string jobName, T data, DateTimeOffset startAfter)
    {
        var jobData = JsonSerializer.Serialize(data);
        var jobInput = new JobInput
        {
            JobName = jobName,
            JobData = jobData,
            StartAfter = startAfter,
        };
        await storage.InsertJob(jobInput);
        return jobInput.Id;
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Parallel.ForEachAsync(storage.FetchNextJob(cancellation.Token), new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = configuration.Worker,
                }, ProcessJob);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error process job");
            }
        }
    }

    private async ValueTask ProcessJob((JobInput JobInput, bool Success) value, CancellationToken token)
    {
        var (jobInput, fetched) = value;
        if (!fetched)
        {
            logger.LogError("Error update fetched job {jobId} {jobName}", jobInput.Id, jobInput.JobName);
            return;
        }

        var jobHandler = serviceProvider.GetKeyedService<IJob>(jobInput.JobName);
        if (jobHandler is not null)
        {
            try
            {
                using var ctsToken = new CancellationTokenSource(TimeSpan.FromMinutes(15));
                await using var sp = serviceProvider.CreateAsyncScope();
                var jobContext = new JobContext
                {
                    Services = sp.ServiceProvider,
                    JobName = jobInput.JobName,
                    JobData = jobInput.JobData,
                };
                await jobHandler.Execute(jobContext, ctsToken.Token);
                await storage.CompleteJobById(jobInput);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job error {jobId} {jobName}", jobInput.Id, jobInput.JobName);
                await storage.FailJobById(jobInput, ex.Message);
            }
        }
        else
        {
            throw new ArgumentException($"No handler registered for job type {jobInput.JobName}. Call {nameof(JobExtensions.AddKuliJob)} to register job");
        }
    }
}
