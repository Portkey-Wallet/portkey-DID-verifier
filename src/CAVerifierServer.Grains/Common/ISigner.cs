using AElf.Cryptography;
using AElf.KeyStore;
using AElf.Types;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.Grains.Common;

public interface ISigner : IDisposable
{
    byte[] Sign(Hash dataHash);
    Address GetAddress();
}

public class KeyStoreSigner : ISigner
{
    private byte[] _privateKey;
    private readonly Address _address;
    private readonly AElfKeyStoreService _aelfKeyStoreService = new();
    private bool _disposed;

    public KeyStoreSigner(IOptions<VerifierAccountOptions> verifierAccountOptions)
    {
        _privateKey = _aelfKeyStoreService.DecryptKeyStoreFromFile(
            verifierAccountOptions.Value.KeyStorePassword,
            verifierAccountOptions.Value.KeyStorePath);
        var publicKey = CryptoHelper.FromPrivateKey(_privateKey).PublicKey;
        _address = Address.FromPublicKey(publicKey);
    }

    public byte[] Sign(Hash dataHash)
    {
        return CryptoHelper.SignWithPrivateKey(_privateKey, dataHash.ToByteArray());
    }

    public Address GetAddress()
    {
        return _address;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        Array.Clear(_privateKey, 0, _privateKey.Length);

        _disposed = true;
        GC.SuppressFinalize(this);
    }

}