using KuliJob;
using KuliJob.Postgres;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class PostgresJobExtension
{
    public static void UsePostgreSQL(this JobConfiguration jobConfiguration, string connectionString, string schema = "kulijob")
    {
        var services = jobConfiguration.ServiceCollection;
        services.AddNpgsqlSlimDataSource(connectionString, serviceKey: KeyedType.KuliJobDb);
        services.AddKeyedSingleton(KeyedType.Schema, schema);
        services.TryAddSingleton<PostgresJobStorage>();
        services.TryAddSingleton<IJobStorage>(sp =>
        {
            return sp.GetRequiredService<PostgresJobStorage>();
        });
    }
}
