using System.ComponentModel.DataAnnotations.Schema;
using KuliJob.Storages;

namespace KuliJob.Sqlite;

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

internal static class CronSqliteMapper
{
    public static Cron ToCron(this CronDb cronSqlite)
    {
        return new()
        {
            CronExpression = cronSqlite.CronExpression,
            Data = cronSqlite.Data,
            Name = cronSqlite.Name,
            TimeZone = cronSqlite.Timezone,
            CreatedAt = cronSqlite.CreatedAt,
            UpdatedAt = cronSqlite.UpdatedAt,
        };
    }

    public static CronDb ToCronSqlite(this Cron cron)
    {
        return new()
        {
            CreatedAt = cron.CreatedAt,
            CronExpression = cron.CronExpression,
            Data = cron.Data,
            Name = cron.Name,
            Timezone = cron.TimeZone,
            UpdatedAt = cron.UpdatedAt,
        };
    }
}
