using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Verifier.Dtos;
using CAVerifierServer.Application;
using CAVerifierServer.Contracts;
using CAVerifierServer.Exception;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using CAVerifierServer.VerifyRevokeCode;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IEnumerable<IVerifyRevokeCodeValidator> _revokeCodeValidators;

    private const string CaServerListKey = "CAServerListKey";

    public AccountAppService(IClusterClient clusterClient,
        IOptions<ChainOptions> chainOptions,
        IDistributedCache<DidServerList> distributedCache,
        IEnumerable<IVerifyCodeSender> verifyCodeSenders, IObjectMapper objectMapper,
        IOptions<WhiteListExpireTimeOptions> whiteListExpireTimeOption, ILogger<AccountAppService> logger,
        IContractsProvider contractsProvider, IOptionsSnapshot<FacebookOptions> facebookOptions,
        IHttpClientFactory httpClientFactory, IEnumerable<IVerifyRevokeCodeValidator> revokeCodeValidators)
    {
        _clusterClient = clusterClient;
        _distributedCache = distributedCache;
        _verifyCodeSenders = verifyCodeSenders;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractsProvider = contractsProvider;
        _httpClientFactory = httpClientFactory;
        _revokeCodeValidators = revokeCodeValidators;
        _facebookOptions = facebookOptions.Value;
        _whiteListExpireTimeOptions = whiteListExpireTimeOption.Value;
        _chainOptions = chainOptions.Value;
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifySecondaryEmailCodeAsync error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.SendVerificationRequestHandler))]
    public virtual async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(
        SendVerificationRequestInput input)
    {
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

        // try
        // {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.GuardianIdentifier);
            var dto = await grain.GetVerifyCodeAsync(input);
            if (!dto.Success)
            {
                return new ResponseResultDto<SendVerificationRequestDto>
                {
                    Success = false,
                    Message = dto.Message
                };
            }

            await verifyCodeSender.SendCodeByGuardianIdentifierAsync(input.GuardianIdentifier, dto.Data.VerifierCode, input.ShowOperationDetails);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = true,
                Data = new SendVerificationRequestDto
                {
                    VerifierSessionId = input.VerifierSessionId
                }
            };
        // }
        // catch (Exception e)
        // {
        //     Logger.LogError(e, Error.SendVerificationRequestErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<SendVerificationRequestDto>
        //     {
        //         Success = false,
        //         Message = Error.SendVerificationRequestErrorLogPrefix + e.Message
        //     };
        // }
    }

    [ExceptionHandler(typeof(System.Exception), Message = "SendNotificationRequestAsync error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.SendNotificationRequestHandler))]
    public virtual async Task<ResponseResultDto<bool>> SendNotificationRequestAsync(SendNotificationRequest request)
    {
        var emailSender = _verifyCodeSenders.FirstOrDefault(v => VerifierSenderType.Email.ToString().Equals(v.Type));
        if (emailSender == null)
        {
            return new ResponseResultDto<bool>
            {
                Success = false,
                Message = "found no emailSender"
            };
        }
        if (!emailSender.ValidateGuardianIdentifier(request.Email))
        {
            return new ResponseResultDto<bool>
            {
                Success = false,
                Message = Error.Message[Error.InvalidGuardianIdentifierInput]
            };
        }

        // try
        // {
            await emailSender.SendTransactionInfoNotificationAsync(request.Email, request.Template, request.ShowOperationDetails);
            return new ResponseResultDto<bool>
            {
                Success = true
            };
        // }
        // catch (Exception e)
        // {
        //     return new ResponseResultDto<bool>
        //     {
        //         Success = false,
        //         Message = e.Message
        //     };
        // }
    }
    
    [ExceptionHandler(typeof(System.Exception), Message = "SendTransactionInformationBeforeApprovalAsync error",
        TargetType = typeof(ApplicationExceptionHandler), LogTargets = ["email", "showOperationDetails"],
        MethodName = nameof(ApplicationExceptionHandler.SendTransactionInformationBeforeApprovalHandler))]
    public virtual async Task<ResponseResultDto<bool>> SendTransactionInformationBeforeApprovalAsync(string email, string showOperationDetails)
    {
        if (email.IsNullOrEmpty())
        {
            return new ResponseResultDto<bool>()
            {
                Success = false,
                Message = "Email is invalid"
            };
        }

        // try
        // {
            return await SendNotificationRequestAsync(new SendNotificationRequest()
            {
                Email = email,
                Template = EmailTemplate.BeforeApproval,
                ShowOperationDetails = showOperationDetails
            });
        // }
        // catch (Exception e)
        // {
        //     _logger.LogError(e, "SendNotificationRequestAsync Error");
        //     return new ResponseResultDto<bool>()
        //     {
        //         Success = false,
        //         Message = e.Message
        //     };
        // }
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyCodeAsync error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifyCodeHandler))]
    public virtual async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
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

        // try
        // {
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
        // }
        // catch (Exception e)
        // {
        //     Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<VerifierCodeDto>
        //     {
        //         Success = false,
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifySecondaryEmailCodeAsync error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifySecondaryEmailCodeHandler))]
    public virtual async Task<ResponseResultDto<bool>> VerifySecondaryEmailCodeAsync(SecondaryEmailVerifyCodeInput input)
    {
        if (input.VerifierSessionId == Guid.Empty
            || input.Code.IsNullOrEmpty() ||
            input.SecondaryEmail.IsNullOrEmpty())
        {
            return new ResponseResultDto<bool>
            {
                Success = false,
                Message = Error.Message[Error.NullOrEmptyInput]
            };
        }

        // try
        // {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.SecondaryEmail);
            var resultDto = await grain.VerifySecondaryEmailCodeAsync(input);
            if (resultDto.Success)
            {
                return new ResponseResultDto<bool>
                {
                    Success = true,
                    Data = true
                };
            }

            return new ResponseResultDto<bool>
            {
                Success = false,
                Message = resultDto.Message
            };
        // }
        // catch (Exception e)
        // {
        //     Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<bool>
        //     {
        //         Success = false,
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }
    
    [ExceptionHandler(typeof(System.Exception), Message = "SendVerificationToSecondaryEmail error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.SendVerificationToSecondaryEmailHandler))]
    public virtual async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationToSecondaryEmail(SecondaryEmailVerificationInput input)
    {
        var verifyCodeSender = _verifyCodeSenders.FirstOrDefault(v => VerifierSenderType.Email.ToString().Equals(v.Type));
        if (verifyCodeSender == null)
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.Unsupported]
            };
        }
        if (!verifyCodeSender.ValidateGuardianIdentifier(input.SecondaryEmail))
        {
            _logger.LogDebug("SendVerificationToSecondaryEmail ValidateGuardianIdentifier failed email:{0}", input.SecondaryEmail);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.InvalidGuardianIdentifierInput]
            };
        }

        // try
        // {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.SecondaryEmail);
            var dto = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput()
            {
                Type = VerifierSenderType.Email.ToString(),
                GuardianIdentifier = input.SecondaryEmail,
                VerifierSessionId = input.VerifierSessionId,
                OperationDetails = string.Empty
            });
            if (!dto.Success)
            {
                return new ResponseResultDto<SendVerificationRequestDto>
                {
                    Success = false,
                    Message = dto.Message
                };
            }

            await verifyCodeSender.SendCodeToSecondaryEmailAsync(input.SecondaryEmail, dto.Data.VerifierCode);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = true,
                Data = new SendVerificationRequestDto
                {
                    VerifierSessionId = input.VerifierSessionId
                }
            };
        // }
        // catch (Exception e)
        // {
        //     Logger.LogError(e, Error.SendVerificationRequestErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<SendVerificationRequestDto>
        //     {
        //         Success = false,
        //         Message = Error.SendVerificationRequestErrorLogPrefix + e.Message
        //     };
        // }
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
        var caIpList = endPoints.Select(ip => ip.Split("//")[1]).Select(formatter => formatter.Split(":")[0]).ToList();
        var result = ipList.Intersect(caIpList).ToList();
        return result.Count == 0 ? null : result[0];
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyGoogleToken error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifyGoogleTokenHandler))]
    public virtual async Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        // try
        // {
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
            var email = GetEmail(tokenRequestDto.SecondaryEmail, resultDto.Data.GoogleUserExtraInfo.Email);
            var sentResultDto = await SendTransactionInformationBeforeApprovalAsync(email, tokenRequestDto.ShowOperationDetails);
            if (sentResultDto is not { Success: true })
            {
                _logger.LogWarning("VerifyGoogleTokenAsync secondaryEmail failed email:{0} message:{1}", email, sentResultDto?.Message);
            }
            return new ResponseResultDto<VerifyGoogleTokenDto>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyGoogleTokenGrainDto, VerifyGoogleTokenDto>(resultDto.Data)
            };
        // }
        // catch (System.Exception e)
        // {
        //     Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<VerifyGoogleTokenDto>
        //     {
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }

    private static string GetEmail(string secondaryEmail, string emailFromGuardian)
    {
        return emailFromGuardian.IsNullOrEmpty() ? secondaryEmail : emailFromGuardian;
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyAppleToken error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifyAppleTokenHandler))]
    public virtual async Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        // try
        // {
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

            var email = resultDto.Data.AppleUserExtraInfo.IsPrivateEmail ? tokenRequestDto.SecondaryEmail : GetEmail(tokenRequestDto.SecondaryEmail, resultDto.Data.AppleUserExtraInfo.Email);
            var response = await SendTransactionInformationBeforeApprovalAsync(email, tokenRequestDto.ShowOperationDetails);
            if (response is { Success: false })
            {
                _logger.LogWarning("sent transaction email failed secondary email:{0} email:{1} showOperationDetails:{2}",
                    tokenRequestDto.SecondaryEmail, resultDto.Data.AppleUserExtraInfo.Email, tokenRequestDto.ShowOperationDetails);
            }
            return new ResponseResultDto<VerifyAppleTokenDto>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyAppleTokenGrainDto, VerifyAppleTokenDto>(resultDto.Data)
            };
        // }
        // catch (System.Exception e)
        // {
        //     Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<VerifyAppleTokenDto>
        //     {
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyTelegramToken error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifyTelegramTokenHandler))]
    public virtual async Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        // try
        // {
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
            await SendTransactionInformationBeforeApprovalAsync(tokenRequestDto.SecondaryEmail, tokenRequestDto.ShowOperationDetails);
            return new ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyTelegramTokenGrainDto, VerifyTokenDto<TelegramUserExtraInfo>>(
                    resultDto.Data)
            };
        // }
        // catch (System.Exception e)
        // {
        //     Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>
        //     {
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyFacebookToken error",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifyFacebookTokenHandler))]
    public virtual async Task<ResponseResultDto<VerifierCodeDto>> VerifyFacebookTokenAsync(VerifyTokenRequestDto input)
    {
        // try
        // {
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
            await SendTransactionInformationBeforeApprovalAsync(input.SecondaryEmail, input.ShowOperationDetails);
            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = true,
                Data = resultDto.Data
            };
        // }
        // catch (System.Exception e)
        // {
        //     Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        //     return new ResponseResultDto<VerifierCodeDto>
        //     {
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyFacebookAccessToken error",
        TargetType = typeof(ApplicationExceptionHandler), LogTargets = ["accessToken"],
        MethodName = nameof(ApplicationExceptionHandler.VerifyFacebookAccessTokenHandler))]
    public virtual async Task<ResponseResultDto<VerifyFacebookTokenResponseDto>> VerifyFacebookAccessTokenAsync(
        string accessToken)
    {
        var app_token = _facebookOptions.AppId + "%7C" + _facebookOptions.AppSecret;
        var requestUrl =
            "https://graph.facebook.com/debug_token?access_token=" + app_token + "&input_token=" + accessToken;
        // try
        // {
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
        // }
        // catch (System.Exception e)
        // {
        //     _logger.LogError(e, "Verify AccessToken failed,AccessToken is {accessToken}", accessToken);
        //     return new ResponseResultDto<VerifyFacebookTokenResponseDto>
        //     {
        //         Success = false,
        //         Message = "Verify Facebook accessToken failed."
        //     };
        // }

        return new ResponseResultDto<VerifyFacebookTokenResponseDto>
        {
            Success = false,
            Message = "Verify Facebook accessToken failed."
        };
    }

    [ExceptionHandler(typeof(System.Exception), Message = "VerifyTwitterToken error",
        TargetType = typeof(ApplicationExceptionHandler), LogTargets = ["{@tokenRequestDto}"],
        MethodName = nameof(ApplicationExceptionHandler.VerifyTwitterTokenHandler))]
    public virtual async Task<ResponseResultDto<VerifyTwitterTokenDto>> VerifyTwitterTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        // try
        // {
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
            await SendTransactionInformationBeforeApprovalAsync(tokenRequestDto.SecondaryEmail, tokenRequestDto.ShowOperationDetails);
            return new ResponseResultDto<VerifyTwitterTokenDto>
            {
                Success = true,
                Data = _objectMapper.Map<VerifyTwitterTokenGrainDto, VerifyTwitterTokenDto>(resultDto.Data)
            };
        // }
        // catch (System.Exception e)
        // {
        //     Logger.LogError(e,
        //         "verify twitter token error, accessToken:{accessToken}, identifierHash:{identifierHash}, salt:{salt}, operationType:{operationType}",
        //         tokenRequestDto.AccessToken, tokenRequestDto.IdentifierHash, tokenRequestDto.Salt,
        //         tokenRequestDto.OperationType);
        //
        //     return new ResponseResultDto<VerifyTwitterTokenDto>
        //     {
        //         Message = Error.VerifyCodeErrorLogPrefix + e.Message
        //     };
        // }
    }

    public async Task<VerifyRevokeCodeResponseDto> VerifyRevokeCodeAsync(
        VerifyRevokeCodeDto revokeCodeDto)
    {
        var revokeValidator = _revokeCodeValidators.FirstOrDefault(t => t.Type == revokeCodeDto.Type);
        if (null == revokeValidator)
        {
            return new VerifyRevokeCodeResponseDto
            {
                Success = false
            };
        }

        var result = await revokeValidator.VerifyRevokeCodeAsync(revokeCodeDto);

        if (result)
        {
            return new VerifyRevokeCodeResponseDto
            {
                Success = true
            };
        }

        return new VerifyRevokeCodeResponseDto
        {
            Success = false
        };
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