using System.Linq.Expressions;
using KuliJob.Storages;

namespace KuliJob.CronJob;

internal class CronJobImpl(
    IJobStorage jobStorage,
    ExpressionSerializer expressionSerializer,
    Serializer serializer) : ICronJob
{
    // public async Task AddOrUpdate<T>(string cronName, string cronExpression, CronOption? cronOption = null) where T : class, IJob
    // {
    //     var data = serializer.Serialize(new
    //     {
    //         JobType = typeof(T).FullName,
    //     });
    //     var timezone = cronOption?.TimeZoneId;
    //     await jobStorage.AddOrUpdateCron(new()
    //     {
    //         Name = cronName,
    //         Data = data,
    //         CronExpression = cronExpression,
    //         TimeZone = timezone,
    //     });
    // }

    public async Task AddOrUpdate<T>(Expression<Func<T, Task>> expression, string cronName, string cronExpression, CronOption? cronOption = null)
    {
        var expr = expressionSerializer.FromExpr(expression);
        var data = serializer.Serialize(new CronData
        {
            Expr = expr,
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
