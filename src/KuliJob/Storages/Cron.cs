namespace KuliJob.Storages;

public class Cron
{
    public Cron()
    {
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public required string Name { get; set; }
    public required string CronExpression { get; set; }
    public required string Data { get; set; }
    public string? TimeZone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
