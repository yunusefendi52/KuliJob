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
    public string? GetString(string key)
    {
        return ((JsonElement)this[key]).GetString();
    }

    public int GetInt(string key)
    {
        return ((JsonElement)this[key]).GetInt32();
    }

    public long GetLong(string key)
    {
        return ((JsonElement)this[key]).GetInt64();
    }

    public decimal GetDecimal(string key)
    {
        return ((JsonElement)this[key]).GetDecimal();
    }

    public double GetDouble(string key)
    {
        return ((JsonElement)this[key]).GetDouble();
    }

    public DateTimeOffset GetDateTimeOffset(string key)
    {
        return ((JsonElement)this[key]).GetDateTimeOffset();
    }

    public DateTime GetDateTime(string key)
    {
        return ((JsonElement)this[key]).GetDateTime();
    }

    public bool GetBool(string key)
    {
        return ((JsonElement)this[key]).GetBoolean();
    }

    public T? Get<T>(string key)
    {
        return ((JsonElement)this[key]).Deserialize<T>(Serializer.jsonSerializerOptions);
    }
}
