using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KuliJob.Internals;

public class Serializer
{
    internal static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions);
    }

    internal T? Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, jsonSerializerOptions);
    }
}
