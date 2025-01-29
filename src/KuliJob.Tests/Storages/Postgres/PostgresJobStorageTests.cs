using System.Collections.Immutable;
using Dapper;
using KuliJob.Internals;
using KuliJob.Postgres;
using KuliJob.Storages;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace KuliJob.Tests.Storages.Postgres;

[Parallelizable]
public class PostgresJobStorageTests : BaseTest
{
    const string dbConnString = "Host=localhost;Username=postgres;Password=postgres;Include Error Detail=True";

    [Test]
    public async Task Can_Start_And_Migrate_PostgresJob()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(KeyedType.KuliJobDb);
        await using var conn = await dataSource.OpenConnectionAsync();
        await Assert.That(() => conn.QueryAsync("select * from kulijob.job")).IsEmpty();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
    }

    [Test]
    public async Task Can_Start_And_Migrate_PostgresJob_WithCustomSchema()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString, "myschemajob");
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(KeyedType.KuliJobDb);
        await using var conn = await dataSource.OpenConnectionAsync();
        await Assert.That(() => conn.QueryAsync("select * from kulijob.job")).IsNull();
        await Assert.That(() => conn.QueryAsync("select * from myschemajob.job")).IsEmpty();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
    }

    [Test]
    public async Task Can_Insert_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddLogging();
        services.AddSingleton(_ => config);
        var serializer = new Serializer();
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow.AddMilliseconds(-250);
        string jobId = "";
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        await Assert.That(() => jobStorage.GetJobById(jobId)).ThrowsNothing();
        await Assert.That(() => jobStorage.GetJobById(jobId)).IsNotNull();
        await Assert.That(() => jobStorage.GetJobById(jobId).ContinueWith(v => v.Result!.Id)).IsEqualTo(jobId);
        var jobData = serializer.Serialize(("Data1", 1.0));
        var jobWithData = await Assert.That(async () =>
        {
            var job = new Job()
            {
                JobName = "job",
                JobData = jobData,
                StartAfter = startAfter,
            };
            await jobStorage.InsertJob(job);
            return job;
        }).ThrowsNothing();
        await Assert.That(jobWithData).IsNotNull();
        await Assert.That(jobWithData!.JobData).IsEqualTo(jobData);
    }

    [Test]
    public async Task Can_Get_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddLogging();
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow;
        var insertJob = await Assert.That(async () =>
        {
            var job = new Job()
            {
                JobName = "job",
                StartAfter = startAfter,
            }; ;
            await jobStorage.InsertJob(job);
            return job;
        }).ThrowsNothing();
        var theJob = await Assert.That(() => jobStorage.GetJobById(insertJob!.Id)).ThrowsNothing();
        await Assert.That(insertJob!.Id).IsEqualTo(insertJob.Id);
        await Assert.That(insertJob!.CreatedOn).IsEqualTo(insertJob.CreatedOn);
        await Assert.That(insertJob.StartAfter).IsEqualTo(insertJob.StartAfter);
    }

    [Test]
    public async Task Should_Return_Null_When_No_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddLogging();
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        await Assert.That(() => jobStorage.GetJobById("2d212fa9-4b15-45fe-a65c-22de0b6b25ef")).IsNull();
    }

    [Test]
    public async Task Can_Cancel_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var serializer = new Serializer();
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow.AddMilliseconds(-250);
        var jobId = "";
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        await Assert.That(() => jobStorage.CancelJobById(jobId)).ThrowsNothing();
        var cancelledJob = await jobStorage.GetJobById(jobId);
        await Assert.That(cancelledJob!.JobState).IsEqualTo(JobState.Cancelled);
        await Assert.That(cancelledJob!.CancelledOn).IsNotNull();
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                JobState = JobState.Completed,
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        await Assert.That(() => jobStorage.CancelJobById(jobId)).ThrowsException();
    }

    [Test]
    public async Task Can_Complete_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var serializer = new Serializer();
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow.AddMilliseconds(-250);
        var jobId = "";
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                JobState = JobState.Active,
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        var completedJob = await jobStorage.GetJobById(jobId);
        await Assert.That(() => jobStorage.CompleteJobById(completedJob!.Id)).ThrowsNothing();
        completedJob = await jobStorage.GetJobById(jobId);
        await Assert.That(completedJob!.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(completedJob!.CompletedOn).IsNotNull();
        await Assert.That(() => jobStorage.CancelJobById(jobId)).ThrowsException();
    }

    [Test]
    public async Task Can_Fail_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var serializer = new Serializer();
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow.AddMilliseconds(-250);
        var jobId = "";
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                JobState = JobState.Active,
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        var theJob = await jobStorage.GetJobById(jobId);
        await Assert.That(() => jobStorage.FailJobById(theJob!.Id, "reason msg")).ThrowsNothing();
        theJob = await jobStorage.GetJobById(jobId);
        await Assert.That(theJob!.JobState).IsEqualTo(JobState.Failed);
        await Assert.That(theJob!.FailedMessage).IsEqualTo("reason msg");
        await Assert.That(() => jobStorage.FailJobById(theJob.Id, "reason msg")).ThrowsException();
    }

    [Test]
    public async Task Cancelled_Fetch_Job_Should_Throws()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var now1 = DateTimeOffset.UtcNow;
        await Assert.That(() => jobStorage.FetchNextJob(cts.Token)).Throws<TaskCanceledException>();
        var now2 = DateTimeOffset.UtcNow;
        await Assert.That(now1).IsBetween(now2.AddMilliseconds(-100), now2.AddMilliseconds(100));
    }

    [Test]
    public async Task Fetch_Job_Should_Not_Empty()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var worker = 10;
        var config = new JobConfiguration
        {
            ServiceCollection = services,
            MinPollingIntervalMs = 100,
            Worker = worker,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        await Parallel.ForEachAsync(Enumerable.Range(0, worker), async (v, c) =>
        {
            var job = new Job()
            {
                JobName = $"job {v}",
                StartAfter = DateTimeOffset.UtcNow.AddMilliseconds(-500),
            };
            await jobStorage.InsertJob(job);
        });
        var results = new List<Job>();
        for (int i = 0; i < worker; i++)
        {
            var item = await jobStorage.FetchNextJob();
            results.Add(item!);
        }
        var batch1 = results.Take(5);
        await Assert.That(batch1).IsNotEmpty();
        var minBatch1 = batch1.MinBy(v => v.StartedOn);
        var maxBatch1 = batch1.MaxBy(v => v.StartedOn);
        var batch1Delta = minBatch1!.StartedOn - maxBatch1!.StartedOn;
        await Assert.That(minBatch1.JobState).IsEqualTo(JobState.Active);
        await Assert.That(maxBatch1.JobState).IsEqualTo(JobState.Active);
        await Assert.That(batch1Delta!.Value.TotalMilliseconds).IsLessThan(50);

        var batch2 = results.Skip(5).Take(5);
        await Assert.That(batch2).IsNotEmpty();
        var minBatch2 = batch2.MinBy(v => v.StartedOn);
        var maxBatch2 = batch2.MaxBy(v => v.StartedOn);
        var batch2Delta = minBatch2!.StartedOn - maxBatch2!.StartedOn;
        await Assert.That(minBatch2!.JobState).IsEqualTo(JobState.Active);
        await Assert.That(maxBatch2!.JobState).IsEqualTo(JobState.Active);
        await Assert.That(batch2Delta!.Value.TotalMilliseconds).IsLessThan(50);

        var batchDelta = maxBatch2.StartedOn - minBatch1.StartedOn;
        await Assert.That(batchDelta!.Value.TotalMilliseconds).IsBetween(0, 50).WithInclusiveBounds();
    }

    // [Test]
    // public async Task Fetch_Should_Break_Inner_Loop_When_Empty()
    // {
    //     await using var postgresStart = new PostgresStart(dbConnString);
    //     var connString = await postgresStart.Start();
    //     var services = new ServiceCollection();
    //     services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
    //     var config = new JobConfiguration
    //     {
    //         ServiceCollection = services,
    //         MinPollingIntervalMs = 200,
    //     };
    //     config.UsePostgreSQL(connString);
    //     services.AddSingleton(_ => config);
    //     var sp = services.BuildServiceProvider();
    //     var jobStorage = sp.GetRequiredService<IJobStorage>();
    //     await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();

    //     var results = ImmutableList<Job>.Empty;
    //     var fetchTask = Task.Factory.StartNew(async () =>
    //     {
    //         await foreach (var item in jobStorage.FetchNextJob())
    //         {
    //             results = results.Add(item);
    //         }
    //     });

    //     await Assert.That(results).HasCount().EqualToZero();
    //     await Task.Delay(config.MinPollingIntervalMs);
    //     await Assert.That(results).HasCount().EqualToZero();

    //     await Parallel.ForEachAsync(Enumerable.Range(0, config.Worker), async (v, c) =>
    //     {
    //         var job = new Job()
    //         {
    //             JobName = $"job {v}",
    //             StartAfter = DateTimeOffset.UtcNow,
    //         };
    //         await jobStorage.InsertJob(job);
    //     });

    //     await Task.Delay(config.MinPollingIntervalMs + 50);
    //     await Assert.That(results).HasCount().EqualTo(config.Worker);
    // }

    // [Test]
    // public async Task Fetch_Should_Break_Inner_Loop_When_Queues_Empty()
    // {
    //     await using var postgresStart = new PostgresStart(dbConnString);
    //     var connString = await postgresStart.Start();
    //     var services = new ServiceCollection();
    //     services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
    //     var config = new JobConfiguration
    //     {
    //         ServiceCollection = services,
    //         MinPollingIntervalMs = 200,
    //         Queues = [],
    //     };
    //     config.UsePostgreSQL(connString);
    //     services.AddSingleton(_ => config);
    //     var sp = services.BuildServiceProvider();
    //     var jobStorage = sp.GetRequiredService<IJobStorage>();
    //     await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();

    //     var results = ImmutableList<Job>.Empty;
    //     var fetchTask = Task.Factory.StartNew(async () =>
    //     {
    //         await foreach (var item in jobStorage.FetchNextJob())
    //         {
    //             results = results.Add(item);
    //         }
    //     });

    //     await Assert.That(results).HasCount().EqualToZero();
    //     await Task.Delay(config.MinPollingIntervalMs + 100);
    //     await Assert.That(results).HasCount().EqualToZero();
    // }

    [Test]
    public async Task Can_Resume_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var serializer = new Serializer();
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow.AddMilliseconds(-250);
        var jobId = "";
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                JobState = JobState.Cancelled,
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        await Assert.That(() => jobStorage.ResumeJob(jobId)).ThrowsNothing();
        var theJob = await jobStorage.GetJobById(jobId);
        await Assert.That(theJob!.JobState).IsEqualTo(JobState.Created);
        await Assert.That(theJob!.CompletedOn).IsNull();
        await Assert.That(() => jobStorage.ResumeJob(jobId)).ThrowsException();
    }

    [Test]
    public async Task Can_Retry_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddSingleton(_ => config);
        var serializer = new Serializer();
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        var startAfter = DateTimeOffset.UtcNow;
        var jobId = "";
        await Assert.That(() =>
        {
            var job = new Job()
            {
                JobName = "job",
                JobState = JobState.Cancelled,
                StartAfter = startAfter,
            };
            jobId = job.Id;
            return jobStorage.InsertJob(job);
        }).ThrowsNothing();
        var retryTime = DateTimeOffset.UtcNow.AddMilliseconds(250);
        var retriedJob = await Assert.That(() => jobStorage.RetryJob(jobId, 250)).ThrowsNothing();
        var theJob = await jobStorage.GetJobById(jobId);
        await Assert.That(theJob!.Id).IsEqualTo(retriedJob!.Id);
        await Assert.That(theJob!.JobState).IsEqualTo(JobState.Retry);
        await Assert.That(theJob!.CompletedOn).IsNull();
        await Assert.That(theJob!.StartAfter).IsEqualTo(retriedJob.StartAfter);
        var deltaRetryTime = retryTime - retriedJob.StartAfter;
        await Assert.That(deltaRetryTime.TotalMilliseconds).IsLessThan(25);
        await Assert.That(() => jobStorage.RetryJob(jobId, 250)).ThrowsException();
    }

    [Test]
    public async Task Can_Fetch_Latest_Job()
    {
        await using var postgresStart = new PostgresStart(dbConnString);
        var connString = await postgresStart.Start();
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton("kulijob_timeprovider", TimeProvider.System);
        var config = new JobConfiguration
        {
            ServiceCollection = services,
        };
        config.UsePostgreSQL(connString);
        services.AddLogging();
        services.AddSingleton(_ => config);
        var sp = services.BuildServiceProvider();
        var jobStorage = sp.GetRequiredService<IJobStorage>();
        await Assert.That(() => jobStorage.StartStorage()).ThrowsNothing();
        await Assert.That(() => jobStorage.GetLatestJobs(1, 15)).IsEmpty();
        var startAfter = DateTimeOffset.UtcNow;
        await Assert.That(async () =>
        {
            await jobStorage.InsertJob(new Job()
            {
                JobName = "job",
                StartAfter = startAfter,
            });
            await jobStorage.InsertJob(new Job()
            {
                JobName = "job",
                JobState = JobState.Active,
                StartAfter = startAfter,
            });
        }).ThrowsNothing();
        await Assert.That(() => jobStorage.GetLatestJobs(1, 15)).HasCount().EqualTo(2);
        await Assert.That(() => jobStorage.GetLatestJobs(1, 15, JobState.Active)).HasCount().EqualTo(1);
    }
}

public static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken)
                                        .ConfigureAwait(false))
        {
            results.Add(item);
        }
        return results;
    }
}
