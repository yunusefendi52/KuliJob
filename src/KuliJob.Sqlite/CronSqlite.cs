using KuliJob.Storages;
using SQLite;

namespace KuliJob.Sqlite;

[Table("cron")]
public class CronSqlite
{
    [PrimaryKey]
    [Column("name")]
    public string Name { get; set; } = null!;
    [Column("cron_expression")]
    public string CronExpression { get; set; } = null!;
    [Column("data")]
    public string Data { get; set; } = null!;
    [Column("timezone")]
    public string? Timezone { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}


internal static class CronSqliteMapper
{
    public static Cron ToCron(this CronSqlite cronSqlite)
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

    public static CronSqlite ToCronSqlite(this Cron cron)
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

