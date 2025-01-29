namespace KuliJob.Tests;

public class SetupServer : IAsyncDisposable
{
    public ServiceProvider Services { get; private set; } = null!;
    public IJobScheduler JobScheduler { get; private set; } = null!;

    public static async Task<SetupServer> Start(
        Action<IServiceCollection>? initServices = null,
        Action<JobConfiguration>? config = null)
    {
        var services = new ServiceCollection();
        initServices?.Invoke(services);
        services.AddLogging();
        services.AddKuliJob(v =>
        {
            config?.Invoke(v);
            // v.JobTimeoutMs = 450;
            v.AddKuliJob<HandlerJob>("handler_job");
            v.AddKuliJob<DelayHandlerJob>("delay_handler_job");
            v.AddKuliJob<ThrowsHandlerJob>("throws_handler_job");
            v.AddKuliJob<CheckDataHandlerJob>("check_data_handler_job");
            v.AddKuliJob<ConditionalThrowsHandlerJob>("conditional_throws_handler_job");
            v.AddKuliJob<ThrowsHandlerJob>();
            v.AddKuliJob<HandlerJob>();
            v.AddKuliJob<DelayHandlerJob>();

            v.UseSqlite(":memory:");
        });
        var sp = services.BuildServiceProvider();
        var jobService = sp.GetRequiredService<JobServiceHosted>();
        var jobScheduler = sp.GetRequiredService<IJobScheduler>();
        await jobService.StartAsync(default);
        await jobScheduler.IsStarted;
        return new()
        {
            Services = sp,
            JobScheduler = jobScheduler,
        };
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        var jobService = Services.GetRequiredService<JobServiceHosted>();
        await jobService.StopAsync(default);
        await Services.DisposeAsync();
    }
}