using KuliJob.Storages;

namespace KuliJob.Tests;

public class FetchJobTests : BaseTest
{
    [Test]
    public async Task Should_Not_Fetch_Next_Job_If_Worker_Is_Full()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Worker = 2;
        });
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var jobIds = await Task.WhenAll(Enumerable.Range(0, 5).Select(c => JobScheduler.ScheduleJobNow<DelayHandlerJob>(new JobDataMap
        {
            { "delay", c >= 1 ? 1000 : 0 },
        })));
        await Task.Delay(1000);
        var jobs = await Assert.That(() => Task.WhenAll(jobIds.Select(v => jobStorage.GetJobById(v)))).ThrowsNothing();
        var job3 = jobs![3];
        await Assert.That(job3!.JobState).IsEqualTo(JobState.Created);
        await Assert.That(job3!.StartedOn).IsNull();
        await Verify(jobs);
    }
}
