
using System.Text.Json;
using KuliJob.Storages;

namespace KuliJob;

internal class CronJobHandler : IJob
{
    public async Task Execute(JobContext context)
    {
        var cron = context.JobData.GetValue<Cron>("cron")!;
        var cronData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cron.Data)!;
        var methodExpr = cronData["expr"].GetString();
        var expressionSerializer = context.Services.GetRequiredService<ExpressionSerializer>();
        var serviceProvider = context.Services.GetRequiredService<IServiceProvider>();
        await expressionSerializer.InvokeExpr(serviceProvider, methodExpr!);
    }
}
