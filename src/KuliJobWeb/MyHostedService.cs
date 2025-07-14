
using KuliJobWeb.Jobs;

namespace KuliJobWeb;

public class MyHostedService(ICronJob cronJob) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // await cronJob.AddOrUpdate<NotifyJob>(t => t.CallApi("This is from cron"), "cron_notify_job", "*/2 * * * *");
    }
}