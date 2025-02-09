namespace KuliJob.Tests;

public class MyTestService(HttpClient httpClient)
{
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
