namespace KuliJob.Tests;

public class ServiceHostedTests : BaseTest
{
    [Test]
    public async Task Should_Dispose_ServiceHosted()
    {
        var jobService = Services.GetRequiredService<JobServiceHosted>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await jobService.StopAsync(cts.Token);
    }
}
