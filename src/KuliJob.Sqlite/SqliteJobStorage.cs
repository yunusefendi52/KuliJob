using System.Data;
using KuliJob.Internals;
using KuliJob.Storages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KuliJob.Sqlite;

internal class SqliteJobStorage(
    JobConfiguration configuration,
    MyClock myClock,
    IServiceProvider serviceProvider) : IJobStorage
{
    public async Task StartStorage(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
    }

    public async Task InsertJob(Job jobInput)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Jobs.AddAsync(jobInput.ToSqliteJobInput());
        await dbContext.SaveChangesAsync();
    }

    public async Task<Job?> FetchNextJob(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var queues = configuration.Queues;
        var now = myClock.GetUtcNow();
        var nextJob = await dbContext
            .Jobs
            .AsNoTracking()
            .OrderBy(v => v.Priority)
            .ThenBy(v => v.CreatedOn)
            .ThenBy(v => v.Id)
            .Where(v => v.JobState < JobState.Active && v.StartAfter < now && queues.Contains(v.Queue!))
            .Take(1)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (nextJob is null)
        {
            return null;
        }
        nextJob.JobState = JobState.Active;
        nextJob.StartedOn = myClock.GetUtcNow();
        nextJob.ServerName = configuration.ServerName;
        dbContext.Jobs.Update(nextJob);
        await dbContext.SaveChangesAsync(cancellationToken);
        return nextJob.ToJobInput();
    }

    public async Task CompleteJobById(string jobId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext.Jobs.SingleAsync(v => v.Id == jobId);
        jobInput.JobState = JobState.Completed;
        jobInput.CompletedOn = myClock.GetUtcNow();
        await dbContext.SaveChangesAsync();
    }

    public async Task CancelJobById(string jobId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext.Jobs.SingleAsync(v => v.Id == jobId);
        jobInput.JobState = JobState.Cancelled;
        jobInput.CancelledOn = myClock.GetUtcNow();
        await dbContext.SaveChangesAsync();
    }

    public async Task FailJobById(string jobId, string failedMessage)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext.Jobs.SingleAsync(v => v.Id == jobId);
        jobInput.JobState = JobState.Failed;
        jobInput.FailedOn = myClock.GetUtcNow();
        jobInput.FailedMessage = failedMessage;
        await dbContext.SaveChangesAsync();
    }

    public async Task ResumeJob(string jobId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext.Jobs.SingleAsync(v => v.Id == jobId);
        jobInput.JobState = JobState.Created;
        await dbContext.SaveChangesAsync();
    }

    public async Task<Job?> GetJobById(string jobId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext.Jobs
            .AsNoTracking()
            .SingleAsync(v => v.Id == jobId);
        return jobInput.ToJobInput();
    }

    public async Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var offset = (page - 1) * limit;
        var q = dbContext.Jobs
            .Skip(offset)
            .Take(limit)
            .OrderByDescending(v => v.CreatedOn)
            .AsQueryable();
        if (jobState != null)
        {
            q = q.Where(v => v.JobState == jobState);
        }
        var jobs = await q.Select(v => v.ToJobInput()).ToListAsync();
        return jobs;
    }

    public async Task<Job> RetryJob(string jobId, int retryDelay)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext.Jobs.SingleAsync(v => v.Id == jobId);
        jobInput.JobState = JobState.Retry;
        jobInput.RetryCount++;
        jobInput.StartAfter = jobInput.StartAfter.AddMilliseconds(retryDelay);
        await dbContext.SaveChangesAsync();
        return jobInput.ToJobInput();
    }

    public async Task AddOrUpdateCron(Cron cron)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // TODO: Check a better way to insert or update. e.g using on conflict?
        var isExists = await dbContext.Crons
            .AsNoTracking()
            .AnyAsync(v => v.Name == cron.Name);
        if (isExists)
        {
            dbContext.Crons.Update(cron.ToCronSqlite());
        }
        else
        {
            dbContext.Crons.Add(cron.ToCronSqlite());
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Cron>> GetCrons()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cronsSqlite = await dbContext.Crons
            .AsNoTracking()
            .Select(v => v.ToCron())
            .ToListAsync();
        return cronsSqlite;
    }

    public async Task DeleteCron(string name)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cron = await dbContext.Crons.SingleAsync(v => v.Name == name);
        dbContext.Crons.Remove(cron);
        await dbContext.SaveChangesAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async Task<Job?> GetJobByThrottle(string throttleKey)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobInput = await dbContext
            .Jobs
            .OrderByDescending(v => v.CreatedOn)
            .FirstOrDefaultAsync(v => v.ThrottleKey == throttleKey);
        if (jobInput is null)
        {
            return null;
        }

        return jobInput.ToJobInput();
    }
}
