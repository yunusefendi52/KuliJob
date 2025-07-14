using KuliJob.Storages;

namespace KuliJob.Tests;

public class RetryTests : BaseTest
{
    [Test]
    public async Task ShouldRetry_WithNoDelay_UntilJobNotThrows()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.QueueJob;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.Enqueue("conditional_throws_handler_job", new()
        {
            {"throwAtCount", 1},
        }, new()
        {
            RetryMaxCount = 2,
        });
        await WaitJobTicks(10);
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.RetryMaxCount).IsEqualTo(2);
        await Assert.That(job!.RetryCount).IsEqualTo(2);
    }

    [Test]
    public async Task ShouldRetry_WithDelay_UntilJobNotThrows()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.QueueJob;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.Enqueue("conditional_throws_handler_job", new()
        {
            {"throwAtCount", 1},
        }, new()
        {
            RetryMaxCount = 2,
            RetryDelayMs = 100,
        });
        await WaitJobTicks(3);
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.RetryMaxCount).IsEqualTo(2);
        await Assert.That(job!.RetryCount).IsEqualTo(2);
    }

    [Test]
    public async Task ShouldNotRetryJobWhenJobCompleted()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.QueueJob;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.Enqueue("handler_job", [], new()
        {
            RetryMaxCount = 3,
        });
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.RetryCount).IsEqualTo(0);
        await Assert.That(job!.RetryMaxCount).IsEqualTo(3);
    }
}
