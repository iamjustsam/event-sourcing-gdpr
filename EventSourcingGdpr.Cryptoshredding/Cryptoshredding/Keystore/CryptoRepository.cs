namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Keystore;

public class CryptoRepository : ICryptoRepository
{
    private readonly IKeyStore _keyStore;

    public CryptoRepository(IKeyStore keyStore)
    {
        _keyStore = keyStore;
    }

    public async Task<EncryptionKey> GetExistingOrNewAsync(string id, Func<string, EncryptionKey> keyGenerator, CancellationToken cancellationToken)
    {
        await using var session = _keyStore.LightweightSession();
        var existingKey = await session.LoadAsync<EncryptionKey>(id, cancellationToken);
        if (existingKey is not null)
            return existingKey;

        var newEncryptionKey = keyGenerator.Invoke(id);
        session.Store(newEncryptionKey);
        await session.SaveChangesAsync(cancellationToken);

        return newEncryptionKey;
    }

    public async Task<EncryptionKey?> GetExistingOrDefaultAsync(string id, CancellationToken cancellationToken)
    {
        await using var session = _keyStore.LightweightSession();
        return await session.LoadAsync<EncryptionKey>(id, cancellationToken);
    }

    public async Task DeleteEncryptionKey(string id, CancellationToken cancellationToken)
    {
        await using var session = _keyStore.LightweightSession();
        session.Delete<EncryptionKey>(id);

        await session.SaveChangesAsync(cancellationToken);
    }
}
