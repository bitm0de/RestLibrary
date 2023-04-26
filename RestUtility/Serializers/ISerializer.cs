namespace RestUtility.Serializers;

/// <summary>
/// Serialization interface for REST HTTP client.
/// </summary>
public interface ISerializer
{
    string Serialize<T>(T obj)
        where T : class;

    T? Deserialize<T>(string data)
        where T : class;
}