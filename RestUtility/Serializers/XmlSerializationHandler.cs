using System.Xml;
using System.Xml.Serialization;

namespace RestUtility.Serializers;

/// <summary>
/// XML serialization handler
/// </summary>
public sealed class XmlSerializationHandler : ISerializer
{
    public string Serialize<T>(T obj) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        var xmlns = new XmlSerializerNamespaces(new [] { XmlQualifiedName.Empty });
        var settings = new XmlWriterSettings
        {
            Indent = false,
            OmitXmlDeclaration = true,
        };

        using var stringWriter = new StringWriter();
        using (var writer = XmlWriter.Create(stringWriter, settings))
            serializer.Serialize(writer, obj, xmlns);
        return stringWriter.ToString();
    }

    public T? Deserialize<T>(string data) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            CheckCharacters = false,
            ConformanceLevel = ConformanceLevel.Auto,
        };

        using var stringReader = new StringReader(data);
        using var reader = XmlReader.Create(stringReader, settings);
        return (T?)serializer.Deserialize(reader);
    }
}