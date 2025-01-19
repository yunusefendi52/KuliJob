using System.Text.Json;

namespace KuliJob.Internals;

public class Serializer
{
    readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    internal string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions);
    }

    internal T? Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, jsonSerializerOptions);
    }
}
