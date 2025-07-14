using KuliJob.Utils;

namespace KuliJob.Tests;

public class TestHelperTests
{
    [Test]
    public async Task Should_Complete_Either_Task_If_Completed_At_The_Same_Time()
    {
        var result = await TaskHelper.RaceAsync((c) => Task.Delay(100, c).ContinueWith(_ => 0), (c) => Task.Delay(100, c).ContinueWith(_ => 1));
        await Assert.That(result).IsEqualTo(0).Or.IsEqualTo(1);
    }

    [Test]
    public async Task Should_Complete_First_Task()
    {
        var result = await TaskHelper.RaceAsync((c) => Task.Delay(100, c).ContinueWith(_ => 0), (c) => Task.Delay(150, c).ContinueWith(_ => 1));
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Should_Complete_Second_Task()
    {
        var result = await TaskHelper.RaceAsync((c) => Task.Delay(150, c).ContinueWith(_ => 0), (c) => Task.Delay(100, c).ContinueWith(_ => 1));
        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task Should_Not_Continue_Last_TaskIfCancelled()
    {
        var shouldCancelled = false;
        var result = await TaskHelper.RaceAsync((c) => Task.Delay(100, c).ContinueWith(_ => 0), (c) => Task.Delay(15_000, c).ContinueWith(t =>
        {
            shouldCancelled = t.IsCanceled;
            return 1;
        }));
        await Assert.That(result).IsEqualTo(0);
        await Task.Delay(200);
        await Assert.That(shouldCancelled).IsEqualTo(true);
    }
}