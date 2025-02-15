using KuliJob;
using KuliJob.Sqlite;
using KuliJob.Storages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqliteJobExtensions
{
    public static void UseSqlite(this JobConfiguration jobConfiguration, string path)
    {
        jobConfiguration.ServiceCollection.TryAddSingleton<SqliteJobStorage>();
        // jobConfiguration.ServiceCollection.AddSingleton<IDbContextFactory<AppDbContext>, EFAppDbContextFactory>();
        // jobConfiguration.ServiceCollection.AddDbContextFactory<AppDbContext, EFAppDbContextFactory>();
        jobConfiguration.ServiceCollection.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
            return new AppDbContext(options);
        });
        jobConfiguration.ServiceCollection.AddDbContextPool<AppDbContext>(v =>
        {
            v.SetupDbContextOptions($"Data Source={path};");
        });
        jobConfiguration.ServiceCollection.TryAddSingleton<IJobStorage>(sp =>
        {
            var configuration = sp.GetRequiredService<JobConfiguration>();
            var storage = sp.GetRequiredService<SqliteJobStorage>();
            return storage;
        });
    }

    internal static void SetupDbContextOptions(this DbContextOptionsBuilder v, string connString)
    {
        v.UseSqlite(connString)
            .UseSnakeCaseNamingConvention();
    }
}

// internal class EFAppDbContextFactory(IServiceProvider serviceProvider) : IDbContextFactory<AppDbContext>
// {
//     public AppDbContext CreateDbContext()
//     {
//         var options = serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
//         return new AppDbContext(options);
//     }
// }
