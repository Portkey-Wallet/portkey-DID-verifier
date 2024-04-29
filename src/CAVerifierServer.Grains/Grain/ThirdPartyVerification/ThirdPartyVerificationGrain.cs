using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using CAVerifierServer.Telegram;
using CAVerifierServer.Telegram.Options;
using CAVerifierServer.Verifier.Dtos;
using CAVerifierServer.Account;
using CAVerifierServer.Grains.Common;
using CAVerifierServer.Grains.Dto;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Grains.State;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public class ThirdPartyVerificationGrain : Grain<ThirdPartyVerificationState>, IThirdPartyVerificationGrain
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly VerifierAccountOptions _verifierAccountOptions;
    private readonly AppleAuthOptions _appleAuthOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ThirdPartyVerificationGrain> _logger;
    private readonly IDistributedCache<AppleKeys> _distributedCache;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly JwtTokenOptions _jwtTokenOptions;
    private readonly ITelegramAuthProvider _telegramAuthProvider;
    private readonly TelegramAuthOptions _telegramAuthOptions;

    public ThirdPartyVerificationGrain(IHttpClientFactory httpClientFactory,
        IOptions<VerifierAccountOptions> verifierAccountOptions,
        IOptions<AppleAuthOptions> appleAuthVerifyOption,
        IObjectMapper objectMapper,
        ILogger<ThirdPartyVerificationGrain> logger,
        IDistributedCache<AppleKeys> distributedCache,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IOptionsSnapshot<JwtTokenOptions> jwtTokenOptions,
        ITelegramAuthProvider telegramAuthProvider,
        IOptions<TelegramAuthOptions> telegramAuthOptions)
    {
        _httpClientFactory = httpClientFactory;
        _verifierAccountOptions = verifierAccountOptions.Value;
        _appleAuthOptions = appleAuthVerifyOption.Value;
        _objectMapper = objectMapper;
        _logger = logger;
        _distributedCache = distributedCache;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _jwtTokenOptions = jwtTokenOptions.Value;
        _telegramAuthProvider = telegramAuthProvider;
        _telegramAuthOptions = telegramAuthOptions.Value;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<VerifyGoogleTokenGrainDto>> VerifyGoogleTokenAsync(VerifyTokenGrainDto grainDto)
    {
        try
        {
            var googleUserInfo = await GetUserInfoFromGoogleAsync(grainDto.AccessToken);

            var tokenDto = new VerifyGoogleTokenGrainDto
            {
                GoogleUserExtraInfo = _objectMapper.Map<GoogleUserInfoDto, GoogleUserExtraInfo>(googleUserInfo)
            };

            tokenDto.GoogleUserExtraInfo.GuardianType = GuardianIdentifierType.Google.ToString();
            tokenDto.GoogleUserExtraInfo.AuthTime = DateTime.UtcNow;

            var signatureOutput = CryptographyHelper.GenerateSignature(Convert.ToInt16(GuardianIdentifierType.Google),
                grainDto.Salt, grainDto.IdentifierHash, _verifierAccountOptions.PrivateKey, grainDto.OperationType,
                grainDto.ChainId, grainDto.OperationDetails);

            tokenDto.Signature = signatureOutput.Signature;
            tokenDto.VerificationDoc = signatureOutput.Data;

            return new GrainResultDto<VerifyGoogleTokenGrainDto>
            {
                Success = true,
                Data = tokenDto
            };
        }
        catch (Exception e)
        {
            return new GrainResultDto<VerifyGoogleTokenGrainDto>
            {
                Message = e.Message
            };
        }
    }

    public async Task<GrainResultDto<VerifyAppleTokenGrainDto>> VerifyAppleTokenAsync(VerifyTokenGrainDto grainDto)
    {
        try
        {
            var securityToken = await ValidateTokenAsync(grainDto.AccessToken);
            var userInfo = GetUserInfoFromToken(securityToken);

            userInfo.GuardianType = GuardianIdentifierType.Apple.ToString();
            userInfo.AuthTime = DateTime.UtcNow;

            var signatureOutput =
                CryptographyHelper.GenerateSignature(Convert.ToInt16(GuardianIdentifierType.Apple), grainDto.Salt,
                    grainDto.IdentifierHash, _verifierAccountOptions.PrivateKey, grainDto.OperationType,
                    grainDto.ChainId, grainDto.OperationDetails);

            return new GrainResultDto<VerifyAppleTokenGrainDto>
            {
                Success = true,
                Data = new VerifyAppleTokenGrainDto
                {
                    AppleUserExtraInfo = userInfo,
                    Signature = signatureOutput.Signature,
                    VerificationDoc = signatureOutput.Data
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            return new GrainResultDto<VerifyAppleTokenGrainDto>
            {
                Message = e.Message
            };
        }
    }

    public async Task<GrainResultDto<VerifierCodeDto>> VerifyFacebookTokenAsync(VerifyTokenGrainDto grainDto)
    {
        try
        {
            var signatureOutput =
                CryptographyHelper.GenerateSignature(Convert.ToInt16(GuardianIdentifierType.Facebook), grainDto.Salt,
                    grainDto.IdentifierHash,
                    _verifierAccountOptions.PrivateKey, grainDto.OperationType, grainDto.ChainId,
                    grainDto.OperationDetails);

            
            return new GrainResultDto<VerifierCodeDto>
            {
                Success = true,
                Data = new VerifierCodeDto
                {
                    Signature = signatureOutput.Signature,
                    VerificationDoc = signatureOutput.Data
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            return new GrainResultDto<VerifierCodeDto>
            {
                Message = e.Message
            };
        }
    }

    public async Task<GrainResultDto<VerifyTelegramTokenGrainDto>> VerifyTelegramTokenAsync(
        VerifyTokenGrainDto tokenGrainDto)
    {
        try
        {
            var securityToken = await ValidateTelegramTokenAsync(tokenGrainDto.AccessToken);
            var expire = securityToken.ValidTo;
            if (expire < DateTime.UtcNow)
            {
                throw new Exception(ThirdPartyMessage.TokenExpiresMessage);
            }

            var userInfo = GetTelegramUserInfoFromToken(securityToken);

            if (!await _telegramAuthProvider.ValidateTelegramHashAsync(userInfo))
            {
                throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
            }

            userInfo.GuardianType = GuardianIdentifierType.Telegram.ToString();
            userInfo.AuthTime = DateTime.UtcNow;

            var signatureOutput =
                CryptographyHelper.GenerateSignature(Convert.ToInt16(GuardianIdentifierType.Telegram),
                    tokenGrainDto.Salt, tokenGrainDto.IdentifierHash,
                    _verifierAccountOptions.PrivateKey, tokenGrainDto.OperationType, tokenGrainDto.ChainId,
                    tokenGrainDto.OperationDetails);

            return new GrainResultDto<VerifyTelegramTokenGrainDto>
            {
                Success = true,
                Data = new VerifyTelegramTokenGrainDto
                {
                    TelegramUserExtraInfo = userInfo,
                    Signature = signatureOutput.Signature,
                    VerificationDoc = signatureOutput.Data
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            return new GrainResultDto<VerifyTelegramTokenGrainDto>
            {
                Message = e.Message
            };
        }
    }

    public async Task<GrainResultDto<VerifyTwitterTokenGrainDto>> VerifyTwitterTokenAsync(VerifyTokenGrainDto grainDto)
    {
        try
        {
            var userInfo = await GetTwitterUserInfoAsync(grainDto.AccessToken);
            var tokenDto = new VerifyTwitterTokenGrainDto
            {
                TwitterUserExtraInfo = _objectMapper.Map<TwitterUserInfo, TwitterUserExtraInfo>(userInfo)
            };

            tokenDto.TwitterUserExtraInfo.GuardianType = GuardianIdentifierType.Twitter.ToString();
            tokenDto.TwitterUserExtraInfo.AuthTime = DateTime.UtcNow;

            var signatureOutput = CryptographyHelper.GenerateSignature(Convert.ToInt16(GuardianIdentifierType.Twitter),
                grainDto.Salt,
                grainDto.IdentifierHash, _verifierAccountOptions.PrivateKey, grainDto.OperationType,
                grainDto.ChainId,grainDto.OperationDetails);

            tokenDto.Signature = signatureOutput.Signature;
            tokenDto.VerificationDoc = signatureOutput.Data;

            return new GrainResultDto<VerifyTwitterTokenGrainDto>
            {
                Success = true,
                Data = tokenDto
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            return new GrainResultDto<VerifyTwitterTokenGrainDto>
            {
                Message = e.Message
            };
        }
    }

    public async Task<TwitterUserInfo> GetTwitterUserInfoAsync(string accessToken)
    {
        var requestUrl = "https://api.twitter.com/2/users/me";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        var result = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("get user info unauthorized, result:{message}", response.ToString());
            throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("get user info fail, result:{message}", response.ToString());
            throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
        }

        _logger.LogInformation("get user info from twitter, result:{message}", result);
        var userInfo = JsonConvert.DeserializeObject<TwitterUserInfoDto>(result);
        if (userInfo?.Data == null)
        {
            throw new Exception("Get userInfo from twitter fail.");
        }

        return userInfo.Data;
    }

    public async Task<GoogleUserInfoDto> GetUserInfoFromGoogleAsync(string accessToken)
    {
        var requestUrl = $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}";

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        var result = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
        }

        _logger.LogInformation("GetUserInfo from google: {userInfo}", result);
        var googleUserInfo = JsonConvert.DeserializeObject<GoogleUserInfoDto>(result);
        if (googleUserInfo == null)
        {
            throw new Exception("Get userInfo from google fail.");
        }

        return googleUserInfo;
    }

    public async Task<SecurityToken> ValidateTokenAsync(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            var kid = jwtToken.Header["kid"].ToString();
            var appleKey = await GetAppleKeyAsync(kid);
            var jwk = new JsonWebKey(JsonConvert.SerializeObject(appleKey));

            var validateParameter = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudiences = _appleAuthOptions.Audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwk
            };

            _jwtSecurityTokenHandler.ValidateToken(identityToken, validateParameter,
                out SecurityToken validatedToken);

            return validatedToken;
        }
        catch (SecurityTokenExpiredException e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            throw new Exception(ThirdPartyMessage.TokenExpiresMessage);
        }
        catch (SecurityTokenException e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, Error.VerifyAppleErrorLogPrefix + e.Message);
            throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
        }
    }

    public async Task<SecurityToken> ValidateTelegramTokenAsync(string identityToken)
    {
        try
        {
            var jwkDto = await GetTelegramJwkFormTelegramAuthAsync();
            var jwk = new JsonWebKey(JsonConvert.SerializeObject(jwkDto));
            var validateParameter = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtTokenOptions.Issuer,
                ValidateAudience = true,
                ValidAudiences = _jwtTokenOptions.Audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                IssuerSigningKey = jwk
            };

            _jwtSecurityTokenHandler.ValidateToken(identityToken, validateParameter,
                out SecurityToken validatedToken);

            return validatedToken;
        }
        catch (SecurityTokenExpiredException e)
        {
            _logger.LogError(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
            throw new Exception(ThirdPartyMessage.TokenExpiresMessage);
        }
        catch (SecurityTokenException e)
        {
            _logger.LogError(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
            throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
            throw new Exception(ThirdPartyMessage.InvalidTokenMessage);
        }
    }

    private AppleUserExtraInfo GetUserInfoFromToken(SecurityToken validatedToken)
    {
        var jwtPayload = ((JwtSecurityToken)validatedToken).Payload;
        var userInfo = new AppleUserExtraInfo
        {
            Id = jwtPayload.Sub
        };

        if (jwtPayload.ContainsKey("email"))
        {
            userInfo.Email = jwtPayload["email"].ToString();
        }

        if (jwtPayload.ContainsKey("email_verified"))
        {
            userInfo.VerifiedEmail = Convert.ToBoolean(jwtPayload["email_verified"]);
        }

        if (jwtPayload.ContainsKey("is_private_email"))
        {
            userInfo.IsPrivateEmail = Convert.ToBoolean(jwtPayload["is_private_email"]);
        }

        return userInfo;
    }

    private TelegramUserExtraInfo GetTelegramUserInfoFromToken(SecurityToken validatedToken)
    {
        var jwtPayload = ((JwtSecurityToken)validatedToken).Payload;
        var userInfo = new TelegramUserExtraInfo();
        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.UserId))
        {
            userInfo.Id = jwtPayload[TelegramTokenClaimNames.UserId].ToString();
        }

        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.UserName))
        {
            userInfo.UserName = jwtPayload[TelegramTokenClaimNames.UserName].ToString();
        }

        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.AuthDate))
        {
            userInfo.AuthDate = jwtPayload[TelegramTokenClaimNames.AuthDate].ToString();
        }

        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.FirstName))
        {
            userInfo.FirstName = jwtPayload[TelegramTokenClaimNames.FirstName].ToString();
        }

        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.LastName))
        {
            userInfo.LastName = jwtPayload[TelegramTokenClaimNames.LastName].ToString();
        }

        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.Hash))
        {
            userInfo.Hash = jwtPayload[TelegramTokenClaimNames.Hash].ToString();
        }

        if (jwtPayload.ContainsKey(TelegramTokenClaimNames.ProtoUrl))
        {
            userInfo.PhotoUrl = jwtPayload[TelegramTokenClaimNames.ProtoUrl].ToString();
        }

        return userInfo;
    }

    private async Task<AppleKey> GetAppleKeyAsync(string kid)
    {
        var appleKeys = await GetAppleKeysAsync();
        return appleKeys.Keys.FirstOrDefault(t => t.Kid == kid);
    }

    private async Task<AppleKeys> GetAppleKeysAsync()
    {
        return await _distributedCache.GetOrAddAsync(
            "apple.auth.keys",
            async () => await GetAppleKeyFormAppleAsync(),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddHours(_appleAuthOptions.KeysExpireTime)
            }
        );
    }

    private async Task<AppleKeys> GetAppleKeyFormAppleAsync()
    {
        var appleKeyUrl = "https://appleid.apple.com/auth/keys";
        var response = await _httpClientFactory.CreateClient().GetStringAsync(appleKeyUrl);

        return JsonConvert.DeserializeObject<AppleKeys>(response);
    }

    private async Task<JwkDto> GetTelegramJwkFormTelegramAuthAsync()
    {
        var url = $"{_telegramAuthOptions.BaseUrl}/api/app/auth/key";
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_telegramAuthOptions.Timeout);
        var response = await httpClient.GetStringAsync(url);
        var resultDto = JsonConvert.DeserializeObject<GrainResultDto<JwkDto>>(response);
        return resultDto?.Data;
    }
}