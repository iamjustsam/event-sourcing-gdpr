namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public interface ICryptoRepository
{
    Task<EncryptionKey> GetExistingOrNewAsync(string id, Func<string, EncryptionKey> keyGenerator, CancellationToken cancellationToken);
    Task<EncryptionKey?> GetExistingOrDefaultAsync(string id, CancellationToken cancellationToken);
    Task DeleteEncryptionKey(string id);
}
