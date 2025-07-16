namespace KuliJob;

public class CronBuilder
{
    public required string CronName { get; set; }
    public required string CronExpression { get; set; }
    public string? TimeZoneId { get; set; }
}
