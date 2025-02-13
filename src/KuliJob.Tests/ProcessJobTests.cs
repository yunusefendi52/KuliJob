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
        var jobId1 = await ss.QueueJob.Enqueue("handler_job", now.AddMilliseconds(5));
        var jobId2 = await ss.QueueJob.Enqueue("handler_job", now.AddMilliseconds(15));
        var jobId3 = await ss.QueueJob.Enqueue("handler_job", now.AddMilliseconds(25));
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

    [Test]
    public async Task Should_Execute_Jobs_Concurrently_By_Worker()
    {
        var tasksSize = 20;
        var delayHandler = 300;
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Worker = tasksSize - 10;
        });
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var now = DateTimeOffset.UtcNow.AddMilliseconds(1);
        var jobIds = await Task.WhenAll(
            Enumerable.Range(0, tasksSize).Select(v => ss.QueueJob.Enqueue("delay_handler_job", new JobDataMap()
            {
                { "delay", delayHandler },
            }))
        );
        await WaitJobTicks(3);
        var results = await Task.WhenAll(
            jobIds.Select(v => jobStorage.GetJobById(v))
        );
        await Task.WhenAll(results.Select(async j => await Assert.That(j!.FailedMessage).IsNull()));
        await Task.WhenAll(results.Select(async j => await Assert.That(j!.JobState).IsEqualTo(JobState.Completed)));
        await Assert.That(results.Length).IsEqualTo(tasksSize);

        var batch1 = results.Take(10);
        var minBatch1 = batch1.MinBy(v => v!.CompletedOn);
        var maxBatch1 = batch1.MaxBy(v => v!.CompletedOn);
        var diffBatch1 = maxBatch1!.CompletedOn!.Value.ToUnixTimeMilliseconds() - minBatch1!.CompletedOn!.Value.ToUnixTimeMilliseconds();
        await Assert.That(diffBatch1).IsLessThanOrEqualTo(100);
        var batch2 = results.Skip(10).Take(10);
        var minBatch2 = batch2.MinBy(v => v!.CompletedOn);
        var maxBatch2 = batch2.MaxBy(v => v!.CompletedOn);
        var diffBatch2 = maxBatch2!.CompletedOn!.Value.ToUnixTimeMilliseconds() - minBatch2!.CompletedOn!.Value.ToUnixTimeMilliseconds();
        await Assert.That(diffBatch2).IsLessThanOrEqualTo(100);

        var diffBetweenBatch = maxBatch2.CompletedOn.Value.ToUnixTimeMilliseconds() - maxBatch1.CompletedOn.Value.ToUnixTimeMilliseconds();
        await Assert.That(diffBetweenBatch).IsBetween(delayHandler - 30, delayHandler + 30);
    }
}
