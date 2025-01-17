namespace KuliJob.Tests;

public class HandlerJob : IJob
{
    public Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class DelayHandlerJob : IJob
{
    public async Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);
    }
}

public class ThrowsHandlerJob : IJob
{
    public Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        throw new Exception("ThrowsHandlerJob throws exception");
    }
}
