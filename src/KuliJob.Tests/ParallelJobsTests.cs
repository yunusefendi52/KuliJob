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
        var jobIds = await Task.WhenAll([
            ss.QueueJob.Enqueue("handler_job", now),
            ss.QueueJob.Enqueue("delay_handler_job", now, new JobDataMap()
            {
                { "delay", 1500 },
            }),
            ss.QueueJob.Enqueue("handler_job", now),
            ss.QueueJob.Enqueue("delay_handler_job", now, new JobDataMap()
            {
                { "delay", 1500 },
            }),
        ]);
        await WaitJobTicks(2);
        var jobs = await Task.WhenAll(jobIds.Select(v => jobStorage.GetJobById(v)));
        var job1 = jobs[0];
        var job2 = jobs[1];
        var job3 = jobs[2];
        var job4 = jobs[3];
        await Assert.That(job1!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job2!.JobState).IsEqualTo(JobState.Active);
        await Assert.That(job3!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job4!.JobState).IsEqualTo(JobState.Active);
        var job1And3Delta = job3.CompletedOn!.Value - job1.CompletedOn!.Value;
        await Assert.That(job1And3Delta).IsBetween(TimeSpan.Zero, TimeSpan.FromMilliseconds(30))
            .Or
            .IsBetween(TimeSpan.Zero, TimeSpan.FromMilliseconds(175)).Because("After EF Core changes");
        await Task.Delay(1600);
        job2 = await jobStorage.GetJobById(job2.Id);
        job4 = await jobStorage.GetJobById(job4.Id);
        await Assert.That(job2!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job4!.JobState).IsEqualTo(JobState.Completed);
    }
}
