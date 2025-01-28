using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KuliJob.Tests;

public class FetchJobStoppedTests : BaseTest
{
    [Test]
    public async Task Should_Not_Deleted_Scheduled_Job()
    {
        var sqliteTmp = Path.GetTempFileName();
        var scheduleAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var jobId = "";
        {
            await using var ss = await SetupServer.Start(config: v =>
            {
                v.UseSqlite(sqliteTmp);
            });
            var Services = ss.Services;
            var JobScheduler = ss.JobScheduler;
            jobId = await JobScheduler.ScheduleJob("handler_job", [], scheduleAt);
        }
        {
            await using var ss = await SetupServer.Start(config: v =>
            {
                v.UseSqlite(sqliteTmp);
            });
            var Services = ss.Services;
            var JobScheduler = ss.JobScheduler;
            var jobStorage = Services.GetRequiredService<IJobStorage>();
            var job = await jobStorage.GetJobById(jobId);
            await Assert.That(job).IsNotNull();
            await Assert.That(job!.JobState).IsEqualTo(JobState.Created);
            await Assert.That(job!.JobName).IsEqualTo("handler_job");
            await Assert.That(job!.StartAfter).IsEqualTo(scheduleAt);
        }
    }

    [Test]
    public async Task Should_Process_Scheduled_Job_After_Server_Stopped()
    {
        var sqliteTmp = Path.GetTempFileName();
        var jobId = "";
        {
            await using var ss = await SetupServer.Start(config: v =>
            {
                v.UseSqlite(sqliteTmp);
            });
            var Services = ss.Services;
            var JobScheduler = ss.JobScheduler;
            jobId = await JobScheduler.ScheduleJob("handler_job", [], DateTimeOffset.UtcNow.AddSeconds(1.5));
        }
        await Task.Delay(1500);
        {
            await using var ss = await SetupServer.Start(config: v =>
            {
                v.UseSqlite(sqliteTmp);
            });
            await WaitJobTicks();
            var Services = ss.Services;
            var JobScheduler = ss.JobScheduler;
            var jobStorage = Services.GetRequiredService<IJobStorage>();
            var job = await jobStorage.GetJobById(jobId);
            await Assert.That(job).IsNotNull();
            await Assert.That(job!.FailedMessage).IsNull();
            await Assert.That(job!.FailedOn).IsNull();
            await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        }
    }
}
