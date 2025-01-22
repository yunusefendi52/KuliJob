using System.Runtime.CompilerServices;
using SQLite;

namespace KuliJob.Storages;

internal class LocalStorage(JobConfiguration configuration, [FromKeyedServices("kulijob_timeprovider")] TimeProvider timeProvider) : IJobStorage
{
    SQLiteConnection db = null!;

    public void Init(string connectionString)
    {
        db = new(connectionString);
    }

    public Task StartStorage()
    {
        db.CreateTable<SqliteJobInput>();
        return Task.CompletedTask;
    }

    public Task InsertJob(JobInput jobInput)
    {
        if (db.Insert(jobInput.ToSqliteJobInput()) != 1)
        {
            throw new Exception("Failed add job");
        }
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<JobInput> FetchNextJob([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Need handle concurrency issue with multiple worker
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(configuration.MinPollingIntervalMs), timeProvider, cancellationToken);
            while (true)
            {
                db.BeginTransaction();
                var jobs = db
                    .Table<SqliteJobInput>()
                    .OrderBy(v => v.CreatedOn)
                    .ThenBy(v => v.Id)
                    .Where(v => v.JobState < JobState.Active && v.StartAfter < DateTimeOffset.UtcNow)
                    .Take(configuration.Worker)
                    .ToArray();
                if (jobs.Length == 0)
                {
                    db.Commit();
                    break;
                }
                foreach (var job in jobs)
                {
                    job.JobState = JobState.Active;
                    job.StartedOn = DateTimeOffset.UtcNow;
                    if (db.Update(job) == 1)
                    {
                        yield return job.ToJobInput();
                    }
                    else
                    {
                        throw new ArgumentException($"Error update fetched job {job.Id} {job.JobName}");
                    }
                }
                db.Commit();
            }
        }
    }

    public Task CompleteJobById(JobInput jobInput)
    {
        jobInput.JobState = JobState.Completed;
        jobInput.CompletedOn = DateTimeOffset.UtcNow;
        if (db.Update(jobInput.ToSqliteJobInput()) != 1)
        {
            throw new ArgumentException($"Failed complete job {jobInput.Id}");
        }
        return Task.CompletedTask;
    }

    public Task CancelJobById(string jobId)
    {
        var jobInput = db.Find<SqliteJobInput>(jobId);
        jobInput.JobState = JobState.Cancelled;
        jobInput.CancelledOn = DateTimeOffset.UtcNow;
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed cancel job {jobInput.Id}");
        }
        return Task.CompletedTask;
    }

    public Task FailJobById(JobInput jobInput, string failedMessage)
    {
        jobInput.JobState = JobState.Failed;
        jobInput.FailedOn = DateTimeOffset.UtcNow;
        jobInput.FailedMessage = failedMessage;
        if (db.Update(jobInput.ToSqliteJobInput()) != 1)
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

    public Task<JobInput?> GetJobById(string jobId)
    {
        return Task.FromResult(db.Table<SqliteJobInput>()
            .Where(v => v.Id == jobId)
            .Select(v => v.ToJobInput())
            .SingleOrDefault());
    }

    public Task<IEnumerable<JobInput>> GetLatestJobs(int page, int limit, JobState? jobState = null)
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

    public Task<JobInput> RetryJob(string jobId, int retryDelay)
    {
        var jobInput = db.Find<SqliteJobInput>(jobId);
        jobInput.JobState = JobState.Retry;
        jobInput.RetryCount++;
        jobInput.StartAfter = jobInput.StartAfter.AddMilliseconds(retryDelay);
        if (db.Update(jobInput) != 1)
        {
            throw new ArgumentException($"Failed to retry job {jobInput.Id}");
        }
        return Task.FromResult(jobInput.ToJobInput());
    }
}

internal class SqliteJobInput
{
    [PrimaryKey]
    public string Id { get; set; } = null!;
    [NotNull]
    [Indexed]
    public string JobName { get; set; } = null!;
    [NotNull]
    public string JobData { get; set; } = null!;
    [Indexed]
    public JobState JobState { get; set; }
    [Indexed]
    public DateTimeOffset StartAfter { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public DateTimeOffset? CancelledOn { get; set; }
    public DateTimeOffset? FailedOn { get; set; }
    public string? FailedMessage { get; set; }
    [Indexed]
    public DateTimeOffset CreatedOn { get; set; }
    public int RetryMaxCount { get; set; }
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; }
}

internal static class JobInputMapper
{
    public static JobInput ToJobInput(this SqliteJobInput sqliteJobInput)
    {
        return new JobInput
        {
            Id = sqliteJobInput.Id,
            JobName = sqliteJobInput.JobName,
            JobData = sqliteJobInput.JobData,
            JobState = sqliteJobInput.JobState,
            StartAfter = sqliteJobInput.StartAfter,
            StartedOn = sqliteJobInput.StartedOn,
            CompletedOn = sqliteJobInput.CompletedOn,
            CancelledOn = sqliteJobInput.CancelledOn,
            FailedOn = sqliteJobInput.FailedOn,
            FailedMessage = sqliteJobInput.FailedMessage,
            CreatedOn = sqliteJobInput.CreatedOn,
            RetryCount = sqliteJobInput.RetryCount,
            RetryDelayMs = sqliteJobInput.RetryDelayMs,
            RetryMaxCount = sqliteJobInput.RetryMaxCount,
        };
    }

    public static SqliteJobInput ToSqliteJobInput(this JobInput jobInput)
    {
        return new SqliteJobInput
        {
            Id = jobInput.Id,
            JobName = jobInput.JobName,
            JobData = jobInput.JobData,
            JobState = jobInput.JobState,
            StartAfter = jobInput.StartAfter,
            StartedOn = jobInput.StartedOn,
            CompletedOn = jobInput.CompletedOn,
            CancelledOn = jobInput.CancelledOn,
            FailedOn = jobInput.FailedOn,
            FailedMessage = jobInput.FailedMessage,
            CreatedOn = jobInput.CreatedOn,
            RetryCount = jobInput.RetryCount,
            RetryDelayMs = jobInput.RetryDelayMs,
            RetryMaxCount = jobInput.RetryMaxCount,
        };
    }
}
