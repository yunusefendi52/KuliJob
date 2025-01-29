using KuliJob.Storages;

namespace KuliJob.Tests;

public class PriorityTests : BaseTest
{
    [Test]
    public async Task Smaller_Priority_Should_Run_First()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Worker = 2;
        });
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var scheduleIn = DateTimeOffset.UtcNow.AddMilliseconds(100);
        var jobIds = await Task.WhenAll(Enumerable.Range(0, 4).Select(v =>
        {
            var priority = v % 2;
            return JobScheduler.ScheduleJob("delay_handler_job", scheduleIn, new()
            {
                { "delay", 200 },
            }, new()
            {
                Priority = priority,
            });
        }));
        await WaitJobTicks(2);
        var jobs = await Task.WhenAll(jobIds.Select(jobStorage.GetJobById));
        var orderedJobs = jobs.OrderBy(v => v!.Priority).ThenBy(v => v!.CompletedOn);
        await Assert.That(orderedJobs).HasCount().EqualTo(4);

        // Priority 0
        {
            var priorityJobs = orderedJobs.Take(2);
            var job1 = priorityJobs.First()!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var job2 = priorityJobs.Last()!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var deltaPriority = job2 - job1;
            await Assert.That(deltaPriority).IsLessThan(25);
        }
        // Priority 1
        {
            var priorityJobs = orderedJobs.Skip(2).Take(2);
            var job1 = priorityJobs.First()!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var job2 = priorityJobs.Last()!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var deltaPriority = job2 - job1;
            await Assert.That(deltaPriority).IsLessThan(25);
        }

        // Delta between priority
        {
            var priorityJobs = orderedJobs;
            var job1 = priorityJobs.First(v => v!.Priority == 0)!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var job2 = priorityJobs.First(v => v!.Priority == 1)!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var deltaPriority = job2 - job1;
            await Assert.That(deltaPriority).IsBetween(180, 225).Because("Smaller priority will run first");
        }
    }

    [Test]
    public async Task Different_Priority_Should_Run_Concurrently_When_There_Is_Worker_Available()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.Worker = 6;
        });
        var Services = ss.Services;
        var JobScheduler = ss.JobScheduler;
        var jobStorage = Services.GetRequiredService<IJobStorage>();
        var scheduleIn = DateTimeOffset.UtcNow.AddMilliseconds(100);
        var jobIds = await Task.WhenAll(Enumerable.Range(0, 4).Select(v =>
        {
            var priority = v % 2;
            return JobScheduler.ScheduleJob("delay_handler_job", scheduleIn, new()
            {
                { "delay", 200 },
            }, new()
            {
                Priority = priority,
            });
        }));
        await WaitJobTicks(2);
        var jobs = await Task.WhenAll(jobIds.Select(jobStorage.GetJobById));
        var orderedJobs = jobs.OrderBy(v => v!.Priority).ThenBy(v => v!.CompletedOn);
        await Assert.That(orderedJobs).HasCount().EqualTo(4);

        {
            var priorityJobs = orderedJobs;
            var job1 = priorityJobs.First(v => v!.Priority == 0)!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var job2 = priorityJobs.First(v => v!.Priority == 1)!.CompletedOn!.Value.ToUnixTimeMilliseconds();
            var deltaPriority = job2 - job1;
            await Assert.That(deltaPriority).IsLessThan(25);
        }
    }
}
