using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Common;

public class CryptographyHelper
{
    public static GenerateSignatureOutput GenerateSignature(int guardianType, string salt,
        string guardianIdentifierHash, string privateKey, string operationType, string chainId, string operationDetails)
    {
        //create signature
        var verifierSPublicKey =
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey)).PublicKey;
        var verifierAddress = Address.FromPublicKey(verifierSPublicKey);
        var data = string.IsNullOrWhiteSpace(chainId)
            ? $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{verifierAddress.ToBase58()},{salt},{operationType}"
            : $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{verifierAddress.ToBase58()},{salt},{operationType},{chainId}";

        data = operationDetails.IsNullOrWhiteSpace()
            ? data
            : $"{data},{HashHelper.ComputeFrom(operationDetails).ToHex()}";
        var hashByteArray = HashHelper.ComputeFrom(data).ToByteArray();
        var signature =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), hashByteArray);
        return new GenerateSignatureOutput
        {
            Data = data,
            Signature = signature.ToHex()
        };
    }
}