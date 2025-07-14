using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Dapper;
using KuliJob.Internals;
using KuliJob.Storage.Data;
using KuliJob.Storages;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KuliJob.Postgres;

internal class PostgresJobStorage(
    PgDataSource dataSource,
    JobConfiguration configuration,
    MyClock myClock) : BaseJobStorage(dataSource, configuration, myClock)
{
    readonly JobConfiguration configuration = configuration;
    readonly MyClock myClock = myClock;

    readonly string schema = dataSource.Schema;

    readonly string channelName = "kulijob_ch";

    public override async Task StartStorage(CancellationToken cancellationToken)
    {
        await base.StartStorage(cancellationToken);

        StartListenToNewJob(cancellationToken);
    }

    void StartListenToNewJob(CancellationToken cancellationToken = default)
    {
        if (!configuration.ListenNotifyNewJobEnabled)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
            await conn.ExecuteAsync($"listen {channelName}");
            conn.Notification += (s, e) =>
            {
                if (Guid.TryParse(e.Payload, out var nextId))
                {
                    NotifyNewJob(nextId);
                }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                await conn.WaitAsync(cancellationToken);
            }
        }, cancellationToken);
    }

    public override async Task<Job?> FetchNextJob(Guid? nextId, CancellationToken cancellationToken = default)
    {
        var jobStateId = Guid.NewGuid();
        var startedOn = myClock.GetUtcNow();
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        var nextJob = await conn.QuerySingleOrDefaultAsync<PostgresJobInput>($"""
        with locked_job as (
            select j.id from {schema}.job j
            where j.job_state < @jobState
                and j.start_after < @now
                and j.queue = ANY(@queues)
                {(nextId != default ? "and j.id = @jobId" : null)}
            order by j.priority, j.created_on, j.id
            limit 1
            for update skip locked
        )

        update {schema}.job job
        set
            job_state = @jobState,
            job_state_id = @jobStateId,
            server_name = @serverName
        from locked_job
        where job.id = locked_job.id
        returning job.*;
        """, new
        {
            now = startedOn,
            serverName = configuration.ServerName,
            jobStateId = jobStateId,
            queues = configuration.Queues.ToArray(),
            jobState = (int)JobState.Active,
            jobId = nextId,
        });

        if (nextJob == null)
        {
            await tx.RollbackAsync(cancellationToken);
            return null;
        }

        await conn.ExecuteAsync($"""
        insert into {schema}.job_state (id, job_id, job_state, message, created_at)
        values (@id, @jobId, '{(int)JobState.Active}', null, @now)
        """, new
        {
            now = startedOn,
            id = jobStateId,
            jobId = nextJob.id,
        });

        await tx.CommitAsync(cancellationToken);

        var jobInput = nextJob?.ToJobInput();
        if (jobInput is not null)
        {
            jobInput.StartedOn = startedOn;
        }
        return jobInput;
    }

    public override async Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var results = await conn.QueryAsync<PostgresJobInput>($"""
        select js.*, j.*, js.message "state_message", js.created_at "state_created_at" from {schema}.job j
        left join {schema}.job_state js on js.job_id = j.id and js.id = j.job_state_id
        {(jobState is not null ? "where j.job_state = @jobState" : null)}
        order by j.created_on desc
        limit @limit
        offset @offset
        """, new
        {
            limit,
            offset = (page - 1) * limit,
            jobState = jobState is null ? (short?)null : (short)jobState,
        });
        return results.Select(v => v.ToJobInput());
    }

    public override async Task InsertJob(Job jobInput)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var jobStateId = GenerateId();
        var jobState = jobInput.JobState;
        var now = myClock.GetUtcNow();

        await conn.ExecuteAsync($"""
        insert into {schema}.job_state (
            id,
            job_id,
            created_at,
            job_state
        )
        values (@id, @job_id, @created_at, @job_state)
        """, new
        {
            id = jobStateId,
            job_id = jobInput.Id,
            created_at = now,
            job_state = jobState,
        });
        await using var command = new NpgsqlCommand($"""
        insert into {schema}.job (
            id,
            job_name,
            job_data,
            job_state,
            job_state_id,
            retry_max_count,
            retry_count,
            retry_delay_ms,
            start_after,
            created_on,
            queue,
            priority,
            throttle_key,
            throttle_seconds
        )
        values (@id, @name, @data, @state, @job_state_id, @retry_max_count, @retry_count, @retry_delay, @start_after, @created_on, @queue, @priority, @throttle_key, @throttle_seconds)
        """, conn);
        command.Parameters.AddWithValue("@id", jobInput.Id);
        command.Parameters.AddWithValue("@name", jobInput.JobName);
        command.Parameters.AddWithValue("@data", NpgsqlTypes.NpgsqlDbType.Jsonb, jobInput.JobData == null ? DBNull.Value : jobInput.JobData);
        command.Parameters.AddWithValue("@state", (short)jobInput.JobState);
        command.Parameters.AddWithValue("@job_state_id", jobStateId);
        command.Parameters.AddWithValue("@retry_max_count", jobInput.RetryMaxCount);
        command.Parameters.AddWithValue("@retry_count", jobInput.RetryCount);
        command.Parameters.AddWithValue("@retry_delay", jobInput.RetryDelayMs);
        command.Parameters.AddWithValue("@start_after", jobInput.StartAfter);
        command.Parameters.AddWithValue("@created_on", jobInput.CreatedOn);
        command.Parameters.AddWithValue("@queue", NpgsqlTypes.NpgsqlDbType.Text, jobInput.Queue!);
        command.Parameters.AddWithValue("@priority", jobInput.Priority);
        command.Parameters.AddWithValue("@server_name", jobInput.Priority);
        command.Parameters.AddWithValue("@throttle_key", !string.IsNullOrEmpty(jobInput.ThrottleKey) ? jobInput.ThrottleKey! : DBNull.Value);
        command.Parameters.AddWithValue("@throttle_seconds", jobInput.ThrottleSeconds);
        var rows = await command.ExecuteNonQueryAsync();
        if (rows <= 0)
        {
            throw new Exception($"Job not added {jobInput.Id} - {jobInput.JobName}");
        }
        if (configuration.ListenNotifyNewJobEnabled)
        {
            if (jobInput.StartAfter < DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(30)))
            {
                await conn.ExecuteAsync($"notify {channelName}, '{jobInput.Id}'");
            }
        }
        await tx.CommitAsync();
    }

    static Guid GenerateId()
    {
        return Guid.NewGuid();
    }
}
