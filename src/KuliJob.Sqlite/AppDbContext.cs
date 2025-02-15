using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace KuliJob.Sqlite;

internal class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public AppDbContext() : base()
    {
    }

    public DbSet<CronDb> Crons => Set<CronDb>();
    public DbSet<JobInputDb> Jobs => Set<JobInputDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CronDb>()
            .HasKey(v => v.Name);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                            || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToUtcDateTimeTicksConverter());
                }
            }
        }
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

internal class DateTimeOffsetToUtcDateTimeTicksConverter(ConverterMappingHints? mappingHints = null) : ValueConverter<DateTimeOffset, long>(
        v => v.UtcDateTime.Ticks,
        v => new DateTimeOffset(v, new TimeSpan(0, 0, 0)),
        mappingHints)
{
    public static ValueConverterInfo DefaultInfo { get; } = new(typeof(DateTimeOffset), typeof(long), i => new DateTimeOffsetToUtcDateTimeTicksConverter(i.MappingHints));
}
