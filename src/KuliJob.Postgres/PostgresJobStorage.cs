using System.Runtime.CompilerServices;
using System.Text.Json;
using Dapper;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KuliJob.Postgres;

internal class PostgresJobStorage(
    [FromKeyedServices(KeyedType.Schema)] string schema,
    [FromKeyedServices(KeyedType.KuliJobDb)] NpgsqlDataSource dataSource,
    [FromKeyedServices("kulijob_timeprovider")] TimeProvider timeProvider,
    JobConfiguration configuration) : IJobStorage
{
    public async Task StartStorage()
    {
        await using var conn = await dataSource.OpenConnectionAsync();
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
            created_on timestamp with time zone not null
        );
        create index if not exists job_name_idx on {schema}.job (name);
        create index if not exists job_name_id_idx on {schema}.job (name, id);
        create index if not exists job_created_on_id_idx on {schema}.job (created_on, id);
        create index if not exists job_name_state_start_after_idx on {schema}.job (name, state, start_after);
        commit;
        """);
    }

    public async Task CancelJobById(string jobId)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set cancelled_on = now(),
            state = '{(int)JobState.Cancelled}'
        where id = @id::uuid
            and state < '{(int)JobState.Completed}'
        returning 1
        """, new
        {
            id = jobId,
        });
    }

    public async Task CompleteJobById(JobInput jobInput)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set completed_on = now(),
            state = '{(int)JobState.Completed}'
        where id = @id::uuid
            and state = '{(int)JobState.Active}'
        returning 1
        """, new
        {
            id = jobInput.Id,
        });
    }

    public async Task FailJobById(JobInput jobInput, string failedMessage)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await conn.QuerySingleAsync($"""
        update {schema}.job
        set failed_on = now(),
            state = '{(int)JobState.Failed}',
            failed_message = @failedMessage
        where id = @id::uuid
            and state = '{(int)JobState.Active}'
        returning 1
        """, new
        {
            id = jobInput.Id,
            failedMessage,
        });
    }

    public async IAsyncEnumerable<JobInput> FetchNextJob([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(configuration.MinPollingIntervalMs), timeProvider, cancellationToken);
            await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
            var results = await conn.QueryAsync<PostgresJobInput>($"""
                with locked_job as (
                    select id from {schema}.job
                    where state < '{(int)JobState.Active}'
                        and start_after < now()
                    order by created_on desc, id
                    limit @limit
                    for update skip locked
                )
                update {schema}.job job
                set
                    state = '{(int)JobState.Active}',
                    started_on = now()
                from locked_job
                where job.id = locked_job.id
                returning job.*
                """, new
            {
                limit = configuration.Worker,
            });
            if (results != null)
            {
                foreach (var item in results)
                {
                    var jobInput = item.ToJobInput();
                    yield return jobInput;
                }
            }
        }
    }

    public async Task<JobInput?> GetJobById(string jobId)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var result = await conn.QuerySingleOrDefaultAsync<PostgresJobInput>($"""
        select * from {schema}.job
        where id = @id::uuid
        """, new
        {
            id = jobId,
        });
        var jobInput = result?.ToJobInput();
        return jobInput;
    }

    public async Task<IEnumerable<JobInput>> GetLatestJobs(int page, int limit, JobState? jobState = null)
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

    public async Task InsertJob(JobInput jobInput)
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
            created_on
        )
        values (@id, @name, @data, @state, @retry_max_count, @retry_count, @retry_delay, @start_after, @created_on)
        """;
        await using var command = dataSource.CreateCommand(commandText);
        command.Parameters.AddWithValue("@id", Guid.Parse(jobInput.Id));
        command.Parameters.AddWithValue("@name", jobInput.JobName);
        command.Parameters.AddWithValue("@data", NpgsqlTypes.NpgsqlDbType.Jsonb, jobInput.JobData == null ? DBNull.Value : jobInput.JobData);
        command.Parameters.AddWithValue("@state", (short)jobInput.JobState);
        command.Parameters.AddWithValue("@retry_max_count", jobInput.RetryMaxCount);
        command.Parameters.AddWithValue("@retry_count", jobInput.RetryCount);
        command.Parameters.AddWithValue("@retry_delay", jobInput.RetryDelayMs);
        command.Parameters.AddWithValue("@start_after", jobInput.StartAfter);
        command.Parameters.AddWithValue("@created_on", jobInput.CreatedOn);
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

    public async Task<JobInput> RetryJob(string jobId, int retryDelay)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var result = await conn.QuerySingleAsync<PostgresJobInput>($"""
        update {schema}.job
        set completed_on = null,
            state = '{(int)JobState.Retry}',
            start_after = start_after + ({retryDelay} * interval '1 ms')
        where id = @id::uuid
            and state = '{(int)JobState.Cancelled}'
        returning *
        """, new
        {
            id = jobId,
        });
        return result.ToJobInput();
    }
}

internal class PostgresJobInput
{
    public Guid id { get; set; }
    public string name { get; set; } = null!;
    public string data { get; set; } = null!;
    public JobState state { get; set; }
    public DateTimeOffset start_after { get; set; }
    public DateTimeOffset? started_on { get; set; }
    public DateTimeOffset? completed_on { get; set; }
    public DateTimeOffset? cancelled_on { get; set; }
    public DateTimeOffset? failed_on { get; set; }
    public string? failed_message { get; set; }
    public DateTimeOffset created_on { get; set; }
    public int retry_max_count { get; set; }
    public int retry_count { get; set; }
    public int retry_delay { get; set; }

    public JobInput ToJobInput()
    {
        return new()
        {
            Id = id.ToString(),
            JobName = name,
            JobState = state,
            JobData = data,
            CancelledOn = cancelled_on,
            CompletedOn = completed_on,
            CreatedOn = created_on,
            FailedMessage = failed_message,
            FailedOn = failed_on,
            RetryCount = retry_count,
            RetryDelayMs = retry_delay,
            RetryMaxCount = retry_max_count,
            StartAfter = start_after,
            StartedOn = started_on,
        };
    }
}
