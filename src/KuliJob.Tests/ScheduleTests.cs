using KuliJob.Storages;

namespace KuliJob.Tests;

public class ScheduleTests
{
    [Test]
    public async Task ShouldFailJobWhenTimedOut()
    {
        var (sp, jobScheduler) = await SetupServier.StartServerSchedulerAsync();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        var jobId = await jobScheduler.ScheduleJobNow("delay_handler_job", "data");
        await Task.Delay(1100);
        var job = await jobStorage.GetJobByState(jobId, JobState.Failed);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedOn).IsNotNull();
        await Assert.That(job!.FailedMessage).IsEqualTo("A task was canceled.");
    }

    [Test]
    public async Task ShouldCompleteJobImmediately()
    {
        var (sp, jobScheduler) = await SetupServier.StartServerSchedulerAsync();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        var jobId = await jobScheduler.ScheduleJobNow("handler_job", "data");
        await Task.Delay(550);
        var job = await jobStorage.GetJobByState(jobId, JobState.Completed);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.CompletedOn).IsNotNull();
    }

    [Test]
    public async Task ShouldFailJobWhenThrowsException()
    {
        var (sp, jobScheduler) = await SetupServier.StartServerSchedulerAsync();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        var jobId = await jobScheduler.ScheduleJobNow("throws_handler_job", "data");
        await Task.Delay(1100);
        var job = await jobStorage.GetJobByState(jobId, JobState.Failed);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedOn).IsNotNull();
        await Assert.That(job!.FailedMessage).IsEqualTo("ThrowsHandlerJob throws exception");
    }
}
