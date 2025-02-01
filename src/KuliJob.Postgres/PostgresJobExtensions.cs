using KuliJob;
using KuliJob.Postgres;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

public static class PostgresJobExtension
{
    public static void UsePostgreSQL(
        this JobConfiguration jobConfiguration,
        string connectionString,
        string schema = "kulijob",
        Logging.ILoggerFactory? loggerFactory = null)
    {
        var services = jobConfiguration.ServiceCollection;
        services.AddSingleton(_ => new PgDataSource(connectionString, schema));
        services.TryAddSingleton<PostgresJobStorage>();
        services.TryAddSingleton<IJobStorage>(sp =>
        {
            return sp.GetRequiredService<PostgresJobStorage>();
        });
    }
}
