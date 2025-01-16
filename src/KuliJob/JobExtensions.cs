using KuliJob.Storages;

namespace KuliJob;

public static class JobExtensions
{
    public static void AddKuliJob(
        this IServiceCollection serviceCollection,
        Action<JobConfiguration>? configure = null)
    {
        var config = new JobConfiguration();
        configure?.Invoke(config);
        serviceCollection.AddSingleton(config);
        RegisterStorage(serviceCollection, config);
        serviceCollection.AddSingleton<JobServerScheduler>();
        serviceCollection.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<JobServerScheduler>());
        serviceCollection.AddSingleton<JobServiceHosted>();
        serviceCollection.AddHostedService(static sp => sp.GetRequiredService<JobServiceHosted>());
    }

    static void RegisterStorage(
        IServiceCollection serviceCollection,
        JobConfiguration configuration)
    {
        if (configuration.UseSqlite)
        {
            serviceCollection.AddSingleton<IJobStorage>(sp => new LocalStorage(null, "kulijob.db", sp.GetRequiredService<JobConfiguration>()));
        }
        else
        {
            serviceCollection.AddSingleton<IJobStorage>(sp => new LocalStorage(new MemoryStream(), null, sp.GetRequiredService<JobConfiguration>()));
        }
    }

    public static void AddKuliJob<T>(this IServiceCollection serviceCollection, string jobName) where T : class, IJob
    {
        serviceCollection.AddKeyedScoped<IJob, T>(jobName);
    }
}