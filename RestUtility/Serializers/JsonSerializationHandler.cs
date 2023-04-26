using System.Text;
using System.Text.Json;

namespace RestUtility.Serializers;

/// <summary>
/// JSON serialization handler
/// </summary>
public sealed class JsonSerializationHandler : ISerializer
{
    public string Serialize<T>(T obj)
        where T : class
    {
        return Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(obj));
    }

    public T? Deserialize<T>(string data) where T : class
    {
        return JsonSerializer.Deserialize<T>(data);
    }
}