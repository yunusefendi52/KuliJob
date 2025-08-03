using System.Diagnostics;
using System.Text.Json;
using Cronos;
using KuliJob.Storages;

namespace KuliJob.CronJob;

internal class CronJobSchedulerService(
    JobConfiguration configuration,
    IJobStorage jobStorage,
    IServiceScopeFactory serviceScopeFactory,
    MyClock myClock,
    ILogger<CronJobSchedulerService> logger)
{
    static readonly TimeSpan throttleTime = TimeSpan.FromSeconds(60);

    public async static Task<bool> CheckShouldSchedule(
        string cronExpression,
        string? timeZone,
        string throttleKey,
        MyClock myClock,
        IJobStorage jobStorage)
    {
        var expression = CronExpression.Parse(cronExpression);
        var timezoneInfo = string.IsNullOrEmpty(timeZone) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var next = expression.GetNextOccurrence(myClock.GetUtcNow().AddMinutes(-1), timezoneInfo, true);
        if (!next.HasValue)
        {
            return false;
        }
        var prevNext = next.Value;
        var now = myClock.GetUtcNow();
        var diff = now - prevNext;
        // TODO: Add handler misfire. Default skip misfire.
        var shouldSchedule = diff >= TimeSpan.Zero && diff < TimeSpan.FromSeconds(60);
        if (!shouldSchedule)
        {
            return false;
        }
        if (!string.IsNullOrEmpty(throttleKey))
        {
            var prevJobThrottle = await jobStorage.GetJobByThrottle(throttleKey);
            if (prevJobThrottle is not null)
            {
                var throttleOn = prevJobThrottle.CreatedOn.Add(throttleTime);
                var delta = now - throttleOn;
                if (delta.TotalMilliseconds < 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public async Task ProcessCron(CancellationToken stoppingToken)
    {
        var cronos = await jobStorage.GetCrons();
        var currCronos = cronos.Where(v => configuration.CronBuilders.Keys.Any(t => t == v.Name)).ToList();
        Serializer serializer = new();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                await Parallel.ForEachAsync(currCronos, new ParallelOptions
                {
                    CancellationToken = stoppingToken,
                    MaxDegreeOfParallelism = configuration.Worker,
                }, async (cron, cancellationToken) =>
                {
                    await using var scope = serviceScopeFactory.CreateAsyncScope();
                    var sp = scope.ServiceProvider;
                    var throttleKey = cron.Name;
                    var nextSchedule = await CheckShouldSchedule(cron.CronExpression, cron.TimeZone, throttleKey, myClock, jobStorage);
                    if (!nextSchedule)
                    {
                        return;
                    }

                    var cronData = JsonSerializer.Deserialize<CronData>(cron.Data, Serializer.jsonSerializerOptions)!;
                    var methodExpr = cronData.Expr;
                    if (string.IsNullOrWhiteSpace(methodExpr))
                    {
                        throw new ArgumentException("Invalid cron job handler");
                    }

                    var queueExprJob = sp.GetRequiredService<IQueueExprJob>();
                    var jobFactory = sp.GetRequiredService<JobFactory>();
                    var methodExprCall = serializer.Deserialize<MethodExprCall>(methodExpr)!;
                    await queueExprJob.Enqueue<CronJobHandler>(t => t.Execute(methodExprCall), new QueueOptions()
                    {
                        Queue = JobExtensions.DefaultCronName,
                        ThrottleKey = throttleKey,
                        ThrottleTime = throttleTime,
                    });
                });

                sw.Stop();
                var delayDiff = Math.Max(configuration.MinCronPollingIntervalMs - sw.ElapsedMilliseconds, 0);
                await Task.Delay(TimeSpan.FromMilliseconds(delayDiff), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cron job");
                break;
            }
        }
    }
}
