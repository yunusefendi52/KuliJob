using KuliJob.Storages;

namespace KuliJob.Tests;

public class ProcessJobTests : BaseTest
{
    [Test]
    public async Task Should_Execute_Latest_Job_First()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Worker = 1;
        });
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var now = DateTimeOffset.UtcNow.AddMilliseconds(1);
        var jobId1 = await ss.JobScheduler.ScheduleJob("handler_job", [], now.AddMilliseconds(5));
        var jobId2 = await ss.JobScheduler.ScheduleJob("handler_job", [], now.AddMilliseconds(15));
        var jobId3 = await ss.JobScheduler.ScheduleJob("handler_job", [], now.AddMilliseconds(25));
        await WaitJobTicks();
        var results = await Task.WhenAll(
            jobStorage.GetJobById(jobId1),
            jobStorage.GetJobById(jobId2),
            jobStorage.GetJobById(jobId3));
        var job1 = results[0]!;
        var job2 = results[1]!;
        var job3 = results[2]!;
        await Assert.That(job1.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job2.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job3.JobState).IsEqualTo(JobState.Completed);

        await Assert.That(job1.CompletedOn!.Value).IsBefore(job2.CompletedOn!.Value);
        await Assert.That(job2.CompletedOn!.Value).IsBefore(job3.CompletedOn!.Value);
    }
}
