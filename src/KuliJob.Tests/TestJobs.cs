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
        var delay = context.JobData.ContainsKey("delay") ? context.JobData.GetInt("delay") : 500;
        await Task.Delay(delay, cancellationToken);
    }
}

public class ThrowsHandlerJob : IJob
{
    public Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        throw new Exception("ThrowsHandlerJob throws exception");
    }
}


public class CheckDataHandlerJob : IJob
{
    public async Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        var txtFile = context.JobData.GetString("txtFile")!;
        context.JobData.GetBool("myBool");
        context.JobData.GetInt("myInt");
        context.JobData.GetLong("myLong");
        context.JobData.GetDouble("myDouble");
        context.JobData.GetDateTime("myDateTime");
        context.JobData.GetDateTimeOffset("myDateOffset");
        await File.WriteAllTextAsync(txtFile, "check_data_handler", cancellationToken);
    }
}

public class ConditionalThrowsHandlerJob : IJob
{
    public Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        var throwAtCount = context.JobData.GetInt("throwAtCount");
        if (context.RetryCount <= throwAtCount)
        {
            throw new Exception("ConditionalThrowsHandlerJob throws exception");
        }

        return Task.CompletedTask;
    }
}
