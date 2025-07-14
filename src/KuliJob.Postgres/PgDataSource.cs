using KuliJob.Storage.Data;
using Npgsql;

namespace KuliJob.Postgres;

internal class PgDataSource(string connectionString, string schema) : BaseDataSource
{
    internal string Schema { get; set; } = schema;

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }

    internal override AppDbContext GetAppDbContext()
    {
        var dbContext = new AppDbContext(connectionString, Schema);
        // dbContext.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.
        return dbContext;
    }
}