namespace KuliJob.Storage.Data;

internal abstract class BaseDataSource()
{
    internal abstract BaseDbContext GetAppDbContext();
}