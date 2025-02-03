using System.Diagnostics;
using Cronos;
using KuliJob.Storages;
using Microsoft.Extensions.Hosting;

namespace KuliJob;

internal class CronJobHostedService(
    JobConfiguration configuration,
    IJobStorage jobStorage,
    IServiceScopeFactory serviceScopeFactory,
    MyClock myClock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();

            await Parallel.ForEachAsync(await jobStorage.GetCrons(), new ParallelOptions
            {
                CancellationToken = stoppingToken,
            }, async (cron, cancellationToken) =>
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var sp = scope.ServiceProvider;
                var expression = CronExpression.Parse(cron.CronExpression);
                var next = expression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
                if (!next.HasValue)
                {
                    return;
                }
                var now = myClock.GetUtcNow();
                var diff = next.Value - now;
                // TODO: Add handler misfire
                var shouldSchedule = diff <= TimeSpan.FromSeconds(60);
                if (shouldSchedule)
                {
                    var scheduler = sp.GetRequiredService<IJobScheduler>();
                    await scheduler.ScheduleJobNow<CronJobHandler>(new JobDataMap
                    {
                        { "cron", cron }
                    });
                }
            });

            sw.Stop();
            var delayDiff = Math.Max(configuration.MinCronPollingIntervalMs - sw.ElapsedMilliseconds, 0);

            await Task.Delay(TimeSpan.FromMilliseconds(delayDiff), stoppingToken);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
