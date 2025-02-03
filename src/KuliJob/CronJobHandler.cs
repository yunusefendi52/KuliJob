
using KuliJob.Storages;

namespace KuliJob;

internal class CronJobHandler : IJob
{
    public async Task Execute(JobContext context)
    {
        var cron = context.JobData.GetValue<Cron>("cron")!;
        var expressionSerializer = context.Services.GetRequiredService<ExpressionSerializer>();
        await expressionSerializer.InvokeExpr(cron.Data);
    }
}
