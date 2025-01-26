using Microsoft.Extensions.Hosting;

namespace KuliJob;

public class JobServiceHosted(IServiceProvider serviceProvider) : BackgroundService
{
    private JobServerScheduler jobScheduler = null!;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        jobScheduler = serviceProvider.GetRequiredService<JobServerScheduler>();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobScheduler.Start();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await jobScheduler.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}