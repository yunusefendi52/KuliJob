using KuliJob.Storages;

namespace KuliJob.Tests;

public class CronTests : BaseTest
{
    [Test]
    public async Task Can_Add_Cron()
    {
        await using var ss = await SetupServer.Start();
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var tmp = Path.GetTempFileName();
        await Assert.That(() => cronJob.AddOrUpdate<ScheduleExpressionTests.MyService>(t => t.ActionMethodTask(tmp), "write_file", "* * * * *")).ThrowsNothing();
    }

    [Test]
    public async Task Can_Execute_Cron()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 0;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobHostedService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = Path.GetTempFileName();
        await Assert.That(() => cronJob.AddOrUpdate<ScheduleExpressionTests.MyService>(t => t.ActionMethodTask(tmp), "write_file", "* * * * *")).ThrowsNothing();
        await WaitCronTicks();
        var crons = await jobStorage.GetCrons();
        var myCron = crons.Single(v => v.Name == "write_file");
    }
}
