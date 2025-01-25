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
        var delay = context.JobData.ContainsKey("delay") ? context.JobData.GetValue<int>("delay") : 500;
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
        var txtFile = context.JobData.GetValue<string>("txtFile")!;
        context.JobData.GetValue<bool>("myBool");
        context.JobData.GetValue<int>("myInt");
        context.JobData.GetValue<long>("myLong");
        context.JobData.GetValue<double>("myDouble");
        context.JobData.GetValue<DateTime>("myDateTime");
        context.JobData.GetValue<DateTimeOffset>("myDateOffset");
        await File.WriteAllTextAsync(txtFile, "check_data_handler", cancellationToken);
    }
}

public class ConditionalThrowsHandlerJob : IJob
{
    public Task Execute(JobContext context, CancellationToken cancellationToken = default)
    {
        var throwAtCount = context.JobData.GetValue<int>("throwAtCount");
        if (context.RetryCount <= throwAtCount)
        {
            throw new Exception("ConditionalThrowsHandlerJob throws exception");
        }

        return Task.CompletedTask;
    }
}
