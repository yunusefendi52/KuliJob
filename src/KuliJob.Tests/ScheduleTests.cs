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

    [Test]
    public async Task ShouldBeAbleToCancelAndResumeJob()
    {
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var startAfter = DateTimeOffset.UtcNow.AddMinutes(1);
        var jobId = await JobScheduler.ScheduleJob("handler_job", "data", startAfter);
        await Task.Delay(550);
        var jobCreated = await jobStorage.GetJobByState(jobId, JobState.Created);
        await Assert.That(jobCreated).IsNotNull();
        await Assert.That(jobCreated!.StartAfter).IsEqualTo(startAfter);
        await Assert.That(jobCreated!.JobState).IsEqualTo(JobState.Created);
        
        await JobScheduler.CancelJob(jobId);
        var jobCancelled = await jobStorage.GetJobByState(jobId, JobState.Cancelled);
        await Assert.That(jobCancelled).IsNotNull();
        await Assert.That(jobCancelled!.StartAfter).IsEqualTo(startAfter);
        await Assert.That(jobCancelled!.CancelledOn).IsNotNull();
        await Assert.That(jobCancelled.JobState).IsEqualTo(JobState.Cancelled);
        
        await JobScheduler.ResumeJob(jobId);
        var jobResumed = await jobStorage.GetJobByState(jobId, JobState.Created);
        await Assert.That(jobResumed).IsNotNull();
        await Assert.That(jobResumed!.StartAfter).IsEqualTo(startAfter);
        await Assert.That(jobResumed!.JobState).IsEqualTo(JobState.Created);
    }
}
