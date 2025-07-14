using KuliJob.Storages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace KuliJob.Dashboard;

internal static class DashboardApi
{
    internal static void AddApi(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/kulijob/api/kulijob");
        apiGroup.MapGet("/jobs", async (
            [FromServices] IJobStorage jobStorage,
            [FromQuery] int page,
            [FromQuery] JobState? jobState = null) =>
        {
            var jobs = await jobStorage.GetLatestJobs(page, 25, jobState);
            return new
            {
                Data = jobs,
            };
        });
        apiGroup.MapGet("/job", async (
            [FromServices] IJobStorage jobStorage,
            [FromQuery] Guid jobId) =>
        {
            var (job, jobStates) = await WhenAll(jobStorage.GetJobById(jobId), jobStorage.GetJobStates(jobId));
            return new
            {
                Data = job,
                JobStates = jobStates,
            };
        });
        apiGroup.MapGet("/cron", async ([FromServices] IJobStorage jobStorage) =>
        {
            var crons = (await jobStorage.GetCrons()).ToList();
            return new
            {
                Data = crons,
            };
        });
        apiGroup.MapGet("/servers", async ([FromServices] IJobStorage jobStorage) =>
        {
            var jobServers = await jobStorage.GetJobServers();
            return new
            {
                Data = jobServers,
            };
        });
    }

    static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> task1, Task<T2> task2)
    {
        await Task.WhenAll(task1, task2);
        var result1 = await task1;
        var result2 = await task2;
        return (result1, result2);
    }
}