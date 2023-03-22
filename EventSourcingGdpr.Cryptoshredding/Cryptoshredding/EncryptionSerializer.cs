using System.Buffers;
using System.Data.Common;
using Marten;
using Marten.Services.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Weasel.Core;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public class EncryptionSerializer : ISerializer
{
    private readonly IEncryptorDecryptor _encryptorDecryptor;
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Create();

    private readonly JsonArrayPool<char> _jsonArrayPool;

    public EncryptionSerializer(IEncryptorDecryptor encryptorDecryptor)
    {
        _encryptorDecryptor = encryptorDecryptor;
        _jsonArrayPool = new JsonArrayPool<char>(_charPool);
    }

    /// <summary>
    ///     Specify whether collections should be stored as json arrays (without type names)
    /// </summary>
    public CollectionStorage CollectionStorage { get; set; } = CollectionStorage.Default;

    /// <summary>
    ///     Specify whether non public members should be used during deserialization
    /// </summary>
    public NonPublicMembersStorage NonPublicMembersStorage { get; set; }

    public string ToJson(object? document)
    {
        var writer = new StringWriter();
        ToJsonAsync(document, writer, default).GetAwaiter().GetResult();

        return writer.ToString();
    }

    public T FromJson<T>(Stream stream)
    {
        // TODO: Decryption
        using var jsonReader = GetJsonTextReader(stream);

        var contractResolver = new JsonNetContractResolver();
        var serializer = CreateSerializer(contractResolver);

        return serializer.Deserialize<T>(jsonReader)!;
    }

    public T FromJson<T>(DbDataReader reader, int index)
    {
        // TODO: Decryption
        using var textReader = reader.GetTextReader(index);
        using var jsonReader = GetJsonTextReader(textReader);

        var contractResolver = new JsonNetContractResolver();
        var serializer = CreateSerializer(contractResolver);

        return serializer.Deserialize<T>(jsonReader)!;
    }

    public ValueTask<T> FromJsonAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return new ValueTask<T>(FromJson<T>(stream));
    }

    public ValueTask<T> FromJsonAsync<T>(DbDataReader reader, int index, CancellationToken cancellationToken = default)
    {
        return new ValueTask<T>(FromJson<T>(reader, index));
    }

    public object FromJson(Type type, Stream stream)
    {
        // TODO: Decryption
        using var jsonReader = GetJsonTextReader(stream);

        var contractResolver = new JsonNetContractResolver();
        var serializer = CreateSerializer(contractResolver);

        return serializer.Deserialize(jsonReader, type)!;
    }

    public object FromJson(Type type, DbDataReader reader, int index)
    {
        // TODO: Decryption
        using var textReader = reader.GetTextReader(index);
        using var jsonReader = GetJsonTextReader(textReader);

        var contractResolver = new JsonNetContractResolver();
        var serializer = CreateSerializer(contractResolver);

        return serializer.Deserialize(textReader, type)!;
    }

    public ValueTask<object> FromJsonAsync(Type type, Stream stream, CancellationToken cancellationToken = new CancellationToken())
    {
        return new ValueTask<object>(FromJson(type, stream));
    }

    public ValueTask<object> FromJsonAsync(Type type, DbDataReader reader, int index, CancellationToken cancellationToken = new CancellationToken())
    {
        return new ValueTask<object>(FromJson(type, reader, index));
    }

    public string ToCleanJson(object? document)
    {
        // TODO: Encryption
        var writer = new StringWriter();

        var contractResolver = new JsonNetContractResolver();
        var serializer = CreateCleanSerializer(contractResolver);
        
        serializer.Serialize(writer, document);

        return writer.ToString();
    }

    public string ToJsonWithTypes(object document)
    {
        // TODO: Encryption
        var writer = new StringWriter();

        var contractResolver = new JsonNetContractResolver();
        var serializer = CreateSerializerWithTypes(contractResolver);
        
        serializer.Serialize(writer, document);

        return writer.ToString();
    }

    /// <summary>
    ///     Specify whether .Net Enum values should be stored as integers or strings
    ///     within the Json document. Default is AsInteger.
    /// </summary>
    public EnumStorage EnumStorage { get; set; } = EnumStorage.AsInteger;

    /// <summary>
    ///     Specify whether properties in the JSON document should use Camel or Pascal casing.
    /// </summary>
    public Casing Casing { get; set; } = Casing.Default;

    public ValueCasting ValueCasting => ValueCasting.Relaxed;

    private async Task ToJsonAsync(object? document, TextWriter writer, CancellationToken cancellationToken)
    {
        // TODO: Encryption
        var dataSubjectId = GetDataSubjectId(document);
        var contractResolver = await GetContractResolverAsync(dataSubjectId, cancellationToken);
        var serializer = CreateSerializer(contractResolver);

        await using var jsonWriter = new JsonTextWriter(writer)
        {
            ArrayPool = _jsonArrayPool, CloseOutput = false, AutoCompleteOnClose = false
        };

        serializer.Serialize(jsonWriter, document);

        await writer.FlushAsync();
    }

    private JsonTextReader GetJsonTextReader(Stream stream)
    {
        var streamReader = new StreamReader(stream);

        var firstByte = streamReader.Peek();
        if (firstByte == 1)
        {
            streamReader.Read();
        }

        return new(streamReader) { ArrayPool = _jsonArrayPool, CloseInput = false };
    }

    private JsonTextReader GetJsonTextReader(TextReader textReader) => new(textReader) { ArrayPool = _jsonArrayPool, CloseInput = false };

    private static string? GetDataSubjectId(object? document)
    {
        if (document is null)
            return null;

        var eventType = document.GetType();
        var properties = eventType.GetProperties();
        var dataSubjectIdPropertyInfo = properties.FirstOrDefault(x => x.GetCustomAttributes(typeof(DataSubjectIdAttribute), false).Any(y => y is DataSubjectIdAttribute));

        if (dataSubjectIdPropertyInfo is null)
            return null;

        var value = dataSubjectIdPropertyInfo.GetValue(document);
        var dataSubjectId = value.ToString();

        return dataSubjectId;
    }

    private async Task<IContractResolver> GetContractResolverAsync(string dataSubjectId, CancellationToken cancellationToken)
    {
        var encryptor = await _encryptorDecryptor.GetEncryptorAsync(dataSubjectId, cancellationToken);
        var fieldEncryptionDecryption = new FieldEncryptionDecryption();

        return new EncryptionContractResolver(encryptor, fieldEncryptionDecryption, Casing, CollectionStorage, NonPublicMembersStorage);
    }

    private JsonSerializer CreateSerializer(IContractResolver contractResolver)
    {
        var serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.Auto,

            // ISO 8601 formatting of DateTime's is mandatory
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            ContractResolver = contractResolver
        };

        if (NonPublicMembersStorage.HasFlag(NonPublicMembersStorage.NonPublicDefaultConstructor))
            serializer.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;

        if (EnumStorage == EnumStorage.AsString)
        {
            var converter = new StringEnumConverter();
            serializer.Converters.Add(converter);
        }

        return serializer;
    }

    private JsonSerializer CreateCleanSerializer(IContractResolver contractResolver)
    {
        var serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = contractResolver
        };

        if (EnumStorage == EnumStorage.AsString)
        {
            var converter = new StringEnumConverter();
            serializer.Converters.Add(converter);
        }

        return serializer;
    }

    private JsonSerializer CreateSerializerWithTypes(IContractResolver contractResolver)
    {
        return new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.Objects,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = contractResolver
        };
    }
}
