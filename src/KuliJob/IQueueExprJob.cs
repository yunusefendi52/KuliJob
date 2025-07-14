using System.Linq.Expressions;

namespace KuliJob;

public interface IQueueExprJob
{
    Task<Guid> Enqueue(Expression<Action> expression, DateTimeOffset startAfter, QueueOptions? queueOptions = null);
    Task<Guid> Enqueue(Expression<Action> expression, QueueOptions? queueOptions = null)
    {
        return Enqueue(expression, DateTimeOffset.UtcNow, queueOptions);
    }
    Task<Guid> Enqueue(Expression<Func<Task>> expression, DateTimeOffset startAfter, QueueOptions? queueOptions = null);
    Task<Guid> Enqueue(Expression<Func<Task>> expression, QueueOptions? queueOptions = null)
    {
        return Enqueue(expression, DateTimeOffset.UtcNow, queueOptions);
    }
    Task<Guid> Enqueue<T>(Expression<Func<T, Task>> expression, DateTimeOffset startAfter, QueueOptions? queueOptions = null);
    Task<Guid> Enqueue<T>(Expression<Func<T, Task>> expression, QueueOptions? queueOptions = null)
    {
        return Enqueue(expression, DateTimeOffset.UtcNow, queueOptions);
    }
}
