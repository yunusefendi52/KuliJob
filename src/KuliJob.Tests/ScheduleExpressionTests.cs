using KuliJob.Storages;

namespace KuliJob.Tests;

public class ScheduleExpressionTests : BaseTest
{
    [Test]
    public async Task Can_Schedule_And_Completed_Expr_Task()
    {
        await using var ss = await SetupServer.Start();
        var jobStorage = ss.Services.GetRequiredService<IJobStorage>();
        var tmp = Path.GetTempFileName();
        var value = 50_000;
        var boolValue = true;
        string? nullValue = null;
        var guidValue = Guid.NewGuid();
        var dateTime = DateTime.UtcNow;
        var dateTimeOffset = DateTimeOffset.UtcNow;
        var decimalValue = decimal.MinValue;
        var jobId = await ss.JobScheduler.ScheduleJobNow(() => ExpressionSerializerTests.TaskMethodParams(tmp, value, boolValue, nullValue, guidValue, dateTime, dateTimeOffset, decimalValue, int.MinValue, short.MaxValue, long.MaxValue), []);
        await WaitJobTicks(2);
        var job = await jobStorage.GetJobById(jobId);
        await Assert.That(job!.FailedMessage).IsNull();
        await Assert.That(job!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(job!.FailedOn).IsNull();
        
        var equalValue = $"Test value {value} {boolValue} nullString {guidValue} {dateTime} {dateTimeOffset} {decimalValue} {int.MinValue} {short.MaxValue} {long.MaxValue}";
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo(equalValue);
    }
}
