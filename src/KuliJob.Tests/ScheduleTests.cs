using KuliJob.Storages;

namespace KuliJob.Tests;

public class ScheduleTests : BaseTest
{
    [Test]
    public async Task ShouldFailJobWhenTimedOut()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.JobTimeoutMs = 450;
        });
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("delay_handler_job", []);
        await WaitJobTicks(2);
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Failed);
        await Assert.That(job!.FailedOn).IsNotNull();
        await Assert.That(job!.FailedMessage).IsEqualTo("A task was canceled.");
    }

    [Test]
    public async Task ShouldCompleteJobImmediately()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("handler_job", []);
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.CompletedOn).IsNotNull();
    }

    [Test]
    public async Task ShouldFailJobWhenThrowsException()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("throws_handler_job", []);
        await WaitJobTicks(2);
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Failed);
        await Assert.That(job!.FailedOn).IsNotNull();
        await Assert.That(job!.FailedMessage).IsEqualTo("ThrowsHandlerJob throws exception");
    }

    [Test]
    public async Task ShouldBeAbleToCancelAndResumeJob()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var startAfter = DateTimeOffset.UtcNow.AddMinutes(1);
        var jobId = await JobScheduler.ScheduleJob("handler_job", [], startAfter);
        await WaitJobTicks();
        var jobCreated = await jobStorage.GetJobById(jobId);
        await Assert.That(jobCreated).IsNotNull();
        await Assert.That(jobCreated!.StartAfter).IsEqualTo(startAfter);
        await Assert.That(jobCreated!.JobState).IsEqualTo(JobState.Created);

        await JobScheduler.CancelJob(jobId);
        var jobCancelled = await jobStorage.GetJobById(jobId);
        await Assert.That(jobCancelled).IsNotNull();
        await Assert.That(jobCancelled!.StartAfter).IsEqualTo(startAfter);
        await Assert.That(jobCancelled!.CancelledOn).IsNotNull();
        await Assert.That(jobCancelled.JobState).IsEqualTo(JobState.Cancelled);

        await JobScheduler.ResumeJob(jobId);
        var jobResumed = await jobStorage.GetJobById(jobId);
        await Assert.That(jobResumed).IsNotNull();
        await Assert.That(jobResumed!.StartAfter).IsEqualTo(startAfter);
        await Assert.That(jobResumed!.JobState).IsEqualTo(JobState.Created);
    }

    [Test]
    public async Task ShouldReceiveDataInJobWhenScheduledJob()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var txtFile = Path.GetTempFileName();
        var jobId = await JobScheduler.ScheduleJobNow("check_data_handler_job", new()
        {
            { "txtFile", txtFile },
            { "myInt", int.MaxValue },
            { "myLong", long.MaxValue },
            { "myBool", true },
            { "myDouble", double.MaxValue },
            { "myDateOffset", DateTimeOffset.UtcNow },
            { "myDateTime", DateTime.UtcNow },
        });
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedMessage).IsNullOrWhitespace();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);

        var txt = await File.ReadAllTextAsync(txtFile);
        await Assert.That(txt).IsEqualTo("check_data_handler");
    }

    [Test]
    public async Task ShouldThrows_WhenHandlerNotRegsitered()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("nonexists_handler_job", []);
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedMessage).Contains("No handler registered for job").And.Contains(job.JobName);
    }

    [Test]
    public async Task ShouldNot_BreakLoop_WhenProcessJobsThrows()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow("nonexists_handler_job", []);
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job).IsNotNull();
        await Assert.That(job!.FailedMessage).Contains("No handler registered for job").And.Contains(job.JobName);
    }

    [Test]
    public async Task Can_execute_job_using_type()
    {
        await using var ss = await SetupServer.Start();
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobId = await JobScheduler.ScheduleJobNow<HandlerJob>([]);
        var jobIdThrow = await JobScheduler.ScheduleJobNow<ThrowsHandlerJob>([], new ScheduleOptions()
        {
            RetryMaxCount = 0,
        });
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        var jobThrows = await jobStorage.GetJobById(jobIdThrow);
        await Assert.That(job!.FailedMessage).IsNull();
        await Assert.That(job!.CompletedOn).IsNotNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);

        await Assert.That(jobThrows!.FailedMessage).IsNotNull();
        await Assert.That(jobThrows!.FailedOn).IsNotNull();
        await Assert.That(jobThrows!.JobState).IsEqualTo(JobState.Failed);
    }
}
