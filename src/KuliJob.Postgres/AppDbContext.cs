using KuliJob.Storage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace KuliJob.Postgres;

internal class AppDbContext : BaseDbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    string? connectionString, schema;

    public AppDbContext() : base()
    {
    }

    public AppDbContext(string connectionString, string schema) : base()
    {
        this.connectionString = connectionString;
        this.schema = schema;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder($"{connectionString};Search Path={schema};");
        var searchPaths = connectionStringBuilder.SearchPath?.Split(',');
        optionsBuilder.UseNpgsql(connectionStringBuilder.ConnectionString, o =>
        {
            if (searchPaths is { Length: > 0 })
            {
                var mainSchema = searchPaths[0];
                o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, mainSchema);
            }
        }).UseSnakeCaseNamingConvention();
    }
}
