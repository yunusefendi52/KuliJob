using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KuliJob.Tests;

public class FetchJobStoppedTests : BaseTest
{
    [Test]
    public async Task Should_Not_Deleted_Scheduled_Job()
    {
        SetupServer? s1 = null;
        await using var funcDisposable = new FuncAsyncDisposable(async () =>
        {
            await s1!.DisposeAsync();
        });
        var scheduleAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var jobId = Guid.Empty;
        {
            var ss = await SetupServer.Start(config: v =>
            {
            });
            s1 = ss;
            var Services = ss.Services;
            var JobScheduler = ss.QueueJob;
            jobId = await JobScheduler.Enqueue("handler_job", scheduleAt);
        }
        {
            var ss = await SetupServer.Start(config: v =>
            {
            }, reuseConnString: s1.CurrentConnString);
            var Services = ss.Services;
            var JobScheduler = ss.QueueJob;
            var jobStorage = Services.GetRequiredService<IJobStorage>();
            var job = await jobStorage.GetJobById(jobId);
            await Assert.That(job).IsNotNull();
            await Assert.That(job!.JobState).IsEqualTo(JobState.Created);
            await Assert.That(job!.JobName).IsEqualTo("handler_job");
            var diff = scheduleAt - job!.StartAfter;
            await Assert.That(diff).IsBetween(TimeSpan.Zero, TimeSpan.FromMilliseconds(100)).WithInclusiveBounds();
        }
    }

    [Test]
    public async Task Should_Process_Scheduled_Job_After_Server_Stopped()
    {
        SetupServer? s1 = null;
        await using var funcDisposable = new FuncAsyncDisposable(async () =>
        {
            await s1!.DisposeAsync();
        });
        var jobId = Guid.Empty;
        {
            var ss = await SetupServer.Start(config: v =>
            {
            });
            s1 = ss;
            var Services = ss.Services;
            var JobScheduler = ss.QueueJob;
            jobId = await JobScheduler.Enqueue("handler_job", DateTimeOffset.UtcNow.AddSeconds(1.5));
        }
        await Task.Delay(1500);
        {
            var ss = await SetupServer.Start(config: v =>
            {
            }, reuseConnString: s1.CurrentConnString);
            await WaitJobTicks();
            var Services = ss.Services;
            var JobScheduler = ss.QueueJob;
            var jobStorage = Services.GetRequiredService<IJobStorage>();
            var job = await jobStorage.GetJobById(jobId);
            await Assert.That(job).IsNotNull();
            await Assert.That(job!.StateMessage).IsNull();
            await Assert.That(job!.FailedOn).IsNull();
            await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        }
    }
}

class FuncAsyncDisposable(Func<ValueTask> valueTask) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return valueTask();
    }
}
