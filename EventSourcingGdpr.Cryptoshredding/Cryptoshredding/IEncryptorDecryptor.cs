using System.Security.Cryptography;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public interface IEncryptorDecryptor
{
    Task<ICryptoTransform> GetEncryptorAsync(string dataSubjectId, CancellationToken cancellationToken);
    Task<ICryptoTransform?> GetDecryptorAsync(string dataSubjectId, CancellationToken cancellationToken);
}
