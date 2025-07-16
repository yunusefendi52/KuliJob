using System.Linq.Expressions;

namespace KuliJob;

internal interface ICronJob
{
    // Task AddOrUpdate<T>(string cronName, string cronExpression, CronOption? cronOption = null) where T : class, IJob;
    Task AddOrUpdate<T>(Expression<Func<T, Task>> expression, string cronName, string cronExpression, CronOption? cronOption = null);
}

public class CronOption
{
    public string? TimeZoneId { get; set; }
}