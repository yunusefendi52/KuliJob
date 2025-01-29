using KuliJob.Storages;

namespace KuliJob.Tests;

public class QueueTests : BaseTest
{
    [Test]
    public async Task Should_Not_Process_If_Queue_Qmpty()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Queues = [];
        });
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var jobId = await ss.JobScheduler.ScheduleJobNow("handler_job");
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job!).IsNotNull().Because("But still can schedule it");
        await Assert.That(job!.JobState).IsEqualTo(JobState.Created);
    }

    [Test]
    public async Task Should_Process_Job_On_Spesific_Queue()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Queues = ["example_queue"];
        });
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var defaultJobId = await ss.JobScheduler.ScheduleJobNow("handler_job");
        var exampleQueueJobId = await ss.JobScheduler.ScheduleJobNow("handler_job", scheduleOptions: new ScheduleOptions
        {
            Queue = "example_queue",
        });
        await WaitJobTicks();
        var defaultJob = await jobStorage.GetJobById(defaultJobId);
        var exampleJob = await jobStorage.GetJobById(exampleQueueJobId);
        await Assert.That(defaultJob).IsNotNull();
        await Assert.That(defaultJob!.JobState).IsEqualTo(JobState.Created);
        await Assert.That(exampleJob).IsNotNull();
        await Assert.That(exampleJob!.CompletedOn).IsNotNull();
        await Assert.That(exampleJob!.JobState).IsEqualTo(JobState.Completed);
    }
}
