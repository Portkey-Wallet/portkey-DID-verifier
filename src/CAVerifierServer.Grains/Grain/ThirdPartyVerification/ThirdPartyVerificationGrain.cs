using System.IdentityModel.Tokens.Jwt;
using System.Net;
using AElf;
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
    private readonly ISigner _signer;

    public ThirdPartyVerificationGrain(IHttpClientFactory httpClientFactory,
        IOptions<VerifierAccountOptions> verifierAccountOptions,
        IOptions<AppleAuthOptions> appleAuthVerifyOption,
        IObjectMapper objectMapper,
        ILogger<ThirdPartyVerificationGrain> logger,
        IDistributedCache<AppleKeys> distributedCache,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        ISigner signer)
    {
        _httpClientFactory = httpClientFactory;
        _verifierAccountOptions = verifierAccountOptions.Value;
        _appleAuthOptions = appleAuthVerifyOption.Value;
        _objectMapper = objectMapper;
        _logger = logger;
        _distributedCache = distributedCache;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _signer = signer;
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

            var verificationDoc = VerificationDocFactory.Create(_signer.GetAddress(),
                Convert.ToInt16(GuardianIdentifierType.Google), grainDto.Salt,
                grainDto.IdentifierHash, grainDto.OperationType, grainDto.ChainId).GetStringRepresentation();
            var signature = _signer.Sign(HashHelper.ComputeFrom(verificationDoc));

            tokenDto.Signature = signature.ToHex();
            tokenDto.VerificationDoc = verificationDoc;

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

            var verificationDoc = VerificationDocFactory.Create(_signer.GetAddress(),
                Convert.ToInt16(GuardianIdentifierType.Apple), grainDto.Salt,
                grainDto.IdentifierHash, grainDto.OperationType, grainDto.ChainId).GetStringRepresentation();
            var signature = _signer.Sign(HashHelper.ComputeFrom(verificationDoc));

            return new GrainResultDto<VerifyAppleTokenGrainDto>
            {
                Success = true,
                Data = new VerifyAppleTokenGrainDto
                {
                    AppleUserExtraInfo = userInfo,
                    Signature = signature.ToHex(),
                    VerificationDoc = verificationDoc
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

    private async Task<GoogleUserInfoDto> GetUserInfoFromGoogleAsync(string accessToken)
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

    private async Task<SecurityToken> ValidateTokenAsync(string identityToken)
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
}