namespace KuliJob.Internals;

internal class MyClock
{
    public virtual DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }

    public virtual DateTimeOffset GetNow()
    {
        return DateTimeOffset.Now;
    }
}
