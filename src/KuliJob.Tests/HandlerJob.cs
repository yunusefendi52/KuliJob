namespace KuliJob.Tests;

public class HandlerJob : IJob
{
    public Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}