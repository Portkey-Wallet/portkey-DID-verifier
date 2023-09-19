using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Common;

public class CryptographyHelper
{
    public static GenerateSignatureOutput GenerateSignature(int guardianType, string salt,
        string guardianIdentifierHash,
        string privateKey,
        string operationType, string merklePath)
    {
        //create signature
        var verifierSPublicKey =
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey)).PublicKey;
        var verifierAddress = Address.FromPublicKey(verifierSPublicKey);
        var data = "";
        if (operationType == "0" || string.IsNullOrWhiteSpace(operationType))
        {
            data = $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow},{verifierAddress.ToBase58()},{salt}";
        }
        else if (operationType is "8" or "9")
        {
            data =
                $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{verifierAddress.ToBase58()},{salt},{operationType},{merklePath}";
        }
        else
        {
            data =
                $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{verifierAddress.ToBase58()},{salt},{operationType}";
        }


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