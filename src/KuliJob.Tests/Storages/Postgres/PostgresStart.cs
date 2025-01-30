using Dapper;
using Npgsql;

namespace KuliJob.Tests.Storages.Postgres;

public class PostgresStart(string dbConnString) : IAsyncDisposable
{
    readonly NpgsqlConnection conn = new(dbConnString);
    string connString = null!;
    string databaseName = null!;

    public async Task<string> Start()
    {
        databaseName = $"kulijob_db_test_{Guid.NewGuid().ToString()[..8]}";
        await conn.ExecuteAsync($"create database {databaseName}");
        await conn.QuerySingleAsync("""
        select 1 from pg_catalog.pg_database
        where datname = @dbName
        """, new
        {
            dbName = databaseName,
        });
        connString = $"{dbConnString};Database={databaseName};";
        return connString;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await conn.ExecuteAsync($"drop database {databaseName} with (force)");
        await conn.DisposeAsync();
    }
}
