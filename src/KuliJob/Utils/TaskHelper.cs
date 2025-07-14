namespace KuliJob.Utils;

internal static class TaskHelper
{
    public static async Task<T> RaceAsync<T>(
        Func<CancellationToken, Task<T>> mainTask,
        Func<CancellationToken, Task<T>> fallbackTask)
    {
        using var cts = new CancellationTokenSource();
        try
        {
            cts.Token.ThrowIfCancellationRequested();
            var mt = mainTask(cts.Token);
            var ft = fallbackTask(cts.Token);
            var completedTask = await Task.WhenAny(mt, ft);

            if (completedTask == mt)
            {
                return await mt;
            }
            else
            {
                return await ft;
            }
        }
        finally
        {
            cts.Cancel();
        }
    }
}
