using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Util;
using CAVerifierServer.Verifier.Dtos;
using CAVerifierServer.Application;
using CAVerifierServer.Contracts;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
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
    private readonly FacebookOptions _facebookOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string CaServerListKey = "CAServerListKey";

    public AccountAppService(IClusterClient clusterClient,
        IOptions<ChainOptions> chainOptions,
        IDistributedCache<DidServerList> distributedCache,
        IEnumerable<IVerifyCodeSender> verifyCodeSenders, IObjectMapper objectMapper,
        IOptions<WhiteListExpireTimeOptions> whiteListExpireTimeOption, ILogger<AccountAppService> logger,
        IContractsProvider contractsProvider, IOptionsSnapshot<FacebookOptions> facebookOptions,
        IHttpClientFactory httpClientFactory)
    {
        _clusterClient = clusterClient;
        _distributedCache = distributedCache;
        _verifyCodeSenders = verifyCodeSenders;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractsProvider = contractsProvider;
        _httpClientFactory = httpClientFactory;
        _facebookOptions = facebookOptions.Value;
        _whiteListExpireTimeOptions = whiteListExpireTimeOption.Value;
        _chainOptions = chainOptions.Value;
    }

    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(
        SendVerificationRequestInput input)
    {
        Stopwatch watcher1 = Stopwatch.StartNew();
        Stopwatch watcher2 = Stopwatch.StartNew();
        var verifyCodeSender = _verifyCodeSenders.FirstOrDefault(v => v.Type == input.Type);
        if (verifyCodeSender == null)
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.Unsupported]
            };
        }

        if (!verifyCodeSender.ValidateGuardianIdentifier(input.GuardianIdentifier))
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.InvalidGuardianIdentifierInput]
            };
        }
        watcher2.Stop();
        var watcher3 = Stopwatch.StartNew();
        var watcher4 = new Stopwatch();
        var watcher5 = new Stopwatch();
        try
        {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.GuardianIdentifier);
            watcher3.Stop();
            watcher4.Start();
            var dto = await grain.GetVerifyCodeAsync(input);
            if (!dto.Success)
            {
                return new ResponseResultDto<SendVerificationRequestDto>
                {
                    Success = false,
                    Message = dto.Message
                };
            }
            watcher4.Stop();
            watcher5.Start();

            await verifyCodeSender.SendCodeByGuardianIdentifierAsync(input.GuardianIdentifier, dto.Data.VerifierCode);
            var responseResultDto = new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = true,
                Data = new SendVerificationRequestDto
                {
                    VerifierSessionId = input.VerifierSessionId
                }
            };
            watcher5.Stop();
            return responseResultDto;
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
        finally
        {
            watcher1.Stop();
            int totalMilliseconds = watcher1.Elapsed.Milliseconds;
            if (totalMilliseconds > 1000)
            {
                Logger.LogInformation("exec SendVerificationRequest 1000, guardianIdentifier:{5}, cost:{0}," +
                                      " validate cost:{1}, get grain cost:{2}," +
                                      " getVerifyCode cost:{3}, sendCode cost:{4}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString(),
                    watcher4.Elapsed.Milliseconds.ToString(),
                    watcher5.Elapsed.Milliseconds.ToString());
            } else if (totalMilliseconds > 500)
            {
                Logger.LogInformation("exec SendVerificationRequest 500, guardianIdentifier:{5}, cost:{0}," +
                                      " validate cost:{1}, get grain cost:{2}," +
                                      " getVerifyCode cost:{3}, sendCode cost:{4}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString(),
                    watcher4.Elapsed.Milliseconds.ToString(),
                    watcher5.Elapsed.Milliseconds.ToString());
            } else if (totalMilliseconds > 200)
            {
                Logger.LogInformation("exec SendVerificationRequest 200, guardianIdentifier:{5}, cost:{0}," +
                                      " validate cost:{1}, get grain cost:{2}," +
                                      " getVerifyCode cost:{3}, sendCode cost:{4}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString(),
                    watcher4.Elapsed.Milliseconds.ToString(),
                    watcher5.Elapsed.Milliseconds.ToString());
            }
            else
            {
                Logger.LogInformation("exec SendVerificationRequest good, guardianIdentifier:{5}, cost:{0}," +
                                      " validate cost:{1}, get grain cost:{2}," +
                                      " getVerifyCode cost:{3}, sendCode cost:{4}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString(),
                    watcher4.Elapsed.Milliseconds.ToString(),
                    watcher5.Elapsed.Milliseconds.ToString());
            }
            
        }
    }


    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
    {
        var watcher1 = Stopwatch.StartNew();
        
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

        var watcher2 = Stopwatch.StartNew();
        var watcher3 = new Stopwatch();
        try
        {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.GuardianIdentifier);
            watcher2.Stop();
            watcher3.Start();
            var resultDto = await grain.VerifyAndCreateSignatureAsync(input);
            if (resultDto.Success)
            {
                var responseResultDto = new ResponseResultDto<VerifierCodeDto>
                {
                    Success = true,
                    Data = new VerifierCodeDto
                    {
                        VerificationDoc = resultDto.Data.Data,
                        Signature = resultDto.Data.Signature
                    }
                };
                watcher3.Stop();
                return responseResultDto;
            }
            else
            {
                watcher3.Stop();
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
        finally
        {
            watcher1.Stop();
            var totalMilliseconds = watcher1.Elapsed.Milliseconds;
            if (totalMilliseconds > 1000)
            {
                Logger.LogInformation("exec VerifyCode 1000, guardianIdentifier:{3}, cost:{0}," +
                                      " get grain cost:{1}," +
                                      " verifyAndCreateSignature cost:{2}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString());
            } else if (totalMilliseconds > 500)
            {
                Logger.LogInformation("exec VerifyCode 500, guardianIdentifier:{3}, cost:{0}," +
                                      " get grain cost:{1}," +
                                      " verifyAndCreateSignature cost:{2}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString());
            } else if (totalMilliseconds > 200) 
            {
                Logger.LogInformation("exec VerifyCode 200, guardianIdentifier:{3}, cost:{0}," +
                                      " get grain cost:{1}," +
                                      " verifyAndCreateSignature cost:{2}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString());
            }
            else
            {
                Logger.LogInformation("exec VerifyCode good, guardianIdentifier:{3}, cost:{0}," +
                                      " get grain cost:{1}," +
                                      " verifyAndCreateSignature cost:{2}",
                    input.GuardianIdentifier,
                    totalMilliseconds.ToString(),
                    watcher2.Elapsed.Milliseconds.ToString(),
                    watcher3.Elapsed.Milliseconds.ToString());
            }
        }
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

    public async Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(tokenRequestDto.AccessToken);
            var resultDto =
                await grain.VerifyTelegramTokenAsync(
                    ObjectMapper.Map<VerifyTokenRequestDto, VerifyTokenGrainDto>(tokenRequestDto));

            if (!resultDto.Success)
            {
                return new ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>
                {
                    Success = false,
                    Message = resultDto.Message
                };
            }

            return new ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyTelegramTokenGrainDto, VerifyTokenDto<TelegramUserExtraInfo>>(
                    resultDto.Data)
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
            return new ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            };
        }
    }

    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyFacebookTokenAsync(VerifyTokenRequestDto input)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(input.AccessToken);
            var resultDto =
                await grain.VerifyFacebookTokenAsync(
                    ObjectMapper.Map<VerifyTokenRequestDto, VerifyTokenGrainDto>(input));

            if (!resultDto.Success)
            {
                return new ResponseResultDto<VerifierCodeDto>
                {
                    Success = false,
                    Message = resultDto.Message
                };
            }

            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = true,
                Data = resultDto.Data
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
            return new ResponseResultDto<VerifierCodeDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            };
        }
    }

    public async Task<ResponseResultDto<VerifyFacebookTokenResponseDto>> VerifyFacebookAccessTokenAsync(
        string accessToken)
    {
        var app_token = _facebookOptions.AppId + "%7C" + _facebookOptions.AppSecret;
        var requestUrl =
            "https://graph.facebook.com/debug_token?access_token=" + app_token + "&input_token=" + accessToken;
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

            var result = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("{Message}", response.ToString());
                return new ResponseResultDto<VerifyFacebookTokenResponseDto>
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            if (response.IsSuccessStatusCode)
            {
                var verifyUserInfo = JsonConvert.DeserializeObject<VerifyFacebookResultResponse>(result);
                if (verifyUserInfo == null)
                {
                    return new ResponseResultDto<VerifyFacebookTokenResponseDto>
                    {
                        Success = false,
                        Message = "Verify Facebook userInfo fail."
                    };
                }

                if (!verifyUserInfo.Data.IsValid)
                {
                    return new ResponseResultDto<VerifyFacebookTokenResponseDto>
                    {
                        Success = false,
                        Message = "Verify accessToken from Facebook fail."
                    };
                }

                if (verifyUserInfo.Data.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return new ResponseResultDto<VerifyFacebookTokenResponseDto>
                    {
                        Success = false,
                        Message = "Token Expired"
                    };
                }

                return new ResponseResultDto<VerifyFacebookTokenResponseDto>
                {
                    Success = true,
                    Data = verifyUserInfo.Data
                };
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Verify AccessToken failed,AccessToken is {accessToken}", accessToken);
            return new ResponseResultDto<VerifyFacebookTokenResponseDto>
            {
                Success = false,
                Message = "Verify Facebook accessToken failed."
            };
        }

        return new ResponseResultDto<VerifyFacebookTokenResponseDto>
        {
            Success = false,
            Message = "Verify Facebook accessToken failed."
        };
    }

    public async Task<ResponseResultDto<VerifyTwitterTokenDto>> VerifyTwitterTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(tokenRequestDto.AccessToken);
            var resultDto =
                await grain.VerifyTwitterTokenAsync(
                    ObjectMapper.Map<VerifyTokenRequestDto, VerifyTokenGrainDto>(tokenRequestDto));

            if (!resultDto.Success)
            {
                return new ResponseResultDto<VerifyTwitterTokenDto>
                {
                    Success = false,
                    Message = resultDto.Message
                };
            }

            return new ResponseResultDto<VerifyTwitterTokenDto>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyTwitterTokenGrainDto, VerifyTwitterTokenDto>(resultDto.Data)
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                "verify twitter token error, accessToken:{accessToken}, identifierHash:{identifierHash}, salt:{salt}, operationType:{operationType}",
                tokenRequestDto.AccessToken, tokenRequestDto.IdentifierHash, tokenRequestDto.Salt,
                tokenRequestDto.OperationType);

            return new ResponseResultDto<VerifyTwitterTokenDto>
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