using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using KuliJob.Storages;

namespace KuliJob;

public class JobConfiguration
{
    public int Worker { get; set; } = Environment.ProcessorCount * 2;
    public int MinPollingIntervalMs { get; set; } = 500;
    public int JobTimeoutMs { get; set; } = 60 * 16_000;
    /// <summary>
    /// Specify job which queue execute on, default queue is "default"
    /// </summary>
    public string[] Queues { get; set; } = ["default"];

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IServiceCollection ServiceCollection { get; init; } = null!;
}

internal class JobServerScheduler(
    ILogger<JobServerScheduler> logger,
    IServiceProvider serviceProvider,
    IJobStorage storage,
    JobConfiguration configuration,
    [FromKeyedServices("kulijob_timeprovider")] TimeProvider timeProvider,
    Serializer serializer,
    ExpressionSerializer expressionSerializer) : IJobScheduler, IAsyncDisposable
{
    readonly CancellationTokenSource cancellation = new();

    readonly TaskCompletionSource isStarted = new();

    public Task IsStarted => isStarted.Task;

    public async Task Start()
    {
        await storage.StartStorage(cancellation.Token);
        logger.LogInformation("🔄 Job scheduler started");
        isStarted.SetResult(); ;
        await ProcessQueueAsync(cancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await cancellation.CancelAsync();
    }

    public async Task<string> ScheduleJob(string jobName, DateTimeOffset startAfter, JobDataMap? data = null, ScheduleOptions? scheduleOptions = null)
    {
        var jobData = serializer.Serialize(data);
        var jobInput = new Job
        {
            JobName = jobName,
            JobData = jobData,
            StartAfter = startAfter,
            RetryMaxCount = scheduleOptions.HasValue ? scheduleOptions.Value.RetryMaxCount : 0,
            RetryDelayMs = scheduleOptions.HasValue ? scheduleOptions.Value.RetryDelayMs : 0,
            Priority = scheduleOptions.HasValue ? scheduleOptions.Value.Priority : 0,
            Queue = string.IsNullOrWhiteSpace(scheduleOptions?.Queue) ? "default" : scheduleOptions.Value.Queue,
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

    private async ValueTask ProcessJob(Job jobInput, CancellationToken cancellationToken)
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
            await storage.CompleteJobById(jobInput.Id);
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
                await storage.FailJobById(jobInput.Id, ex.Message);
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

    public Task<string> ScheduleJob(Expression<Action> expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleFromExpr(expression, startAfter, scheduleOptions);
    }

    public Task<string> ScheduleJob(Expression<Func<Task>> expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleFromExpr(expression, startAfter, scheduleOptions);
    }

    public Task<string> ScheduleJob<T>(Expression<Func<T, Task>> expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null)
    {
        return ScheduleFromExpr(expression, startAfter, scheduleOptions);
    }

    async Task<string> ScheduleFromExpr(LambdaExpression expression, DateTimeOffset startAfter, ScheduleOptions? scheduleOptions = null)
    {
        var methodArg = expressionSerializer.FromExprToObject(expression)!;
        return await ScheduleJob("expr_job", startAfter, new JobDataMap
        {
            { "k_type", methodArg.DeclType },
            { "k_methodName", methodArg.MethodName },
            { "k_args", methodArg.Arguments! },
        }, scheduleOptions);
    }
}
