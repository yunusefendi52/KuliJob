using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using KuliJob;

namespace Microsoft.Extensions.DependencyInjection;

public static class JobExtensions
{
    public static void AddKuliJob(
        this IServiceCollection serviceCollection,
        Action<JobConfiguration>? configure = null)
    {
        var config = new JobConfiguration()
        {
            ServiceCollection = serviceCollection,
        };
        configure?.Invoke(config);
        config.UseSqlite(":memory:");
        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton<JobServerScheduler>();
        serviceCollection.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<JobServerScheduler>());
        serviceCollection.AddSingleton<JobServiceHosted>();
        serviceCollection.AddSingleton<Serializer>();
        serviceCollection.AddHostedService(static sp => sp.GetRequiredService<JobServiceHosted>());
        serviceCollection.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
    }

    public static void UseSqlite(this JobConfiguration jobConfiguration, string connectionString)
    {
        jobConfiguration.ServiceCollection.TryAddSingleton<LocalStorage>();
        jobConfiguration.ServiceCollection.TryAddSingleton<IJobStorage>(sp =>
        {
            var configuration = sp.GetRequiredService<JobConfiguration>();
            var storage = sp.GetRequiredService<LocalStorage>();
            storage.Init(connectionString);
            return storage;
        });
    }

    public static void AddKuliJob<T>(this JobConfiguration configuration, string jobName) where T : class, IJob
    {
        configuration.ServiceCollection.AddKeyedScoped<IJob, T>($"kulijob.{jobName}");
    }
}
