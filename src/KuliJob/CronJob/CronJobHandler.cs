using System.Text.Json;
using KuliJob.Storages;

namespace KuliJob.CronJob;

internal class CronJobHandler : IJob
{
    public async Task Execute(JobContext context)
    {
        var cron = context.JobData.GetValue<Cron>("k_cron")!;
        var cronData = JsonSerializer.Deserialize<CronData>(cron.Data, Serializer.jsonSerializerOptions)!;
        var methodExpr = cronData.Expr;
        if (!string.IsNullOrWhiteSpace(methodExpr))
        {
            var expressionSerializer = context.Services.GetRequiredService<ExpressionSerializer>();
            var serviceProvider = context.Services.GetRequiredService<IServiceProvider>();
            await expressionSerializer.InvokeExpr(serviceProvider, methodExpr!);
        }
        else
        {
            throw new ArgumentException("Invalid cron job handler");
        }
    }
}
