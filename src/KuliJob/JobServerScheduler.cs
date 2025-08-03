using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using KuliJob.CronJob;
using KuliJob.Storages;
using KuliJob.Utils;

namespace KuliJob;

internal class JobServerScheduler(
    ILogger<JobServerScheduler> logger,
    IServiceScopeFactory serviceScopeFactory,
    IJobStorage storage,
    JobConfiguration configuration,
    Serializer serializer,
    ExpressionSerializer expressionSerializer,
    CronJobSchedulerService cronJobHostedService) : IQueueJob, IQueueExprJob, IAsyncDisposable
{
    readonly CancellationTokenSource cancellation = new();

    readonly TaskCompletionSource isStarted = new();

    public Task IsStarted => isStarted.Task;

    readonly Channel<Guid> notifier = Channel.CreateBounded<Guid>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropNewest,
    });

    public async Task Start()
    {
        await storage.StartStorage(cancellation.Token);
        logger.LogInformation($"""
        🔄 Job scheduler started. Configuration:
            Server name: {configuration.ServerName}
            Worker: {configuration.Worker}
            Queues: {string.Join(", ", configuration.Queues)}
            Min job polling: {configuration.MinPollingIntervalMs} ms
            Real-time job notifier enabled: {configuration.ListenNotifyNewJobEnabled}
            Min cron polling: {configuration.MinCronPollingIntervalMs} ms
            Polling heartbeat: {configuration.HeartbeatPolling} ms
        """);
        isStarted.SetResult();
        storage.NextJobIdNotifier += (s, e) =>
        {
            notifier.Writer.TryWrite(e);
        };
        await Task.WhenAll(cronJobHostedService.ProcessCron(cancellation.Token), ProcessQueueAsync(cancellation.Token), PollHeartbeat(cancellation.Token)
            , ProcessMaintenance(cancellation.Token));
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        cancellation.Cancel();
        return ValueTask.CompletedTask;
    }

    public async Task<Guid> Enqueue(string jobName, DateTimeOffset startAfter, JobDataMap? data = null, QueueOptions? scheduleOptions = null)
    {
        var jobData = serializer.Serialize(data);
        var throttleKey = scheduleOptions.HasValue ? scheduleOptions.Value.ThrottleKey : null;
        var throttleTime = scheduleOptions.HasValue && scheduleOptions.Value.ThrottleTime.HasValue ? scheduleOptions.Value.ThrottleTime.Value : TimeSpan.Zero;
        var jobInput = new Job
        {
            JobStateId = null,
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
        Guid? nextId = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            while (true)
            {
                var nextJob = await storage.FetchNextJob(nextId, cancellationToken);
                nextId = null;
                if (nextJob is null)
                {
                    break;
                }

                yield return nextJob;
            }
            nextId = await TaskHelper.RaceAsync(
                (c) => Task.Delay(TimeSpan.FromMilliseconds(configuration.MinPollingIntervalMs), c).ContinueWith(v => (Guid?)null),
                (c) => notifier.Reader.ReadAsync(c).AsTask()!.ContinueWith(v =>
                {
                    return (Guid?)null;
                    // if (v.IsCanceled)
                    // {
                    //     return null;
                    // }
                    // return (Guid?)v.Result; // TODO: this thing sometimes breaks integration test
                }));
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

            var jobDataMap = serializer.Deserialize<JobDataMap>(jobInput.JobData!);
            var jobContext = new JobContext
            {
                Services = sp,
                JobName = jobInput.JobName,
                JobData = jobDataMap!,
                RetryCount = jobInput.RetryCount,
            };

            var isExprJob = jobDataMap?.ContainsKey("k_type") ?? false;
            if (isExprJob)
            {
                var kType = jobDataMap!.GetValue<string>("k_type");
                var kMethodName = jobDataMap.GetValue<string>("k_methodName");
                var kArgs = jobDataMap.GetValue<IEnumerable<MethodExprCall.MethodExprCallArg?>>("k_args");
                var exprSerializer = sp.GetRequiredService<ExpressionSerializer>();
                var serviceProvider = sp.GetRequiredService<IServiceProvider>();
                await exprSerializer.InvokeExpr(serviceProvider, new MethodExprCall
                {
                    DeclType = kType!,
                    MethodName = kMethodName!,
                    Arguments = kArgs,
                });
            }
            else
            {
                var jobFactory = sp.GetRequiredService<JobFactory>();
                var jobHandler = jobFactory.ResolveService(sp, jobInput.JobName) ?? throw new ArgumentException($"No handler registered for job type {jobInput.JobName}. Call {nameof(JobExtensions.AddKuliJob)} to register job");
                await jobHandler.Execute(jobContext);
            }
            await storage.CompleteJobById(jobInput.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job error {jobId} {jobName}", jobInput.Id, jobInput.JobName);
            await storage.FailJobById(jobInput.Id, ex.Message);

            if (jobInput.RetryMaxCount > 0 && jobInput.RetryCount < jobInput.RetryMaxCount)
            {
                logger.LogError(ex, "Job error, will retry {jobId} {jobName} current retry {retryCount} max {retryMaxCount}", jobInput.Id, jobInput.JobName, jobInput.RetryCount, jobInput.RetryMaxCount);
                await storage.RetryJob(jobInput.Id, jobInput.RetryDelayMs);
                if (jobInput.RetryDelayMs == 0)
                {
                    goto RETRY;
                }
            }
        }
    }

    public async Task CancelJob(Guid jobId)
    {
        await storage.CancelJobById(jobId);
    }

    public async Task ResumeJob(Guid jobId)
    {
        await storage.ResumeJob(jobId);
    }

    Task<Guid> IQueueExprJob.Enqueue(Expression<Action> expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        return EnqueueFromExpr(expression, startAfter, scheduleOptions);
    }

    Task<Guid> IQueueExprJob.Enqueue(Expression<Func<Task>> expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        return EnqueueFromExpr(expression, startAfter, scheduleOptions);
    }

    Task<Guid> IQueueExprJob.Enqueue<T>(Expression<Func<T, Task>> expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        return EnqueueFromExpr(expression, startAfter, scheduleOptions);
    }

    async Task<Guid> EnqueueFromExpr(LambdaExpression expression, DateTimeOffset startAfter, QueueOptions? scheduleOptions = null)
    {
        var methodArg = expressionSerializer.FromExprToObject(expression)!;
        var jobName = $"{methodArg.DeclType.Split(',')[0]}.{methodArg.MethodName}";
        return await Enqueue(jobName, startAfter, new JobDataMap
        {
            { "k_type", methodArg.DeclType },
            { "k_methodName", methodArg.MethodName },
            { "k_args", methodArg.Arguments! },
        }, scheduleOptions);
    }

    async Task PollHeartbeat(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(configuration.HeartbeatPolling, cancellationToken);
                await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
                var sp = serviceScope.ServiceProvider;
                var storage = sp.GetRequiredService<IJobStorage>();
                await storage.UpdateHeartbeatServer();
            }
            catch (Exception ex)
            {
                logger.LogError("Error poll heartbeat {ex}", ex);
            }
        }
    }

    async Task ProcessMaintenance(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
                var sp = serviceScope.ServiceProvider;
                var storage = sp.GetRequiredService<IJobStorage>();
                await storage.RemoveInactiveServers();

                await Task.Delay(configuration.ServerPollingMaintananceIntervalMs, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running maintanance");
            }
        }
    }
}
