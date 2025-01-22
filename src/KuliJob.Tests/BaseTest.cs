namespace KuliJob.Tests;

public class BaseTest
{
    public IServiceProvider Services { get; protected set; } = null!;
    public IJobScheduler JobScheduler { get; protected set; } = null!;

    protected virtual void InitServices(IServiceCollection services)
    {
    }

    [SetUp]
    public async Task Init()
    {
        var (sp, jobScheduler) = await SetupServier.StartServerSchedulerAsync(InitServices);
        Services = sp;
        JobScheduler = jobScheduler;
    }

    public static async Task WaitJobTicks(int count = 1)
    {
        await Task.Delay(600 * count);
    }
}
