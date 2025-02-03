using System.Runtime.CompilerServices;
using System.Text.Json;
using Dapper;
using KuliJob.Internals;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KuliJob.Postgres;

internal class PostgresJobStorage(
    PgDataSource dataSource,
    JobConfiguration configuration,
    MyClock myClock) : IJobStorage
{
    readonly string schema = dataSource.Schema;

    public async Task StartStorage(CancellationToken cancellationToken)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync($"""
        start transaction;
        create schema if not exists {schema};
        create table if not exists {schema}.job  (
            id uuid not null,
            name text not null,
            data jsonb,
            state smallint not null default(0),
            retry_max_count smallint not null default(0),
            retry_count smallint not null default(0),
            retry_delay smallint not null default(0),
            start_after timestamp with time zone not null,
            started_on timestamp with time zone,
            completed_on timestamp with time zone,
            cancelled_on timestamp with time zone,
            failed_on timestamp with time zone,
            failed_message text,
            created_on timestamp with time zone not null,
            queue text not null default 'default',
            priority smallint not null default(0),
            server_name text
        );
        create index if not exists job_name_idx on {schema}.job (name);
        create index if not exists job_name_id_idx on {schema}.job (name, id);
        create index if not exists job_created_on_id_idx on {schema}.job (created_on, id);
        create index if not exists job_name_state_start_after_idx on {schema}.job (name, state, start_after);
        create index if not exists job_name_state_start_after_queue_idx on {schema}.job (name, state, start_after, queue);
        alter table {schema}.job add if not exists priority smallint not null default(0);
        create index if not exists job_priority_created_on_id_idx on {schema}.job (priority, created_on, id);
        create index if not exists job_priority_created_on_id_queue_idx on {schema}.job (priority, created_on, id, queue);
        commit;
        """);
    }

    public async Task CancelJobById(string jobId)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set cancelled_on = @now,
            state = '{(int)JobState.Cancelled}'
        where id = @id::uuid
            and state < '{(int)JobState.Completed}'
        returning 1
        """, new
        {
            id = jobId,
            now = myClock.GetUtcNow(),
        });
    }

    public async Task CompleteJobById(string jobId)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set completed_on = @now,
            state = '{(int)JobState.Completed}'
        where id = @id::uuid
            and state = '{(int)JobState.Active}'
        returning 1
        """, new
        {
            id = jobId,
            now = myClock.GetUtcNow(),
        });
    }

    public async Task FailJobById(string jobId, string failedMessage)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set failed_on = @now,
            state = '{(int)JobState.Failed}',
            failed_message = @failedMessage
        where id = @id::uuid
            and state = '{(int)JobState.Active}'
        returning 1
        """, new
        {
            id = jobId,
            failedMessage,
            now = myClock.GetUtcNow(),
        });
    }

    public async Task<Job?> FetchNextJob(CancellationToken cancellationToken = default)
    {
        // TODO: Check queue query where here
        var queues = string.Join(',', configuration.Queues.Select(v => $"'{v}'"));
        if (string.IsNullOrEmpty(queues))
        {
            return null;
        }
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var nextJob = await conn.QuerySingleOrDefaultAsync<PostgresJobInput>($"""
        with locked_job as (
            select id from {schema}.job
            where state < '{(int)JobState.Active}'
                and start_after < @now
                and queue in ({queues})
            order by priority, created_on, id
            limit 1
            for update skip locked
        )
        update {schema}.job job
        set
            state = '{(int)JobState.Active}',
            started_on = @now,
            server_name = @serverName
        from locked_job
        where job.id = locked_job.id
        returning job.*
        """, new
        {
            now = myClock.GetUtcNow(),
            serverName = configuration.ServerName,
        });
        return nextJob?.ToJobInput();
    }

    public async Task<Job?> GetJobById(string jobId)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var result = await conn.QuerySingleOrDefaultAsync<PostgresJobInput>($"""
        select * from {schema}.job
        where id = @id::uuid
        """, new
        {
            id = jobId,
            now = myClock.GetUtcNow(),
        });
        var jobInput = result?.ToJobInput();
        return jobInput;
    }

    public async Task<IEnumerable<Job>> GetLatestJobs(int page, int limit, JobState? jobState = null)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var results = await conn.QueryAsync<PostgresJobInput>($"""
        select * from {schema}.job
        {(jobState is not null ? "where state = @jobState" : null)}
        order by started_on desc
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

    public async Task InsertJob(Job jobInput)
    {
        var commandText = $"""
        insert into {schema}.job (
            id,
            name,
            data,
            state,
            retry_max_count,
            retry_count,
            retry_delay,
            start_after,
            created_on,
            queue,
            priority
        )
        values (@id, @name, @data, @state, @retry_max_count, @retry_count, @retry_delay, @start_after, @created_on, @queue, @priority)
        """;
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(commandText, conn);
        command.Parameters.AddWithValue("@id", Guid.Parse(jobInput.Id));
        command.Parameters.AddWithValue("@name", jobInput.JobName);
        command.Parameters.AddWithValue("@data", NpgsqlTypes.NpgsqlDbType.Jsonb, jobInput.JobData == null ? DBNull.Value : jobInput.JobData);
        command.Parameters.AddWithValue("@state", (short)jobInput.JobState);
        command.Parameters.AddWithValue("@retry_max_count", jobInput.RetryMaxCount);
        command.Parameters.AddWithValue("@retry_count", jobInput.RetryCount);
        command.Parameters.AddWithValue("@retry_delay", jobInput.RetryDelayMs);
        command.Parameters.AddWithValue("@start_after", jobInput.StartAfter);
        command.Parameters.AddWithValue("@created_on", jobInput.CreatedOn);
        command.Parameters.AddWithValue("@queue", NpgsqlTypes.NpgsqlDbType.Text, jobInput.Queue!);
        command.Parameters.AddWithValue("@priority", jobInput.Priority);
        command.Parameters.AddWithValue("@server_name", jobInput.Priority);
        var rows = await command.ExecuteNonQueryAsync();
    }

    public async Task ResumeJob(string jobId)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set completed_on = null,
            state = '{(int)JobState.Created}'
        where id = @id::uuid
            and state = '{(int)JobState.Cancelled}'
        returning 1
        """, new
        {
            id = jobId,
        });
    }

    public async Task<Job> RetryJob(string jobId, int retryDelay)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var result = await conn.QuerySingleAsync<PostgresJobInput>($"""
        update {schema}.job
        set completed_on = null,
            state = '{(int)JobState.Retry}',
            start_after = start_after + ({retryDelay} * interval '1 ms'),
            retry_count = retry_count + 1
        where id = @id::uuid
        returning *
        """, new
        {
            id = jobId,
        });
        return result.ToJobInput();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
