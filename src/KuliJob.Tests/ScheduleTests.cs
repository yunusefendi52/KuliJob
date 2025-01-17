using KuliJob.Storages;

namespace KuliJob.Tests;

public class ScheduleTests : BaseTest
{
    [Test]
    public async Task ShouldFailJobWhenTimedOut()
    {
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("delay_handler_job", "data");
        await Task.Delay(1100);
        var job = await jobStorage.GetJobByState(jobId, JobState.Failed);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedOn).IsNotNull();
        await Assert.That(job!.FailedMessage).IsEqualTo("A task was canceled.");
    }

    [Test]
    public async Task ShouldCompleteJobImmediately()
    {
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("handler_job", "data");
        await Task.Delay(550);
        var job = await jobStorage.GetJobByState(jobId, JobState.Completed);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.CompletedOn).IsNotNull();
    }

    [Test]
    public async Task ShouldFailJobWhenThrowsException()
    {
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("throws_handler_job", "data");
        await Task.Delay(1100);
        var job = await jobStorage.GetJobByState(jobId, JobState.Failed);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedOn).IsNotNull();
        await Assert.That(job!.FailedMessage).IsEqualTo("ThrowsHandlerJob throws exception");
    }
}
