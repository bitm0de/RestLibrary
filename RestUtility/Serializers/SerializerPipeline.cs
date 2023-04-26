using System.Collections;

namespace RestUtility.Serializers;

/// <inheritdoc />
/// <summary>
/// Serialization pipeline for automatic data handling
/// </summary>
public sealed class SerializerPipeline : IEnumerable<KeyValuePair<string, ISerializer>>
{
    public Dictionary<string, ISerializer> Serializers { get; }

    public SerializerPipeline(params KeyValuePair<string, ISerializer>[] serializers)
        : this((IEnumerable<KeyValuePair<string, ISerializer>>)serializers)
    { }

    public SerializerPipeline(IEnumerable<KeyValuePair<string, ISerializer>> serializers)
    {
        if (serializers is null)
            throw new ArgumentNullException(nameof(serializers));

        var d = new Dictionary<string, ISerializer>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in serializers)
            d.Add(kv.Key, kv.Value);

        Serializers = d;
    }

    public IEnumerator<KeyValuePair<string, ISerializer>> GetEnumerator()
    {
        return Serializers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}