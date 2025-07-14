using System.Diagnostics;

namespace KuliJob;

public class JobConfiguration
{
    public int Worker { get; set; } = Environment.ProcessorCount * 2;
    public int MinPollingIntervalMs { get; set; } = 15_000;
    /// <summary>
    /// Specify job which queue execute on, default queue is "default"
    /// </summary>
    public HashSet<string> Queues { get; set; } = ["default"];
    public int MinCronPollingIntervalMs { get; set; } = 15_000;

    /// <summary>
    /// Polling check inactive server in milliseconds
    /// </summary>
    public int ServerPollingMaintananceIntervalMs { get; set; } = 60_000 * 10;
    /// <summary>
    /// Purge inactive server that haven't sent a heartbeat in milliseconds
    /// </summary>
    public int ServerPurgeInactiveMs { get; set; } = 60_000 * 2;

    /// <summary>
    /// Specify polling heartbeat, in milliseconds
    /// </summary>
    public int HeartbeatPolling { get; set; } = 15_000;

    /// <summary>
    /// Whether to enable real-time new job notifier. Defaults to true
    /// Postgres uses LISTEN/NOTIFY
    /// </summary>
    public bool ListenNotifyNewJobEnabled { get; set; } = true;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IServiceCollection ServiceCollection { get; init; } = null!;
    internal JobFactory JobFactory { get; init; } = null!;
    internal string ServerName { get; } = Environment.MachineName;

}
