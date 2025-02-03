using System.Linq.Expressions;

namespace KuliJob;

public interface ICronJob
{
    Task AddOrUpdate<T>(Expression<Func<T, Task>> expression, string cronName, string cronExpression, string? timezone = null);
}
