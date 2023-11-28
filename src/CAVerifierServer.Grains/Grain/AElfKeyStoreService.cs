using AElf;
using AElf.Cryptography;
using AElf.Cryptography.Exceptions;
using AElf.Types;
using CAVerifierServer.Grains.Options;
using Nethereum.KeyStore;

namespace CAVerifierServer.Grains.Grain;

public class AElfKeyStoreService : KeyStoreService
{
    public string EncryptKeyStoreAsJson(string password, string privateKey)
    {
        var keyPair = CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey));
        if (keyPair?.PrivateKey == null || keyPair.PublicKey == null)
            throw new InvalidKeyPairException("Invalid keypair (null reference).", null);

        var address = Address.FromPublicKey(keyPair.PublicKey);
        return EncryptAndGenerateDefaultKeyStoreAsJson(password, keyPair.PrivateKey, address.ToBase58());
    }

    public byte[] DecryptKeyStoreFromFile(VerifierAccountOptions verifierAccountOptions)
    {
        using var textReader = File.OpenText(verifierAccountOptions.KeyStorePath);
        var keyStoreContent = textReader.ReadToEnd();
        return DecryptKeyStoreFromJson(verifierAccountOptions.KeyStorePassword, keyStoreContent);
    }

}