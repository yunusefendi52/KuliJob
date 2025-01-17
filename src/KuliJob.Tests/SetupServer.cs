namespace KuliJob.Tests;

public static class SetupServier
{
    public static async Task<(IServiceProvider ServiceProvider, IJobScheduler JobScheduler)> StartServerSchedulerAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKuliJob(v =>
        {
            v.JobTimeoutMs = 450;
            v.IsTest = true;
        });
        services.AddKuliJob<HandlerJob>("handler_job");
        services.AddKuliJob<DelayHandlerJob>("delay_handler_job");
        services.AddKuliJob<ThrowsHandlerJob>("throws_handler_job");
        services.AddKuliJob<CheckDataHandlerJob>("check_data_handler_job");
        services.AddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var sp = services.BuildServiceProvider();
        var jobService = sp.GetRequiredService<JobServiceHosted>();
        var jobScheduler = sp.GetRequiredService<IJobScheduler>();
        await jobService.StartAsync(default);
        await jobScheduler.IsStarted;
        return (sp, jobScheduler);
    }
}