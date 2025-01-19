namespace KuliJob.Tests;

public static class SetupServier
{
    public static async Task<(IServiceProvider ServiceProvider, IJobScheduler JobScheduler)> StartServerSchedulerAsync(Action<IServiceCollection>? initServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKuliJob(v =>
        {
            v.JobTimeoutMs = 450;
            v.IsTest = true;
            v.AddKuliJob<HandlerJob>("handler_job");
            v.AddKuliJob<DelayHandlerJob>("delay_handler_job");
            v.AddKuliJob<ThrowsHandlerJob>("throws_handler_job");
            v.AddKuliJob<CheckDataHandlerJob>("check_data_handler_job");
            v.AddKuliJob<ConditionalThrowsHandlerJob>("conditional_throws_handler_job");

            v.UseSqlite(":memory:");
        });
        services.AddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        initServices?.Invoke(services);
        var sp = services.BuildServiceProvider();
        var jobService = sp.GetRequiredService<JobServiceHosted>();
        var jobScheduler = sp.GetRequiredService<IJobScheduler>();
        await jobService.StartAsync(default);
        await jobScheduler.IsStarted;
        return (sp, jobScheduler);
    }
}