using KuliJob.Storages;
using Microsoft.Extensions.Hosting;

namespace KuliJob.CronJob;

internal class CronRegisterService(
    ICronJob cronJob,
    JobConfiguration configuration,
    IQueueJob queueJob) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await queueJob.IsStarted;

        foreach (var cronBuilder in configuration.CronBuilders)
        {
            await cronBuilder.Value(cronJob);
        }
    }
}