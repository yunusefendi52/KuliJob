using Cronos;
using KuliJob.Internals;
using KuliJob.Storages;

namespace KuliJob.Tests;

public class CronTests : BaseTest
{
    [Test]
    public async Task Can_Add_Cron()
    {
        await using var ss = await SetupServer.Start();
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var tmp = TestUtils.GetTempFile();
        await Assert.That(() => cronJob.AddOrUpdate<ScheduleExpressionTests.MyService>(t => t.ActionMethodTask(tmp), "write_file", "* * * * *")).ThrowsNothing();
    }

    [Test]
    [TestCase("Asia/Jakarta")]
    [TestCase("")]
    [TestCase(null)]
    public async Task Should_Execute_Cron_Only_Once_Per_Minute(string? timeZoneId)
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 100;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobHostedService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = TestUtils.GetTempFile();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(tmp), "inc_file", "* * * * *", new CronOption
        {
            TimeZoneId = timeZoneId,
        })).ThrowsNothing();
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("1");
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("1").Because("Still in 1 minute window throttle");
    }

    [Test]
    public async Task Should_Run_2_Crons()
    {
        await using var ss = await SetupServer.Start();
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobHostedService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var firstTmp = TestUtils.GetTempFile();
        var secondTmp = TestUtils.GetTempFile();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(firstTmp), "inc_file_1", "* * * * *")).ThrowsNothing();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(secondTmp), "inc_file_2", "* * * * *")).ThrowsNothing();
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(firstTmp)).IsEqualTo("1");
        await Assert.That(() => File.ReadAllTextAsync(secondTmp)).IsEqualTo("1");
    }
}
