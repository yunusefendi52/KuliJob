namespace KuliJob.Tests;

public class BaseTest
{
    public static async Task WaitJobTicks(int count = 1)
    {
        await Task.Delay(600 * count);
    }
    
    public static async Task WaitCronTicks(int count = 1)
    {
        await Task.Delay(1050 * count);
    }
}
