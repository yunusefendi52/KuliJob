using KuliJob.Storages;

namespace KuliJob.Tests;

public class RetryTests : BaseTest
{
    [Test]
    public async Task ShouldRetry_WithNoDelay_UntilJobNotThrows()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("conditional_throws_handler_job", new()
        {
            {"throwAtCount", 1},
        }, new()
        {
            RetryMaxCount = 2,
        });
        await WaitJobTicks();
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
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("conditional_throws_handler_job", new()
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
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("handler_job", [], new()
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

    [Test]
    public async Task ShouldRetry_WithDelay_WhenJobThrows_EndsWithFailed()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("conditional_throws_handler_job", new()
        {
            {"throwAtCount", 2},
        }, new()
        {
            RetryDelayMs = 500,
            RetryMaxCount = 2,
        });
        await WaitJobTicks(2);
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedMessage).IsNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Retry);
        await Assert.That(job!.RetryMaxCount).IsEqualTo(2);
        await Assert.That(job!.RetryCount).IsEqualTo(2);

        await WaitJobTicks();
        job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Failed);
        await Assert.That(job!.RetryMaxCount).IsEqualTo(2);
        await Assert.That(job!.RetryCount).IsEqualTo(2);
        await Assert.That(job!.FailedMessage).IsEqualTo("ConditionalThrowsHandlerJob throws exception");
    }
}
