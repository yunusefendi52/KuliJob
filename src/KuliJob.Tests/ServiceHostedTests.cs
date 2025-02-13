namespace KuliJob.Tests;

public class ServiceHostedTests : BaseTest
{
    [Test]
    public async Task Should_Dispose_ServiceHosted()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.QueueJob;
        var jobService = Services.GetRequiredService<JobServiceHosted>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await jobService.StopAsync(cts.Token);
    }
}
