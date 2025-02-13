using KuliJob.Tests.Storages.Postgres;

namespace KuliJob.Tests;

public class SetupServer : IAsyncDisposable
{
    public ServiceProvider Services { get; private set; } = null!;
    public IQueueJob QueueJob { get; private set; } = null!;

    public Func<Task>? Dispose { get; private set; }

    public static async Task<SetupServer> Start(
        Action<IServiceCollection>? initServices = null,
        Action<JobConfiguration>? config = null)
    {
        Func<Task>? Dispose = null;
        string connString = null!;
        if (ModuleInitializer.K_TestStorage == KTestType.Pg)
        {
            var postgresStart = new PostgresStart();
            connString = await postgresStart.Start();
            Dispose = async () =>
            {
                await postgresStart.DisposeAsync();
            };
        }
        else if (ModuleInitializer.K_TestStorage == KTestType.Memory)
        {
            connString = ":memory:";
        }
        else if (ModuleInitializer.K_TestStorage == KTestType.Sqlite)
        {
            connString = TestUtils.GetTempFile();
            Dispose = () =>
            {
                File.Delete(connString);
                return Task.CompletedTask;
            };
        }

        var services = new ServiceCollection();
        initServices?.Invoke(services);
        services.AddLogging();
        services.AddKuliJob(v =>
        {
            v.MinPollingIntervalMs = 500;
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

            if (ModuleInitializer.K_TestStorage == KTestType.Memory || ModuleInitializer.K_TestStorage == KTestType.Sqlite)
            {
                v.UseSqlite(connString);
            }
            else if (ModuleInitializer.K_TestStorage == KTestType.Pg)
            {
                v.UsePostgreSQL(connString);
            }
            else
            {
                throw new ArgumentException("Invalid test storage");
            }
        });
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var jobService = sp.GetRequiredService<JobServiceHosted>();
        var queueJob = sp.GetRequiredService<IQueueJob>();
        var cronScheduler = sp.GetRequiredService<CronJobHostedService>();
        await jobService.StartAsync(default);
        await queueJob.IsStarted;
        return new()
        {
            Services = sp,
            QueueJob = queueJob,
            Dispose = Dispose,
        };
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        var jobService = Services.GetRequiredService<JobServiceHosted>();
        await jobService.StopAsync(default);
        await Services.DisposeAsync();
        if (Dispose is not null)
        {
            await Dispose.Invoke();
        }
    }
}