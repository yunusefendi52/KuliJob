using System.ComponentModel.DataAnnotations.Schema;
using KuliJob.Storages;

namespace KuliJob.Storage.Data;

[Table("cron")]
public class CronDb
{
    public string Name { get; set; } = null!;
    public string CronExpression { get; set; } = null!;
    public string Data { get; set; } = null!;
    public string? Timezone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
