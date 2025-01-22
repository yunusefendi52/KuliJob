namespace KuliJob.Tests;

public class BaseTest
{
    public static async Task WaitJobTicks(int count = 1)
    {
        await Task.Delay(600 * count);
    }
}
