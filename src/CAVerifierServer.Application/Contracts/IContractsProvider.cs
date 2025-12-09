using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
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
    public async Task<GetCAServersOutput> GetCaServersListAsync(ChainInfo chainInfo)
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
        var methodName = "GetCAServers";
        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainInfo.ContractAddress,
            methodName, param);
        var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        return GetCAServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }
}