using System.Linq.Expressions;
using KuliJob.Storages;

namespace KuliJob;

internal class CronJob(
    IJobStorage jobStorage,
    ExpressionSerializer expressionSerializer,
    Serializer serializer) : ICronJob
{
    public async Task AddOrUpdate<T>(Expression<Func<T, Task>> expression, string cronName, string cronExpression, CronOption? cronOption = null)
    {
        var expr = expressionSerializer.FromExpr(expression);
        var data = serializer.Serialize(new
        {
            expr,
        });
        var timezone = cronOption?.TimeZoneId;
        await jobStorage.AddOrUpdateCron(new()
        {
            Name = cronName,
            Data = data,
            CronExpression = cronExpression,
            TimeZone = timezone,
        });
    }
}
