using System.Security.Cryptography;
using Newtonsoft.Json;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public class DecryptionJsonConverter : JsonConverter
{
    private readonly ICryptoTransform _decryptor;
    private readonly FieldEncryptionDecryption _fieldEncryptionDecryption;

    public DecryptionJsonConverter(ICryptoTransform decryptor, FieldEncryptionDecryption fieldEncryptionDecryption)
    {
        _decryptor = decryptor;
        _fieldEncryptionDecryption = fieldEncryptionDecryption;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new InvalidOperationException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return _fieldEncryptionDecryption.GetDecryptedOrDefault(reader.Value, _decryptor, objectType);
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override bool CanRead => true;

    public override bool CanWrite => false;
}
