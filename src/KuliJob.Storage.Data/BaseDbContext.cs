using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KuliJob.Storage.Data;

internal class BaseDbContext : DbContext
{
    public BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    public BaseDbContext() : base()
    {
    }

    public DbSet<CronDb> Crons => Set<CronDb>();
    public DbSet<JobInputDb> Jobs => Set<JobInputDb>();
    public DbSet<JobStateDb> JobStateDbs => Set<JobStateDb>();
    public DbSet<JobServerEntry> JobServers => Set<JobServerEntry>();

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
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}

[Table("job_state")]
[Index(nameof(Id))]
[Index(nameof(Id), nameof(JobId), IsUnique = true)]
internal class JobStateDb
{
    public required Guid Id { get; set; }
    public required Guid JobId { get; set; }
    public JobState JobState { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
