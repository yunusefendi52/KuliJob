using System.Data;
using KuliJob.Storage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KuliJob.Sqlite;

internal class AppDbContext : BaseDbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public AppDbContext() : base()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var isEfMigration = Environment.GetCommandLineArgs().FirstOrDefault()?.EndsWith("ef.dll") == true;
        if (isEfMigration)
        {
            optionsBuilder.SetupDbContextOptions(":memory:");
        }
    }
}
