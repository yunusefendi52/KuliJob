using KuliJob.Internals;
using KuliJob.Storages;

namespace KuliJob.Tests;

public class JobServerEntryTests : BaseTest
{
    [Test]
    public async Task Should_Able_To_Update_Heartbeat()
    {
        var machineName = "";
        await using var ss = await SetupServer.Start(s =>
        {
        }, config: v =>
        {
            machineName = v.ServerName;
        });
        var storage = ss.Services.GetRequiredService<IJobStorage>();
        var jobServers = await Assert.That(() => storage.GetJobServers()).ThrowsNothing();
        await Assert.That(jobServers).IsNotEmpty();
        var jobServer = await Assert.That(() => jobServers!.Single(v => v.Id == machineName)).ThrowsNothing();
        await Assert.That(jobServer!.LastHeartbeat.ToUnixTimeMilliseconds()).IsGreaterThan(DateTimeOffset.MinValue.ToUnixTimeMilliseconds());

        var theLastHeartbeat = jobServer!.LastHeartbeat;
        await Task.Delay(100);
        await storage.UpdateHeartbeatServer();
        jobServers = await Assert.That(() => storage.GetJobServers()).ThrowsNothing();
        jobServer = await Assert.That(() => jobServers!.Single(v => v.Id == machineName)).ThrowsNothing();
        await Assert.That(jobServer!.LastHeartbeat.ToUnixTimeMilliseconds()).IsGreaterThan(theLastHeartbeat.ToUnixTimeMilliseconds());
    }

    [Test]
    public async Task Should_Poll_Heartbeat()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.HeartbeatPolling = 500;
        });
        var firstHeartbeat = DateTimeOffset.UtcNow;
        await Task.Delay(1100);
        var storage = ss.Services.GetRequiredService<IJobStorage>();
        var jobServers = await Assert.That(() => storage.GetJobServers()).ThrowsNothing();
        var jobServer = await Assert.That(() => jobServers!.Single()).ThrowsNothing();
        await Assert.That(jobServer!.LastHeartbeat.ToUnixTimeMilliseconds()).IsGreaterThan(firstHeartbeat.ToUnixTimeMilliseconds());
    }

    [Test]
    public async Task Should_Purge_Inactive_Server()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.HeartbeatPolling = 60_000;
            v.ServerPollingMaintananceIntervalMs = 2000;
            v.ServerPurgeInactiveMs = 1000;
        });
        var storage = ss.Services.GetRequiredService<IJobStorage>();
        await Assert.That(() => storage.GetJobServers()).ThrowsNothing().And.IsNotEmpty();
        await Task.Delay(3000);
        await Assert.That(() => storage.GetJobServers()).ThrowsNothing().And.IsEmpty();
    }

    [Test]
    public async Task Should_Not_Purge_Active_Server()
    {
        await using var ss = await SetupServer.Start(config: v =>
        {
            v.HeartbeatPolling = 500;
            v.ServerPollingMaintananceIntervalMs = 1000;
            v.ServerPurgeInactiveMs = 800;
        });
        var storage = ss.Services.GetRequiredService<IJobStorage>();
        await Assert.That(() => storage.GetJobServers()).ThrowsNothing().And.IsNotEmpty();
        await Task.Delay(3000);
        await Assert.That(() => storage.GetJobServers()).ThrowsNothing().And.IsNotEmpty();
    }
}
