namespace KuliJob.Tests;

public abstract class BaseTest
{
    public IServiceProvider Services { get; protected set; } = null!;
    public IJobScheduler JobScheduler { get; protected set; } = null!;

    [SetUp]
    public async Task Init()
    {
        var (sp, jobScheduler) = await SetupServier.StartServerSchedulerAsync();
        Services = sp;
        JobScheduler = jobScheduler;
    }
}
