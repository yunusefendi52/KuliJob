using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using KuliJob.Storages;

namespace KuliJob;

public class JobConfiguration
{
    public int Worker { get; set; } = Environment.ProcessorCount * 2;
    public int MinPollingIntervalMs { get; set; } = 500;
    public int JobTimeoutMs { get; set; } = 60 * 16_000;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IServiceCollection ServiceCollection { get; init; } = null!;
}

public class JobServerScheduler(
    ILogger<JobServerScheduler> logger,
    IServiceProvider serviceProvider,
    IJobStorage storage,
    JobConfiguration configuration,
    [FromKeyedServices("kulijob_timeprovider")] TimeProvider timeProvider,
    Serializer serializer) : IJobScheduler, IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await cancellation.CancelAsync();
    }

    public async Task<string> ScheduleJob(string jobName, JobDataMap data, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null)
    {
        var jobData = serializer.Serialize(data);
        var jobInput = new JobInput
        {
            JobName = jobName,
            JobData = jobData,
            StartAfter = startAfter,
            RetryMaxCount = scheduleOptions.HasValue ? scheduleOptions.Value.RetryMaxCount : 0,
            RetryDelayMs = scheduleOptions.HasValue ? scheduleOptions.Value.RetryDelayMs : 0,
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

    private async ValueTask ProcessJob(JobInput jobInput, CancellationToken cancellationToken)
    {
    RETRY:
        try
        {
            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(configuration.JobTimeoutMs), timeProvider);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
            cts.Token.ThrowIfCancellationRequested();
            await using var serviceSCope = serviceProvider.CreateAsyncScope();
            var sp = serviceSCope.ServiceProvider;
            var jobHandler = sp.GetKeyedService<IJob>($"kulijob.{jobInput.JobName}") ?? throw new ArgumentException($"No handler registered for job type {jobInput.JobName}. Call {nameof(JobExtensions.AddKuliJob)} to register job");
            var jobDataMap = serializer.Deserialize<JobDataMap>(jobInput.JobData);
            var jobContext = new JobContext
            {
                Services = sp,
                JobName = jobInput.JobName,
                JobData = jobDataMap!,
                RetryCount = jobInput.RetryCount,
            };
            await jobHandler.Execute(jobContext, cts.Token);
            await storage.CompleteJobById(jobInput);
        }
        catch (Exception ex)
        {
            if (jobInput.RetryMaxCount > 0 && jobInput.RetryCount < jobInput.RetryMaxCount)
            {
                logger.LogError(ex, "Job error, will retry {jobId} {jobName} current retry {retryCount} max {retryMaxCount}", jobInput.Id, jobInput.JobName, jobInput.RetryCount, jobInput.RetryMaxCount);
                var retriedJobInput = await storage.RetryJob(jobInput.Id, jobInput.RetryDelayMs);
                if (jobInput.RetryDelayMs == 0)
                {
                    jobInput = retriedJobInput;
                    goto RETRY;
                }
            }
            else
            {
                logger.LogError(ex, "Job error {jobId} {jobName}", jobInput.Id, jobInput.JobName);
                await storage.FailJobById(jobInput, ex.Message);
            }
        }
    }

    public async Task CancelJob(string jobId)
    {
        await storage.CancelJobById(jobId);
    }

    public async Task ResumeJob(string jobId)
    {
        await storage.ResumeJob(jobId);
    }
}
