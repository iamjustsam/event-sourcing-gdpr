using System.Security.Cryptography;
using EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Keystore;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public class EncryptorDecryptor : IEncryptorDecryptor
{
    private readonly ICryptoRepository _cryptoRepository;

    public EncryptorDecryptor(ICryptoRepository cryptoRepository)
    {
        _cryptoRepository = cryptoRepository;
    }

    public async Task<ICryptoTransform> GetEncryptorAsync(string dataSubjectId, CancellationToken cancellationToken)
    {
        var encryptionKey = await _cryptoRepository.GetExistingOrNewAsync(dataSubjectId, CreateNewEncryptionKey, cancellationToken);
        var aes = GetAes(encryptionKey);

        return aes.CreateEncryptor();
    }

    public async Task<ICryptoTransform?> GetDecryptorAsync(string dataSubjectId, CancellationToken cancellationToken)
    {
        var encryptionKey = await _cryptoRepository.GetExistingOrDefaultAsync(dataSubjectId, cancellationToken);
        if (encryptionKey is null)
            return default;

        var aes = GetAes(encryptionKey);

        return aes.CreateDecryptor();
    }

    private static EncryptionKey CreateNewEncryptionKey(string id)
    {
        var aes = Aes.Create();
        aes.Padding = PaddingMode.PKCS7;

        var key = aes.Key;
        var nonce = aes.IV;
        var encryptionKey = new EncryptionKey(id, key, nonce);

        return encryptionKey;
    }

    private static Aes GetAes(EncryptionKey encryptionKey)
    {
        var aes = Aes.Create();

        aes.Padding = PaddingMode.PKCS7;
        aes.Key = encryptionKey.Key;
        aes.IV = encryptionKey.Nonce;

        return aes;
    }
}
