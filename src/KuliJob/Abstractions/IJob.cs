namespace KuliJob.Abstractions;

public interface IJob
{
    Task ExecuteTask(JobContext jobContext, CancellationToken cancellationToken = default);
}
