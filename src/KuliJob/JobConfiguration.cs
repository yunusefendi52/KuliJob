using System.Diagnostics;

namespace KuliJob;

public class JobConfiguration
{
    public int Worker { get; set; } = Environment.ProcessorCount * 2;
    public int MinPollingIntervalMs { get; set; } = 15_000;
    /// <summary>
    /// Specify job which queue execute on, default queue is "default"
    /// </summary>
    public string[] Queues { get; set; } = ["default"];

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IServiceCollection ServiceCollection { get; init; } = null!;
    internal JobFactory JobFactory { get; init; } = null!;
    internal string ServerName { get; } = Environment.MachineName;
}
