using System.ComponentModel;
using System.Security.Cryptography;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public class FieldEncryptionDecryption
{
    public object? GetEncryptedOrDefault(object? value, ICryptoTransform? encryptor)
    {
        if (encryptor is null)
            throw new ArgumentNullException(nameof(encryptor));

        if (value is null)
            return default;

        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using var writer = new StreamWriter(cryptoStream);

        var valueAsText = value.ToString();
        writer.Write(valueAsText);
        writer.Flush();
        cryptoStream.FlushFinalBlock();

        var encryptedData = memoryStream.ToArray();
        var encryptedText = Convert.ToBase64String(encryptedData);

        return encryptedText;
    }

    public object? GetDecryptedOrDefault(object? value, ICryptoTransform? decryptor, Type destinationType)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value is not string valueAsString)
            return value;

        if (decryptor is null)
            return GetMaskedValue(destinationType);

        var encryptedValue = Convert.FromBase64String(valueAsString);

        using var memoryStream = new MemoryStream(encryptedValue);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);

        var decryptedText = reader.ReadToEnd();

        return Parse(destinationType, decryptedText);
    }

    private static object? Parse(Type outputType, string value)
    {
        var converter = TypeDescriptor.GetConverter(outputType);

        return converter.ConvertFromString(value);
    }

    private static object? GetMaskedValue(Type destinationType) => destinationType == typeof(string) ? "***" : Activator.CreateInstance(destinationType);
}
