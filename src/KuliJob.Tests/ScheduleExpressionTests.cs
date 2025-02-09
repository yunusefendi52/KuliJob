using KuliJob.Storages;

namespace KuliJob.Tests;

public class ScheduleExpressionTests : BaseTest
{
    [Test]
    public async Task Can_Schedule_And_Completed_Expr_Task()
    {
        await using var ss = await SetupServer.Start();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = TestUtils.GetTempFile();
        var value = 50_000;
        var boolValue = true;
        string? nullValue = null;
        var guidValue = Guid.NewGuid();
        var dateTime = DateTime.UtcNow;
        var dateTimeOffset = DateTimeOffset.UtcNow;
        var decimalValue = decimal.MinValue;
        var jobId = await ss.JobScheduler.ScheduleJobNow(() => ExpressionSerializerTests.TaskMethodParams(tmp, value, boolValue, nullValue, guidValue, dateTime, dateTimeOffset, decimalValue, int.MinValue, short.MaxValue, long.MaxValue));
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job!.FailedMessage).IsNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.FailedOn).IsNull();

        var equalValue = $"Test value {value} {boolValue} nullString {guidValue} {dateTime} {dateTimeOffset} {decimalValue} {int.MinValue} {short.MaxValue} {long.MaxValue}";
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo(equalValue);
    }

    [Test]
    public async Task Can_Schedule_And_Completed_Expr_Action()
    {
        await using var ss = await SetupServer.Start();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = TestUtils.GetTempFile();
        var jobId = await ss.JobScheduler.ScheduleJobNow(() => ActionMethod(tmp));
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job!.FailedMessage).IsNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.FailedOn).IsNull();

        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("action_method");
    }

    [Test]
    public async Task Can_Schedule_And_Completed_Expr_From_Param_Lambda()
    {
        await using var ss = await SetupServer.Start(v =>
        {
            v.AddScoped<MyService>();
        });
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = TestUtils.GetTempFile();
        var jobId = await ss.JobScheduler.ScheduleJobNow<MyService>(t => t.ActionMethodTask(tmp));
        await WaitJobTicks();
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job!.FailedMessage).IsNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.FailedOn).IsNull();

        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo("action_method_task");
    }

    static void ActionMethod(string filePath)
    {
        File.WriteAllText(filePath, "action_method");
    }

    internal class MyService
    {
        internal async Task ActionMethodTask(string filePath)
        {
            if (File.Exists(filePath))
            {
                throw new Exception("File exists. should not be");
            }
            await File.WriteAllTextAsync(filePath, "action_method_task");
        }
    }
}
