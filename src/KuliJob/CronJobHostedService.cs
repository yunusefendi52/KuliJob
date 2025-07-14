using System.Diagnostics;
using Cronos;
using KuliJob.Storages;

namespace KuliJob;

internal class CronJobHostedService(
    JobConfiguration configuration,
    IJobStorage jobStorage,
    IServiceScopeFactory serviceScopeFactory,
    MyClock myClock,
    ILogger<CronJobHostedService> logger)
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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var cronos = await jobStorage.GetCrons();
                await Parallel.ForEachAsync(cronos, new ParallelOptions
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

                    var scheduler = sp.GetRequiredService<IQueueJob>();
                    await scheduler.Enqueue<CronJobHandler>(new JobDataMap
                    {
                        { "k_cron", cron },
                    }, new()
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
