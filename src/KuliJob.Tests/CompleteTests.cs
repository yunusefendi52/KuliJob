using KuliJob.Storages;

namespace KuliJob.Tests;

public class CompleteTests
{
    [Test]
    public async Task ShouldCompleteJob()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKuliJob();
        services.AddKuliJob<HandlerJob>("handler-job");
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        var jobService = sp.GetRequiredService<JobServiceHosted>();
        var jobScheduler = sp.GetRequiredService<IJobScheduler>();
        await jobService.StartAsync(default);
        var jobId = await jobScheduler.ScheduleJob("handler-job", "data");
        await Task.Delay(550);
        var job = await jobStorage.GetCompletedJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.CompletedOn).IsNotNull();
    }
}
