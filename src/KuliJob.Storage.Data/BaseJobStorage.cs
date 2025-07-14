using System.Text.Json;
using KuliJob.Internals;
using KuliJob.Storages;
using Microsoft.EntityFrameworkCore;

namespace KuliJob.Storage.Data;

internal abstract class BaseJobStorage(
    BaseDataSource dataSource,
    JobConfiguration configuration,
    MyClock myClock) : IJobStorage
{
    public event EventHandler<Guid>? NextJobIdNotifier;

    protected void NotifyNewJob(Guid nextId)
    {
        NextJobIdNotifier?.Invoke(this, nextId);
    }

    public virtual async Task StartStorage(CancellationToken cancellationToken)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await AddServer(dbContext);
        await tx.CommitAsync(cancellationToken);
    }

    protected async Task AddServer(BaseDbContext dbContext)
    {
        // Insert server
        var dataJson = JsonSerializer.Serialize(new
        {
            Worker = configuration.Worker,
            Queues = configuration.Queues.ToList(),
            StartedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.JobServers
            .Where(v => v.Id == configuration.ServerName)
            .ExecuteDeleteAsync();
        dbContext.JobServers.Add(new()
        {
            Id = configuration.ServerName,
            Data = dataJson,
            LastHeartbeat = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task CancelJobById(Guid jobId)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var jobStateId = GenerateId();
        var job = await dbContext.Jobs.SingleOrDefaultAsync(v => v.Id == jobId && v.JobState < JobState.Completed);
        if (job == null)
        {
            throw new Exception($"Job not found {jobId}");
        }
        job.JobStateId = jobStateId;
        job.JobState = JobState.Cancelled;
        dbContext.JobStateDbs.Add(new()
        {
            Id = jobStateId,
            JobId = jobId,
            CreatedAt = myClock.GetUtcNow(),
            JobState = job.JobState,
        });
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task CompleteJobById(Guid jobId)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var jobStateId = GenerateId();
        var job = await dbContext.Jobs.SingleOrDefaultAsync(v => v.Id == jobId && v.JobState == JobState.Active);
        if (job == null)
        {
            throw new Exception($"Job not found {jobId}");
        }
        job.JobStateId = jobStateId;
        job.JobState = JobState.Completed;
        dbContext.JobStateDbs.Add(new()
        {
            Id = jobStateId,
            JobId = jobId,
            CreatedAt = myClock.GetUtcNow(),
            JobState = job.JobState,
        });
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task FailJobById(Guid jobId, string failedMessage)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var jobStateId = GenerateId();
        var job = await dbContext.Jobs.SingleOrDefaultAsync(v => v.Id == jobId && v.JobState == JobState.Active);
        if (job == null)
        {
            throw new Exception($"Job to fail not found {jobId}");
        }
        job.JobStateId = jobStateId;
        job.JobState = JobState.Failed;
        dbContext.JobStateDbs.Add(new()
        {
            Id = jobStateId,
            JobId = jobId,
            CreatedAt = myClock.GetUtcNow(),
            JobState = job.JobState,
            Message = failedMessage,
        });
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public virtual async Task<Job?> FetchNextJob(Guid? nextId, CancellationToken cancellationToken = default)
    {

        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var jobStateId = Guid.NewGuid();
        var queues = configuration.Queues.ToArray();
        var now = myClock.GetUtcNow();

        var nextJob = await dbContext.Jobs
            .Where(v => v.JobState < JobState.Active && v.StartAfter < now && queues.Contains(v.Queue))
            .OrderBy(v => v.Priority).ThenBy(v => v.CreatedOn).ThenBy(v => v.Id)
            .Take(1)
            .FirstOrDefaultAsync(cancellationToken);
        if (nextJob is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return null;
        }

        nextJob.JobState = JobState.Active;
        nextJob.JobStateId = jobStateId;
        nextJob.ServerName = configuration.ServerName;

        dbContext.JobStateDbs.Add(new()
        {
            Id = jobStateId,
            JobId = nextJob.Id,
            CreatedAt = now,
            JobState = nextJob.JobState,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        var jobInput = nextJob?.ToJobData();
        if (jobInput is not null)
        {
            jobInput.StartedOn = now;
        }

        return jobInput;
    }

    public async Task<Job?> GetJobById(Guid jobId)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var job = await dbContext.Jobs.AsNoTracking()
            .Select(v => new
            {
                Job = v,
                JobStates = dbContext.JobStateDbs.AsNoTracking().Where(t => t.JobId == v.Id && t.Id == v.JobStateId).First(),
            })
            .Where(v => v.Job.Id == jobId)
            .SingleOrDefaultAsync(v => v.Job.Id == jobId);
        if (job == null)
        {
            return null;
        }

        var jobInput = job.Job.ToJobData();
        var currJobState = job.JobStates;
        if (currJobState is not null)
        {
            if (currJobState.JobState == JobState.Completed)
            {
                jobInput.CompletedOn = currJobState.CreatedAt;
            }
            else if (currJobState.JobState == JobState.Failed)
            {
                jobInput.FailedOn = currJobState.CreatedAt;
            }
            else if (currJobState.JobState == JobState.Cancelled)
            {
                jobInput.CancelledOn = currJobState.CreatedAt;
            }
            jobInput.StateMessage = currJobState.Message;
        }
        return jobInput;
    }

    public async Task<List<JobStateEntry>?> GetJobStates(Guid jobId)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var data = await dbContext.JobStateDbs
            .AsNoTracking()
            .Where(v => v.JobId == jobId)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new JobStateEntry
            {
                Id = v.Id,
                JobId = v.JobId,
                CreatedAt = v.CreatedAt,
                JobState = v.JobState,
                Message = v.Message,
            })
            .ToListAsync();
        return data;
    }

    public virtual async Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var query = dbContext.Jobs.AsNoTracking()
            .Select(v => new
            {
                Job = v,
                JobState = dbContext.JobStateDbs.Where(t => t.JobId == v.Id).OrderByDescending(v => v.CreatedAt).First(),
            });

        if (jobState is not null)
        {
            query = query.Where(j => j.JobState.JobState == jobState);
        }

        var results = await query.OrderByDescending(j => j.Job.CreatedOn)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(v => v.Job.ToJobData(v.JobState.Message, v.JobState.CreatedAt))
            .ToListAsync();
        return results;
    }

    public virtual async Task InsertJob(Job jobInput)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var jobStateId = GenerateId();
            var jobState = jobInput.JobState;
            var now = myClock.GetUtcNow();

            dbContext.JobStateDbs.Add(new()
            {
                Id = jobStateId,
                JobId = jobInput.Id,
                CreatedAt = now,
                JobState = jobState
            });

            dbContext.Jobs.Add(new()
            {
                Id = jobInput.Id,
                JobName = jobInput.JobName,
                JobData = jobInput.JobData,
                JobState = jobInput.JobState,
                JobStateId = jobStateId,
                RetryMaxCount = jobInput.RetryMaxCount,
                RetryCount = jobInput.RetryCount,
                RetryDelayMs = jobInput.RetryDelayMs,
                StartAfter = jobInput.StartAfter,
                CreatedOn = jobInput.CreatedOn,
                Queue = jobInput.Queue,
                Priority = jobInput.Priority,
                ThrottleKey = !string.IsNullOrEmpty(jobInput.ThrottleKey) ? jobInput.ThrottleKey : null,
                ThrottleSeconds = jobInput.ThrottleSeconds
            });

            // Save changes
            var rows = await dbContext.SaveChangesAsync();
            if (rows <= 0)
            {
                throw new Exception($"Job not added {jobInput.Id} - {jobInput.JobName}");
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ResumeJob(Guid jobId)
    {
        // TODO: REMOVE THIS?
        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var jobStateId = GenerateId();
        var job = await dbContext.Jobs.SingleOrDefaultAsync(v => v.Id == jobId && v.JobState == JobState.Cancelled);
        if (job == null)
        {
            throw new Exception($"Job not found {jobId}");
        }
        job.JobStateId = jobStateId;
        job.JobState = JobState.Created;
        dbContext.JobStateDbs.Add(new()
        {
            Id = jobStateId,
            JobId = jobId,
            CreatedAt = myClock.GetUtcNow(),
            JobState = job.JobState,
        });
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task RetryJob(Guid jobId, int retryDelay)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var jobStateId = GenerateId();
        var job = await dbContext.Jobs.SingleOrDefaultAsync(v => v.Id == jobId && (v.JobState == JobState.Failed || v.JobState == JobState.Cancelled));
        if (job == null)
        {
            throw new Exception($"Job to retry not found {jobId}");
        }
        job.JobStateId = jobStateId;
        job.JobState = JobState.Retry;
        job.StartAfter = job.StartAfter.Add(TimeSpan.FromMilliseconds(retryDelay));
        job.RetryCount++;
        dbContext.JobStateDbs.Add(new()
        {
            Id = jobStateId,
            JobId = jobId,
            CreatedAt = myClock.GetUtcNow(),
            JobState = job.JobState,
        });
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async Task AddOrUpdateCron(Cron cron)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var now = myClock.GetUtcNow();

        var existingCron = await dbContext.Crons
            .SingleOrDefaultAsync(c => c.Name == cron.Name);

        if (existingCron != null)
        {
            // Update existing record
            existingCron.CronExpression = cron.CronExpression;
            existingCron.Data = cron.Data;
            existingCron.Timezone = cron.TimeZone;
            existingCron.UpdatedAt = now;
        }
        else
        {
            dbContext.Crons.Add(new()
            {
                Name = cron.Name,
                CronExpression = cron.CronExpression,
                Data = cron.Data,
                Timezone = cron.TimeZone,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Cron>> GetCrons()
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var list = await dbContext.Crons.AsNoTracking()
            .Select(v => new Cron
            {
                CronExpression = v.CronExpression,
                Data = v.Data,
                Name = v.Name,
                CreatedAt = v.CreatedAt,
                TimeZone = v.Timezone,
                UpdatedAt = v.UpdatedAt,
            }).ToListAsync();
        return list;
    }

    public async Task DeleteCron(string name)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        await dbContext.Crons.Where(v => v.Name == name).ExecuteDeleteAsync();
    }

    public async Task<Job?> GetJobByThrottle(string throttleKey)
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var job = await dbContext.Jobs.AsNoTracking()
            .Where(v => v.ThrottleKey == throttleKey)
            .OrderByDescending(v => v.CreatedOn)
            .Take(1)
            .FirstOrDefaultAsync();
        return job?.ToJobData();
    }

    static Guid GenerateId()
    {
        return Guid.NewGuid();
    }

    public async Task UpdateHeartbeatServer()
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var now = myClock.GetUtcNow();
        await dbContext.JobServers.Where(v => v.Id == configuration.ServerName)
            .ExecuteUpdateAsync(v => v.SetProperty(t => t.LastHeartbeat, now));
    }

    public async Task<List<JobServerEntry>> GetJobServers()
    {
        await using var dbContext = dataSource.GetAppDbContext();
        var jobServers = await dbContext.JobServers.AsNoTracking()
            .ToListAsync();
        return jobServers;
    }

    public async Task RemoveInactiveServers()
    {
        await using var dbContext = dataSource.GetAppDbContext();
        // await using var tx = await dbContext.Database.BeginTransactionAsync();
        var minLastHeartbeat = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMilliseconds(configuration.ServerPurgeInactiveMs));
        await dbContext.JobServers
            .Where(v => v.LastHeartbeat < minLastHeartbeat)
            .ExecuteDeleteAsync();
        // await tx.CommitAsync();
    }
}
