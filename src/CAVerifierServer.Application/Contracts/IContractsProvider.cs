using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.Types;
using CAVerifierServer.Application;
using CAVerifierServer.Options;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.Contracts;

public interface IContractsProvider
{
    public Task<GetCAServersOutput> GetCaServersListAsync(ChainInfo chainInfo);
}

public class ContractsProvider : IContractsProvider, ISingletonDependency
{
    private static readonly string AElfClientDefaultAddress = Address.FromPublicKey(CryptoHelper
        .FromPrivateKey(ByteArrayHelper.HexStringToByteArray(AElfClientConstants.DefaultPrivateKey))
        .PublicKey).ToBase58();

    public async Task<GetCAServersOutput> GetCaServersListAsync(ChainInfo chainInfo)
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var methodName = "GetCAServers";
        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(
            AElfClientDefaultAddress,
            chainInfo.ContractAddress,
            methodName, param);
        var txWithSign = client.SignTransaction(AElfClientConstants.DefaultPrivateKey, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        return GetCAServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }
}