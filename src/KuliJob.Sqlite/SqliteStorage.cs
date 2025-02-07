using System.Runtime.CompilerServices;
using KuliJob.Internals;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection;
using SQLite;

namespace KuliJob.Sqlite;

internal class SqliteStorage(JobConfiguration configuration, MyClock myClock) : IJobStorage
{
    SQLiteConnection db = null!;

    public void Init(string connectionString)
    {
        db = new(connectionString);
    }

    public Task StartStorage(CancellationToken cancellationToken)
    {
        db.CreateTable<SqliteJobInput>();
        db.CreateTable<CronSqlite>();
        return Task.CompletedTask;
    }

    public Task InsertJob(Job jobInput)
    {
        if (db.Insert(jobInput.ToSqliteJobInput()) != 1)
        {
            throw new Exception("Failed add job");
        }
        return Task.CompletedTask;
    }

    public Task<Job?> FetchNextJob(CancellationToken cancellationToken = default)
    {
        try
        {
            var savePoint = db.SaveTransactionPoint();

            var queues = configuration.Queues;
            var now = myClock.GetUtcNow();
            var nextJob = db
                .Table<SqliteJobInput>()
                .OrderBy(v => v.Priority)
                .ThenBy(v => v.CreatedOn)
                .ThenBy(v => v.Id)
                .Where(v => v.JobState < JobState.Active && v.StartAfter < now && queues.Contains(v.Queue))
                .Take(1)
                .SingleOrDefault();
            if (nextJob is null)
            {
                db.Release(savePoint);
                return Task.FromResult<Job?>(null);
            }
            nextJob.JobState = JobState.Active;
            nextJob.StartedOn = myClock.GetUtcNow();
            nextJob.ServerName = configuration.ServerName;
            db.Update(nextJob);

            db.Release(savePoint);

            return Task.FromResult(nextJob?.ToJobInput());
        }
        catch (Exception)
        {
            db.Rollback();
            throw;
        }
    }

    public Task CompleteJobById(string jobId)
    {
        var jobInput = db.Find<SqliteJobInput>(jobId);
        jobInput.JobState = JobState.Completed;
        jobInput.CompletedOn = myClock.GetUtcNow();
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed complete job {jobInput.Id}");
        }
        return Task.CompletedTask;
    }

    public Task CancelJobById(string jobId)
    {
        var jobInput = db.Find<SqliteJobInput>(jobId);
        jobInput.JobState = JobState.Cancelled;
        jobInput.CancelledOn = myClock.GetUtcNow();
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed cancel job {jobInput.Id}");
        }
        return Task.CompletedTask;
    }

    public Task FailJobById(string jobId, string failedMessage)
    {
        var jobInput = db.Find<SqliteJobInput>(jobId);
        jobInput.JobState = JobState.Failed;
        jobInput.FailedOn = myClock.GetUtcNow();
        jobInput.FailedMessage = failedMessage;
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed fail job {jobInput.Id}");
        }
        return Task.CompletedTask;
    }

    public Task ResumeJob(string jobId)
    {
        var jobInput = db.Find<SqliteJobInput>(jobId);
        jobInput.JobState = JobState.Created;
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed to resume job {jobInput.Id}");
        }
        return Task.CompletedTask;
    }

    // public Task<JobInput?> GetJobByState(string jobId, JobState jobState)
    // {
    //     return Task.FromResult(db.Table<SqliteJobInput>()
    //         .Where(v => v.Id == jobId && v.JobState == jobState)
    //         .Select(v => v.ToJobInput())
    //         .SingleOrDefault());
    // }

    public Task<Job?> GetJobById(string jobId)
    {
        return Task.FromResult(db.Table<SqliteJobInput>()
            .Where(v => v.Id == jobId)
            .Select(v => v.ToJobInput())
            .SingleOrDefault());
    }

    public Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null)
    {
        var offset = (page - 1) * limit;
        var q = db.Table<SqliteJobInput>()
            .Skip(offset)
            .Take(limit)
            .OrderByDescending(v => v.StartedOn);
        if (jobState != null)
        {
            q = q.Where(v => v.JobState == jobState);
        }
        return Task.FromResult(q.Select(v => v.ToJobInput()));
    }

    public Task<Job> RetryJob(string jobId, int retryDelay)
    {
        var jobInput = db.Find<SqliteJobInput>(v => v.Id == jobId);
        jobInput.JobState = JobState.Retry;
        jobInput.RetryCount++;
        jobInput.StartAfter = jobInput.StartAfter.AddMilliseconds(retryDelay);
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed to retry job {jobInput.Id}");
        }
        return Task.FromResult(jobInput.ToJobInput());
    }

    public Task AddOrUpdateCron(Cron cron)
    {
        var cronSqlite = cron.ToCronSqlite();
        db.InsertOrReplace(cronSqlite);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Cron>> GetCrons()
    {
        var cronsSqlite = db.Table<CronSqlite>()
            .ToList()
            .Select(v => v.ToCron());
        return Task.FromResult(cronsSqlite);
    }

    public Task DeleteCron(Cron cron)
    {
        db.Delete<CronSqlite>(cron.Name);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        db.Dispose();
        return ValueTask.CompletedTask;
    }

    public async Task<Job?> GetJobByThrottle(string throttleKey, TimeSpan? throttleTime)
    {
        await Task.Yield();
        var jobInput = db.Table<SqliteJobInput>()
            .SingleOrDefault(v => v.ThrottleKey == throttleKey);
        if (jobInput is null)
        {
            return null;
        }
        if (throttleTime is not null)
        {
            var throttleOn = jobInput.CreatedOn.Add(throttleTime.Value);
            var now = myClock.GetUtcNow();
            var delta = now - throttleOn;
            if (delta.TotalMilliseconds >= 0)
            {
                return null;
            }
        }

        return jobInput.ToJobInput();
    }
}
