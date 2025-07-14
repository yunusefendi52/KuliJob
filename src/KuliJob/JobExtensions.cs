using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using KuliJob;

namespace Microsoft.Extensions.DependencyInjection;

public static class JobExtensions
{
    internal const string DefaultCronName = "k_queue_cron";

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
        if (!config.Queues.Add(DefaultCronName))
        {
            throw new Exception($"You cannot add reserved queue {DefaultCronName}");
        }
        configure?.Invoke(config);
        serviceCollection.TryAddSingleton<MyClock>();
        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton<JobServerScheduler>();
        serviceCollection.AddSingleton<IQueueJob>(sp => sp.GetRequiredService<JobServerScheduler>());
        serviceCollection.AddSingleton<IQueueExprJob>(sp => sp.GetRequiredService<JobServerScheduler>());
        serviceCollection.AddSingleton<JobServiceHosted>();
        serviceCollection.AddSingleton<Serializer>();
        serviceCollection.AddHostedService(static sp => sp.GetRequiredService<JobServiceHosted>());
        serviceCollection.AddSingleton<ExpressionSerializer>();
        config.AddKuliJob<ExprJob>("expr_job");
        config.AddKuliJob<CronJobHandler>();
        serviceCollection.AddSingleton<ICronJob, CronJob>();
        serviceCollection.AddSingleton<CronJobHostedService>();
    }

    public static void AddKuliJob<T>(this JobConfiguration configuration, string? jobName = null) where T : class, IJob
    {
        var tType = typeof(T);
        jobName ??= tType.Name;
        configuration.JobFactory.RegisterJob(jobName, tType);
    }
}
