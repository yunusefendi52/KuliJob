using Microsoft.Extensions.Hosting;

namespace KuliJob;

public class JobServiceHosted(JobServerScheduler jobScheduler) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobScheduler.Start();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        jobScheduler.Dispose();
        return base.StopAsync(cancellationToken);
    }
}