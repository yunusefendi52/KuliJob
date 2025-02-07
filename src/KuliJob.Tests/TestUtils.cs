namespace KuliJob.Tests;

public static class TestUtils
{
    public static string GetTempFile()
    {
        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
    }
}