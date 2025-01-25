using KuliJob;
using KuliJob.Sqlite;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqliteJobExtensions
{
    public static void UseSqlite(this JobConfiguration jobConfiguration, string connectionString)
    {
        jobConfiguration.ServiceCollection.TryAddSingleton<SqliteStorage>();
        jobConfiguration.ServiceCollection.TryAddSingleton<IJobStorage>(sp =>
        {
            var configuration = sp.GetRequiredService<JobConfiguration>();
            var storage = sp.GetRequiredService<SqliteStorage>();
            storage.Init(connectionString);
            return storage;
        });
    }
}
