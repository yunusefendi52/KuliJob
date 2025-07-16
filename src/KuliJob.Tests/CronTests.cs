using Cronos;
using KuliJob.CronJob;
using KuliJob.Internals;
using KuliJob.Storages;
using NSubstitute;

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
    public async Task Should_Execute_Cron_After_AddCronJob()
    {
        var tmp = TestUtils.GetTempFile();
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 100;
            v.AddCron<MyTestService>(t => t.IncrementTextFile(tmp), new()
            {
                CronName = "inc_file",
                CronExpression = "* * * * *",
            });
        });
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("1");
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
        var cronScheduler = ss.Services.GetRequiredService<CronJobSchedulerService>();
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
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 1000;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobSchedulerService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var firstTmp = TestUtils.GetTempFile();
        var secondTmp = TestUtils.GetTempFile();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(firstTmp), "inc_file_1", "* * * * *")).ThrowsNothing();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(secondTmp), "inc_file_2", "* * * * *")).ThrowsNothing();
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(firstTmp)).IsEqualTo("1");
        await Assert.That(() => File.ReadAllTextAsync(secondTmp)).IsEqualTo("1");
    }

    [Test]
    public async Task Should_Run_Only_At_Current_Minute()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 1000;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobSchedulerService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var firstTmp = TestUtils.GetTempFile();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.WriteCurrentDate(firstTmp), "write_date", "* * * * *")).ThrowsNothing();
        await WaitCronTicks(2);
        var date = await Assert.That(() => DateTimeOffset.Parse(File.ReadAllText(firstTmp)).ToUniversalTime()).ThrowsNothing();
        var utcNow = DateTimeOffset.UtcNow;
        await Assert.That(() => date).IsBetween(utcNow.AddSeconds(-5), utcNow.AddSeconds(5)).WithInclusiveBounds();
    }

    [Test]
    [TestCase("* * * * *", null, true)]
    [TestCase("*/2 * * * *", null, false)]
    [TestCase("*/3 * * * *", null, true)]
    [TestCase("*/5 * * * *", "Asia/Jakarta", false)]
    [TestCase("*/5 * * * *", null, false)]
    [TestCase("* * */5 * *", null, false)]
    public async Task CheckShouldSchedule_Tests(string cron, string? timeZone, bool expected)
    {
        var storage = Substitute.For<IJobStorage>();
        var throttleKey = "inc";
        storage.GetJobByThrottle(throttleKey).Returns((Job?)null);
        var myClock = Substitute.For<MyClock>();
        myClock.GetUtcNow().Returns(DateTimeOffset.Parse("2025-02-12T14:03:32Z"));
        var check = await CronJobSchedulerService.CheckShouldSchedule(cron, timeZone, throttleKey, myClock, storage);
        await Assert.That(check).IsEqualTo(expected);
    }

    [Test]
    public async Task Should_Return_Null_If_Invalid_Date()
    {
        var storage = Substitute.For<IJobStorage>();
        var throttleKey = "inc";
        storage.GetJobByThrottle(throttleKey).Returns((Job?)null);
        var myClock = Substitute.For<MyClock>();
        myClock.GetUtcNow().Returns(DateTimeOffset.Parse("2025-02-28T00:05:32Z"));
        var check = await CronJobSchedulerService.CheckShouldSchedule("0 0 */29 * *", null, throttleKey, myClock, storage);
        await Assert.That(check).IsFalse();
    }

    [Test]
    public async Task Should_Skip_If_Cron_Misfire_By_Default()
    {
        var storage = Substitute.For<IJobStorage>();
        var throttleKey = "inc";
        storage.GetJobByThrottle(throttleKey).Returns((Job?)null);
        var myClock = Substitute.For<MyClock>();
        myClock.GetUtcNow().Returns(DateTimeOffset.Parse("2025-02-10T00:05:10Z"));
        var check = await CronJobSchedulerService.CheckShouldSchedule("*/10 * * * *", null, throttleKey, myClock, storage);
        await Assert.That(check).IsFalse();
    }

    [Test]
    public async Task Should_Not_Schedule_Cron_If_Already_Scheduled()
    {
        var storage = Substitute.For<IJobStorage>();
        var throttleKey = "inc";
        storage.GetJobByThrottle(throttleKey).Returns(new Job
        {
            CreatedOn = DateTimeOffset.Parse("2025-02-10T00:10:00Z"),
        });
        var myClock = Substitute.For<MyClock>();
        myClock.GetUtcNow().Returns(DateTimeOffset.Parse("2025-02-10T00:10:59Z"));
        var check = await CronJobSchedulerService.CheckShouldSchedule("*/10 * * * *", null, throttleKey, myClock, storage);
        await Assert.That(check).IsFalse();
    }

    [Test]
    public async Task Should_Schedule_Cron_If_Job_More_Than_Scheduled()
    {
        var storage = Substitute.For<IJobStorage>();
        var throttleKey = "inc";
        storage.GetJobByThrottle(throttleKey).Returns(new Job
        {
            CreatedOn = DateTimeOffset.Parse("2025-02-10T00:10:00Z"),
        });
        var myClock = Substitute.For<MyClock>();
        myClock.GetUtcNow().Returns(DateTimeOffset.Parse("2025-02-10T00:20:00Z"));
        var check = await CronJobSchedulerService.CheckShouldSchedule("*/10 * * * *", null, throttleKey, myClock, storage);
        await Assert.That(check).IsTrue();
    }
}
