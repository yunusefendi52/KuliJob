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
        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton<JobServerScheduler>();
        serviceCollection.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<JobServerScheduler>());
        serviceCollection.AddSingleton<JobServiceHosted>();
        serviceCollection.AddSingleton<Serializer>();
        serviceCollection.AddHostedService(static sp => sp.GetRequiredService<JobServiceHosted>());
        serviceCollection.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        serviceCollection.AddSingleton<ExpressionSerializer>();
        config.AddKuliJob<ExprJob>("expr_job");
    }

    public static void AddKuliJob<T>(this JobConfiguration configuration, string jobName) where T : class, IJob
    {
        configuration.ServiceCollection.AddKeyedScoped<IJob, T>($"kulijob.{jobName}");
    }
}
