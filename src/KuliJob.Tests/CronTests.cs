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
    public async Task Should_Execute_Cron_Only_Once_Per_Minute()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 100;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobHostedService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(tmp), "inc_file", "* * * * *")).ThrowsNothing();
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("1");
        myClock.AddTimeBy = TimeSpan.FromSeconds(58);
        await WaitCronTicks(3);
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("2");
    }

    [Test]
    public async Task Should_Update_Added_Cron()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 100;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobHostedService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var firstTmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(firstTmp), "inc_file", "* * * * *")).ThrowsNothing();
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(firstTmp)).IsEqualTo("1");
        
        var secondTmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(secondTmp), "inc_file", "* * * * *")).ThrowsNothing();
        
        myClock.AddTimeBy = TimeSpan.FromSeconds(58);
        await WaitCronTicks(3);
        await Assert.That(() => File.ReadAllTextAsync(secondTmp)).IsEqualTo("1");
    }

    [Test]
    public async Task Should_Run_2_Crons()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.MinCronPollingIntervalMs = 100;
        });
        var cronJob = ss.Services.GetRequiredService<ICronJob>();
        var myClock = ss.Services.GetRequiredService<MyClock>();
        var cronScheduler = ss.Services.GetRequiredService<CronJobHostedService>();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var firstTmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var secondTmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(firstTmp), "inc_file_1", "* * * * *")).ThrowsNothing();
        await Assert.That(() => cronJob.AddOrUpdate<MyTestService>(t => t.IncrementTextFile(secondTmp), "inc_file_2", "* * * * *")).ThrowsNothing();
        await WaitCronTicks(2);
        await Assert.That(() => File.ReadAllTextAsync(firstTmp)).IsEqualTo("1");
        await Assert.That(() => File.ReadAllTextAsync(secondTmp)).IsEqualTo("1");
    }
}

public class MyTestService
{
    public async Task IncrementTextFile(string tmp)
    {
        if (!File.Exists(tmp))
        {
            await File.WriteAllTextAsync(tmp, "1");
            return;
        }

        try
        {
            string content = await File.ReadAllTextAsync(tmp);
            if (int.TryParse(content, out int number))
            {
                number++;
                await File.WriteAllTextAsync(tmp, number.ToString());
            }
            else
            {
                throw new FormatException("File content is not a valid integer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file: {ex.Message}");
        }
    }
}
