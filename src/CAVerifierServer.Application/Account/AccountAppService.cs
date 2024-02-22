using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using CAVerifierServer.Application;
using CAVerifierServer.Contracts;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Infrastructure;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace CAVerifierServer.Account;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AccountAppService : CAVerifierServerAppService, IAccountAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ChainOptions _chainOptions;
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSenders;
    private readonly WhiteListExpireTimeOptions _whiteListExpireTimeOptions;
    private readonly IDistributedCache<DidServerList> _distributedCache;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AccountAppService> _logger;
    private readonly IContractsProvider _contractsProvider;
    private readonly IVerifiedRequestTimestampCacheProvider _timestampCacheProvider;

    private const string CaServerListKey = "CAServerListKey";

    public AccountAppService(IClusterClient clusterClient,
        IOptions<ChainOptions> chainOptions,
        IDistributedCache<DidServerList> distributedCache,
        IEnumerable<IVerifyCodeSender> verifyCodeSenders, IObjectMapper objectMapper,
        IOptions<WhiteListExpireTimeOptions> whiteListExpireTimeOption, ILogger<AccountAppService> logger,
        IContractsProvider contractsProvider, IVerifiedRequestTimestampCacheProvider timestampCacheProvider)
    {
        _clusterClient = clusterClient;
        _distributedCache = distributedCache;
        _verifyCodeSenders = verifyCodeSenders;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractsProvider = contractsProvider;
        _timestampCacheProvider = timestampCacheProvider;
        _whiteListExpireTimeOptions = whiteListExpireTimeOption.Value;
        _chainOptions = chainOptions.Value;
    }

    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(
        SendVerificationRequestInput input)
    {
        var verificationRequest = new VerificationRequest();
        verificationRequest.MergeFrom(ByteArrayHelper.HexStringToByteArray(input.VerificationRequest));
        var hash = HashHelper.ComputeFrom(verificationRequest);

        // Validate timestamp
        if (_timestampCacheProvider.IsVerificationRequestExpiredOrHandledBefore(hash, verificationRequest.Timestamp))
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.RequestExpiredOrHandledBefore]
            };
        }
        
        // Validate signature
        CryptoHelper.RecoverPublicKey(ByteArrayHelper.HexStringToByteArray(input.Signature), hash.ToByteArray(), out var pubkey);
        if (!await ValidatePubkeyAsync(pubkey.ToHex()))
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.CAServerNotExist]
            };
        }

        var verifyCodeSender = _verifyCodeSenders.FirstOrDefault(v => v.Type == verificationRequest.Type);
        if (verifyCodeSender == null)
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.Unsupported]
            };
        }

        if (!verifyCodeSender.ValidateGuardianIdentifier(verificationRequest.GuardianIdentifier))
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.InvalidGuardianIdentifierInput]
            };
        }

        try
        {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(verificationRequest.GuardianIdentifier);
            var dto = await grain.GetVerifyCodeAsync(verificationRequest);
            if (!dto.Success)
            {
                return new ResponseResultDto<SendVerificationRequestDto>
                {
                    Success = false,
                    Message = dto.Message
                };
            }

            await verifyCodeSender.SendCodeByGuardianIdentifierAsync(verificationRequest.GuardianIdentifier, dto.Data.VerifierCode);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = true,
                Data = new SendVerificationRequestDto
                {
                    VerifierSessionId = Guid.Parse(verificationRequest.VerifierSessionId)
                }
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.SendVerificationRequestErrorLogPrefix + e.Message);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.SendVerificationRequestErrorLogPrefix + e.Message
            };
        }
    }


    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
    {
        if (input.VerifierSessionId == Guid.Empty ||
            input.Code.IsNullOrEmpty() ||
            input.GuardianIdentifier.IsNullOrEmpty() ||
            input.Salt.IsNullOrEmpty() ||
            input.GuardianIdentifierHash.IsNullOrEmpty()
           )
        {
            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = Error.Message[Error.NullOrEmptyInput]
            };
        }

        try
        {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.GuardianIdentifier);
            var resultDto = await grain.VerifyAndCreateSignatureAsync(input);
            if (resultDto.Success)
            {
                return new ResponseResultDto<VerifierCodeDto>
                {
                    Success = true,
                    Data = new VerifierCodeDto
                    {
                        VerificationDoc = resultDto.Data.Data,
                        Signature = resultDto.Data.Signature
                    }
                };
            }

            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = resultDto.Message
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            };
        }
    }
    
    private async Task<bool> ValidatePubkeyAsync(string pubkey)
    {
        var didServerList = await GetDidServerListAsync();
        if (didServerList == null)
        {
            throw new UserFriendlyException("No CAServer is Found");
        }

        var servers = didServerList.DidServers.Distinct().ToList();
        return servers.Select(s => s.Pubkey).Contains(pubkey);
    }

    public async Task<string> WhiteListCheckAsync(List<string> ipList)
    {
        var didServerList = await GetDidServerListAsync();
        if (didServerList == null)
        {
            throw new UserFriendlyException("No CAServer is Found");
        }

        var servers = didServerList.DidServers.Distinct().ToList();
        var endPoints = new List<string>();
        servers.ForEach(t => { endPoints.Add(t.EndPoint); });
        _logger.LogDebug("CaServerIPList id {ipList} :", string.Join(",", endPoints));
        var caIpList = endPoints.Select(ip => ip.Split("//")[1]).Select(formatter => formatter.Split(":")[0]).ToList();
        _logger.LogDebug("Formatter ipList is {caIpList}", string.Join(",", caIpList));
        var result = ipList.Intersect(caIpList).ToList();
        return result.Count == 0 ? null : result[0];
        
        // var servers = didServerList.DidServers.Distinct().ToList();
        // var pubkeys = new List<string>();
        // servers.ForEach(t => { pubkeys.Add(t.Pubkey); });
        // _logger.LogDebug("CaServerPubkeyList id {pubkeyList} :", string.Join(",", pubkeys));
        // var caPubkeyList = pubkeys.Select(pubkey => pubkey.Split("//")[1]).Select(formatter => formatter.Split(":")[0]).ToList();
        // _logger.LogDebug("Formatter pubkeyList is {caPubkeyList}", string.Join(",", caPubkeyList));
        // var result = pubkeyList.Intersect(caPubkeyList).ToList();
        // return result.Count == 0 ? null : result[0];
    }

    public async Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(tokenRequestDto.AccessToken);
            var resultDto =
                await grain.VerifyGoogleTokenAsync(
                    ObjectMapper.Map<VerifyTokenRequestDto, VerifyTokenGrainDto>(tokenRequestDto));

            if (!resultDto.Success)
            {
                return new ResponseResultDto<VerifyGoogleTokenDto>
                {
                    Success = false,
                    Message = resultDto.Message
                };
            }

            return new ResponseResultDto<VerifyGoogleTokenDto>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyGoogleTokenGrainDto, VerifyGoogleTokenDto>(resultDto.Data)
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
            return new ResponseResultDto<VerifyGoogleTokenDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            };
        }
    }

    public async Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(tokenRequestDto.AccessToken);
            var resultDto =
                await grain.VerifyAppleTokenAsync(
                    ObjectMapper.Map<VerifyTokenRequestDto, VerifyTokenGrainDto>(tokenRequestDto));

            if (!resultDto.Success)
            {
                return new ResponseResultDto<VerifyAppleTokenDto>
                {
                    Success = false,
                    Message = resultDto.Message
                };
            }

            return new ResponseResultDto<VerifyAppleTokenDto>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyAppleTokenGrainDto, VerifyAppleTokenDto>(resultDto.Data)
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
            return new ResponseResultDto<VerifyAppleTokenDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            };
        }
    }


    private async Task<DidServerList> GetDidServerListAsync()
    {
        return await _distributedCache.GetOrAddAsync(
            CaServerListKey,
            async () => await GetCaServerListAsync(),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_whiteListExpireTimeOptions.ExpireTime)
            }
        );
    }

    private async Task<DidServerList> GetCaServerListAsync()
    {
        var didServerList = new DidServerList();
        var didServers = new List<DidServer>();
        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            var output = await _contractsProvider.GetCaServersListAsync(chainInfo.Value);
            var servers = output.CaServers;
            servers.ForEach(t =>
            {
                var didServer = _objectMapper.Map<CAServer, DidServer>(t);
                didServers.Add(didServer);
            });
            didServerList.DidServers = didServers;
        }

        return didServerList;
    }
}