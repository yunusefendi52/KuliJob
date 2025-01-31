namespace KuliJob.Internals;

internal class JobFactory(IServiceCollection services)
{
    readonly Dictionary<string, Type> _services = [];

    public void RegisterJob(string key, Type serviceType)
    {
        services.AddScoped(serviceType);
        _services.Add(key, serviceType);
    }

    public IJob? ResolveService(IServiceProvider serviceProvider, string key)
    {
        if (_services.TryGetValue(key, out var serviceType))
        {
            return (IJob)serviceProvider.GetRequiredService(serviceType);
        }
        
        return null;
    }
}