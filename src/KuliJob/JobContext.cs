using System.Text.Json;

namespace KuliJob;

public class JobContext
{
    public required IServiceProvider Services { get; init; }
    public required string JobName { get; set; } = null!;
    public required JobDataMap JobData { get; set; } = null!;
    public required int RetryCount { get; set; }
}

public class JobDataMap : Dictionary<string, object>
{
    public bool TryGetValue<T>(string key, out T? value)
    {
        if (TryGetValue(key, out var @object))
        {
            var jsonEl = (JsonElement)@object;
            value = jsonEl.Deserialize<T>(Serializer.jsonSerializerOptions);
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public T? GetValue<T>(string key)
    {
        return TryGetValue<T>(key, out var value) ? value : default;
    }
}
