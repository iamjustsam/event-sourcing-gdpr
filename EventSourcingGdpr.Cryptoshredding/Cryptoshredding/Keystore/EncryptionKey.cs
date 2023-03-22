namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Keystore;

public class EncryptionKey
{
    public string Id { get; set; }
    public byte[] Key { get; }
    public byte[] Nonce { get; }

    public EncryptionKey(string id, byte[] key, byte[] nonce)
    {
        Id = id;
        Key = key;
        Nonce = nonce;
    }
}
