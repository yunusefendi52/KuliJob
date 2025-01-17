using System.Diagnostics;
using System.Text.Json;
using KuliJob.Storages;

namespace KuliJob;

public class JobContext
{
    public required IServiceProvider Services { get; init; }
    public required string JobName { get; set; } = null!;
    public required JobDataMap JobData { get; set; } = null!;
}

public class JobDataMap : Dictionary<string, object>
{
    public string GetString(string key)
    {
        return ((JsonElement)this[key]).GetString() ?? throw new ArgumentException($"Key {key} is not a string");
    }

    public int GetInt(string key)
    {
        return ((JsonElement)this[key]).GetInt32();
    }

    public long GetLong(string key)
    {
        return ((JsonElement)this[key]).GetInt64();
    }
    
    public double GetDouble(string key)
    {
        return ((JsonElement)this[key]).TryGetDouble(out var value) ? value : throw new ArgumentException($"Key {key} is not a double");
    }

    public DateTimeOffset GetDateTimeOffset(string key)
    {
        return ((JsonElement)this[key]).GetDateTimeOffset();
    }

    public DateTime GetDateTime(string key)
    {
        return ((JsonElement)this[key]).GetDateTime();
    }

    public bool GetBool(string key)
    {
        return ((JsonElement)this[key]).GetBoolean();
    }

    public T? Get<T>(string key)
    {
        return ((JsonElement)this[key]).Deserialize<T>();
    }
}

public class JobConfiguration
{
    public int Worker { get; set; } = 10;
    public int MinPollingIntervalMs { get; set; } = 500;
    public int JobTimeoutMs { get; set; } = 15_000;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IServiceCollection ServiceCollection { get; init; } = null!;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal bool IsTest { get; set; }
}

public class JobServerScheduler(
    ILogger<JobServerScheduler> logger,
    IServiceProvider serviceProvider,
    IJobStorage storage,
    JobConfiguration configuration,
    [FromKeyedServices("kulijob_timeprovider")] TimeProvider timeProvider) : IJobScheduler, IDisposable
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

    public async Task<string> ScheduleJob(string jobName, JobDataMap data, DateTimeOffset startAfter)
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

    private async ValueTask ProcessJob((JobInput JobInput, bool Success) value, CancellationToken cancellationToken)
    {
        var (jobInput, fetched) = value;
        if (!fetched)
        {
            logger.LogError("Error update fetched job {jobId} {jobName}", jobInput.Id, jobInput.JobName);
            return;
        }

        try
        {
            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(configuration.JobTimeoutMs), timeProvider);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
            cts.Token.ThrowIfCancellationRequested();
            await using var serviceSCope = serviceProvider.CreateAsyncScope();
            var sp = serviceSCope.ServiceProvider;
            var jobHandler = sp.GetKeyedService<IJob>($"kulijob.{jobInput.JobName}") ?? throw new ArgumentException($"No handler registered for job type {jobInput.JobName}. Call {nameof(JobExtensions.AddKuliJob)} to register job");
            var jobDataMap = JsonSerializer.Deserialize<JobDataMap>(jobInput.JobData);
            var jobContext = new JobContext
            {
                Services = sp,
                JobName = jobInput.JobName,
                JobData = jobDataMap!,
            };
            await jobHandler.Execute(jobContext, cts.Token);
            await storage.CompleteJobById(jobInput);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job error {jobId} {jobName}", jobInput.Id, jobInput.JobName);
            await storage.FailJobById(jobInput, ex.Message);
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
