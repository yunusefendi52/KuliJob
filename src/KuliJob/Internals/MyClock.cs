namespace KuliJob.Internals;

internal class MyClock
{
    internal TimeSpan AddTimeBy { get; set; }

    public virtual DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow.Add(AddTimeBy);
    }
}
