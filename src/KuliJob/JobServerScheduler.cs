using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using KuliJob.Storages;

namespace KuliJob;

internal class JobServerScheduler(
    ILogger<JobServerScheduler> logger,
    IServiceScopeFactory serviceScopeFactory,
    IJobStorage storage,
    JobConfiguration configuration,
    Serializer serializer,
    ExpressionSerializer expressionSerializer,
    CronJobHostedService cronJobHostedService) : IQueueJob, IAsyncDisposable
{
    readonly CancellationTokenSource cancellation = new();

    readonly TaskCompletionSource isStarted = new();

    public Task IsStarted => isStarted.Task;

    public async Task Start()
    {
        await storage.StartStorage(cancellation.Token);
        logger.LogInformation($"""
        🔄 Job scheduler started. Configuration:
            Server name: {configuration.ServerName}
            Worker: {configuration.Worker}
            Queues: {string.Join(", ", configuration.Queues)}
            Min job polling: {configuration.MinPollingIntervalMs} ms
            Min cron polling: {configuration.MinCronPollingIntervalMs} ms
        """);
        isStarted.SetResult();
        await Task.WhenAll(cronJobHostedService.ProcessCron(cancellation.Token), ProcessQueueAsync(cancellation.Token));
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        cancellation.Cancel();
        return ValueTask.CompletedTask;
    }

    public async Task<string> Enqueue(string jobName, DateTimeOffset startAfter, JobDataMap? data = null, QueueOptions? scheduleOptions = null)
    {
        var jobData = serializer.Serialize(data);
        var throttleKey = scheduleOptions.HasValue ? scheduleOptions.Value.ThrottleKey : null;
        var throttleTime = scheduleOptions.HasValue && scheduleOptions.Value.ThrottleTime.HasValue ? scheduleOptions.Value.ThrottleTime.Value : TimeSpan.Zero;
        var jobInput = new Job
        {
            JobName = jobName,
            JobData = jobData,
            StartAfter = startAfter,
            RetryMaxCount = scheduleOptions.HasValue ? scheduleOptions.Value.RetryMaxCount : 0,
            RetryDelayMs = scheduleOptions.HasValue ? scheduleOptions.Value.RetryDelayMs : 0,
            Priority = scheduleOptions.HasValue ? scheduleOptions.Value.Priority : 0,
            Queue = string.IsNullOrWhiteSpace(scheduleOptions?.Queue) ? "default" : scheduleOptions.Value.Queue,
            ThrottleKey = throttleKey,
            ThrottleSeconds = (int)throttleTime.TotalSeconds,
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
                await Parallel.ForEachAsync(FetchLoop(cancellation.Token), new ParallelOptions
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

    async IAsyncEnumerable<Job> FetchLoop([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (true)
            {
                var nextJob = await storage.FetchNextJob(cancellationToken);
                if (nextJob is null)
                {
                    break;
                }

                yield return nextJob;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(configuration.MinPollingIntervalMs), cancellationToken);
        }
    }

    private async ValueTask ProcessJob(Job jobInput, CancellationToken cancellationToken)
    {
    RETRY:
        try
        {
            using var timeoutCancellation = new CancellationTokenSource();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
            cts.Token.ThrowIfCancellationRequested();
            await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
            var sp = serviceScope.ServiceProvider;
            var jobFactory = sp.GetRequiredService<JobFactory>();
            var jobHandler = jobFactory.ResolveService(sp, jobInput.JobName) ?? throw new ArgumentException($"No handler registered for job type {jobInput.JobName}. Call {nameof(JobExtensions.AddKuliJob)} to register job");
            var jobDataMap = serializer.Deserialize<JobDataMap>(jobInput.JobData);
            var jobContext = new JobContext
            {
                Services = sp,
                JobName = jobInput.JobName,
                JobData = jobDataMap!,
                RetryCount = jobInput.RetryCount,
            };
            await jobHandler.Execute(jobContext);
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

    public Task<string> Enqueue(Expression<Action> expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        return EnqueueFromExpr(expression, startAfter, scheduleOptions);
    }

    public Task<string> Enqueue(Expression<Func<Task>> expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        return EnqueueFromExpr(expression, startAfter, scheduleOptions);
    }

    public Task<string> Enqueue<T>(Expression<Func<T, Task>> expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        return EnqueueFromExpr(expression, startAfter, scheduleOptions);
    }

    async Task<string> EnqueueFromExpr(LambdaExpression expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        var methodArg = expressionSerializer.FromExprToObject(expression)!;
        return await Enqueue("expr_job", startAfter, new JobDataMap
        {
            { "k_type", methodArg.DeclType },
            { "k_methodName", methodArg.MethodName },
            { "k_args", methodArg.Arguments! },
        }, scheduleOptions);
    }
}
