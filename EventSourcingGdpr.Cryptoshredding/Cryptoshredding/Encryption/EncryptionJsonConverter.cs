using System.Security.Cryptography;
using Newtonsoft.Json;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Encryption;

public class EncryptionJsonConverter : JsonConverter
{
    private readonly ICryptoTransform _encryptor;
    private readonly FieldEncryptionDecryption _fieldEncryptionDecryption;

    public EncryptionJsonConverter(ICryptoTransform encryptor, FieldEncryptionDecryption fieldEncryptionDecryption)
    {
        _encryptor = encryptor;
        _fieldEncryptionDecryption = fieldEncryptionDecryption;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var result = _fieldEncryptionDecryption.GetEncryptedOrDefault(value, _encryptor);
        writer.WriteValue(result);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new InvalidOperationException();
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override bool CanRead => false;

    public override bool CanWrite => true;
}
