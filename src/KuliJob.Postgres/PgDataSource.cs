using Npgsql;

namespace KuliJob.Postgres;

internal class PgDataSource(string connectionString, string schema)
{
    internal string Schema { get; set; } = schema;

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }
}