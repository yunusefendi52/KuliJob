namespace KuliJob;

public interface IJob
{
    Task Execute(JobContext context);
}
