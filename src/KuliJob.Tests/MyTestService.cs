using System.Globalization;

namespace KuliJob.Tests;

#pragma warning disable CS9113 // Parameter is unread.
public class MyTestService(HttpClient httpClient)
#pragma warning restore CS9113 // Parameter is unread.
{
    public async Task WriteCurrentDate(string tmp)
    {
        await File.WriteAllTextAsync(tmp, DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
    }

    public async Task IncrementTextFile(string tmp)
    {
        if (!File.Exists(tmp))
        {
            await File.WriteAllTextAsync(tmp, "1");
            return;
        }

        try
        {
            string content = await File.ReadAllTextAsync(tmp);
            if (int.TryParse(content, out int number))
            {
                number++;
                await File.WriteAllTextAsync(tmp, number.ToString());
            }
            else
            {
                throw new FormatException("File content is not a valid integer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file: {ex.Message}");
        }
    }
}
