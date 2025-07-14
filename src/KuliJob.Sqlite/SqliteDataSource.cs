using KuliJob.Storage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KuliJob.Sqlite;

internal class SqliteDataSource(string path) : BaseDataSource
{
    internal override AppDbContext GetAppDbContext()
    {
        var builder = new DbContextOptionsBuilder();
        builder.SetupDbContextOptions($"Data Source={path}");
        var dbContext = new AppDbContext(builder.Options);
        // dbContext.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.
        return dbContext;
    }
}