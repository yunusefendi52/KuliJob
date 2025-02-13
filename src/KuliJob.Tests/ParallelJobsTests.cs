using KuliJob.Storages;

namespace KuliJob.Tests;

public class ParallelJobsTests : BaseTest
{
    [Test]
    public async Task Should_Process_Job_After_One_Job_Completed()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Worker = 2;
        });
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var now = DateTimeOffset.UtcNow.AddMilliseconds(1);
        var jobId1 = await ss.QueueJob.Enqueue("handler_job", now);
        var jobId2 = await ss.QueueJob.Enqueue("delay_handler_job", now, new JobDataMap()
        {
            { "delay", 1000 },
        });
        var jobId3 = await ss.QueueJob.Enqueue("handler_job", now);
        var jobId4 = await ss.QueueJob.Enqueue("delay_handler_job", now, new JobDataMap()
        {
            { "delay", 1000 },
        });
        await WaitJobTicks();
        var job1 = await jobStorage.GetJobById(jobId1);
        var job2 = await jobStorage.GetJobById(jobId2);
        var job3 = await jobStorage.GetJobById(jobId3);
        var job4 = await jobStorage.GetJobById(jobId4);
        await Assert.That(job1!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job2!.JobState).IsEqualTo(JobState.Active);
        await Assert.That(job3!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job4!.JobState).IsEqualTo(JobState.Active);
        var job1And3Delta = job3.CompletedOn!.Value - job1.CompletedOn!.Value;
        await Assert.That(job1And3Delta).IsBetween(TimeSpan.Zero, TimeSpan.FromMilliseconds(30));
        await Task.Delay(1100);
        job2 = await jobStorage.GetJobById(jobId2);
        job4 = await jobStorage.GetJobById(jobId4);
        await Assert.That(job2!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job4!.JobState).IsEqualTo(JobState.Completed);
    }
}
