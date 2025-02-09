using System.Linq.Expressions;

namespace KuliJob;

public interface ICronJob
{
    Task AddOrUpdate<T>(Expression<Func<T, Task>> expression, string cronName, string cronExpression, CronOption? cronOption = null);
}

public class CronOption
{
    public string? TimeZoneId { get; set; }
}