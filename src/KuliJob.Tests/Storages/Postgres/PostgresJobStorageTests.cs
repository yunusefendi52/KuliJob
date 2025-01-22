using Dapper;
using KuliJob.Postgres;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace KuliJob.Tests.Storages.Postgres;

[Parallelizable]
public class PostgresJobStorageTests : BaseTest
{
    const string dbConnString = "Host=localhost;Username=postgres;Password=postgres;Include Error Detail=True";

    [Test]
    public async Task Can_Start_And_Migrate_PostgresJob()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(KeyedType.KuliJobDb);
        await using var conn = await dataSource.OpenConnectionAsync();
        await Assert.That(() => conn.QueryAsync("select * from kulijob.job")).IsEmpty();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
    }

    [Test]
    public async Task Can_Start_And_Migrate_PostgresJob_WithCustomSchema()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString, "myschemajob");
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(KeyedType.KuliJobDb);
        await using var conn = await dataSource.OpenConnectionAsync();
        await Assert.That(() => conn.QueryAsync("select * from kulijob.job")).IsNull();
        await Assert.That(() => conn.QueryAsync("select * from myschemajob.job")).IsEmpty();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
    }
}
