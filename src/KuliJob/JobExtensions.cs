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
        var jobFactory = new JobFactory(serviceCollection);
        serviceCollection.AddSingleton(jobFactory);
        var config = new JobConfiguration()
        {
            ServiceCollection = serviceCollection,
            JobFactory = jobFactory,
        };
        configure?.Invoke(config);
        serviceCollection.TryAddSingleton<MyClock>();
        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton<JobServerScheduler>();
        serviceCollection.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<JobServerScheduler>());
        serviceCollection.AddSingleton<JobServiceHosted>();
        serviceCollection.AddSingleton<Serializer>();
        serviceCollection.AddHostedService(static sp => sp.GetRequiredService<JobServiceHosted>());
        serviceCollection.AddSingleton<ExpressionSerializer>();
        config.AddKuliJob<ExprJob>("expr_job");
        config.AddKuliJob<CronJobHandler>();
        serviceCollection.AddSingleton<ICronJob, CronJob>();
        serviceCollection.AddSingleton<CronJobHostedService>();
        serviceCollection.AddHostedService(static sp => sp.GetRequiredService<CronJobHostedService>());
    }

    public static void AddKuliJob<T>(this JobConfiguration configuration, string? jobName = null) where T : class, IJob
    {
        var tType = typeof(T);
        jobName ??= tType.Name;
        configuration.JobFactory.RegisterJob(jobName, tType);
    }
}
