using System.Linq.Expressions;
using KuliJob.Storages;
using NodaTime;

namespace KuliJob;

internal class CronJob(
    IJobStorage jobStorage,
    ExpressionSerializer expressionSerializer) : ICronJob
{
    public async Task AddOrUpdate<T>(Expression<Func<T, Task>> expression, string cronName, string cronExpression, CronOption? cronOption = null)
    {
        var expr = expressionSerializer.FromExpr(expression);
        var timezone = cronOption?.Timezone;
        await jobStorage.AddOrUpdateCron(new()
        {
            Name = cronName,
            Data = expr,
            CronExpression = cronExpression,
            Timezone = timezone,
        });
    }
}
