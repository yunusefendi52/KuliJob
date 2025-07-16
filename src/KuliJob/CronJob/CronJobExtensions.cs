using System.Linq.Expressions;
using Cronos;

namespace KuliJob;

public static class CronJobExtensions
{
    public static void AddCron<T>(
        this JobConfiguration configuration,
        Expression<Func<T, Task>> expression,
        CronBuilder cronBuilder)
    {
        if (!CronExpression.TryParse(cronBuilder.CronExpression, CronFormat.Standard, out var _))
        {
            throw new ArgumentException($"Invalid cron expression '{cronBuilder.CronExpression}'");
        }

        configuration.CronBuilders.Add(async (cronJob) =>
        {
            await cronJob.AddOrUpdate(expression, cronBuilder.CronName, cronBuilder.CronExpression, new()
            {
                TimeZoneId = cronBuilder.TimeZoneId,
            });
        });
    }
}
