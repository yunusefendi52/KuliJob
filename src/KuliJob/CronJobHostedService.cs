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
                    var expression = CronExpression.Parse(cron.CronExpression);
                    var timezoneInfo = string.IsNullOrEmpty(cron.TimeZone) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(cron.TimeZone);
                    var next = expression.GetNextOccurrence(myClock.GetUtcNow().AddMinutes(-1), timezoneInfo, true);
                    if (!next.HasValue)
                    {
                        return;
                    }
                    var prevNext = next.Value;
                    var now = myClock.GetUtcNow();
                    var diff = now - prevNext;
                    // TODO: Add handler misfire. Default skip misfire.
                    var shouldSchedule = diff >= TimeSpan.Zero && diff < TimeSpan.FromSeconds(60);
                    if (shouldSchedule)
                    {
                        var throttleKey = cron.Name;
                        var throttleTime = TimeSpan.FromSeconds(60);
                        if (!string.IsNullOrEmpty(throttleKey))
                        {
                            var prevJobThrottle = await jobStorage.GetJobByThrottle(throttleKey);
                            if (prevJobThrottle is not null)
                            {
                                var throttleOn = prevJobThrottle.CreatedOn.Add(throttleTime);
                                var delta = now - throttleOn;
                                if (delta.TotalMilliseconds < 0)
                                {
                                    return;
                                }
                            }
                        }
                        var scheduler = sp.GetRequiredService<IJobScheduler>();
                        await scheduler.ScheduleJobNow<CronJobHandler>(new JobDataMap
                        {
                            { "cron", cron },
                        }, new()
                        {
                            Queue = "k_cron",
                            ThrottleKey = throttleKey,
                            ThrottleTime = throttleTime,
                        });
                    }
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
