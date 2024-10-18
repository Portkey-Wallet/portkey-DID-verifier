using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Exception;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CAVerifierServer.VerifyRevokeCode;

public class FaceBookRevokeCodeValidator : IVerifyRevokeCodeValidator
{
    private readonly ILogger<FaceBookRevokeCodeValidator> _logger;
    private readonly FacebookOptions _facebookOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public FaceBookRevokeCodeValidator(ILogger<FaceBookRevokeCodeValidator> logger,
        IOptionsSnapshot<FacebookOptions> facebookOptions, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _facebookOptions = facebookOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public string Type => "Facebook";

    [ExceptionHandler(typeof(System.Exception), Message = "validate Facebook token failed",
        TargetType = typeof(ApplicationExceptionHandler), 
        MethodName = nameof(ApplicationExceptionHandler.VerifyRevokeCodeHandler))]
    public virtual async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var result = await VerifyFacebookAccessTokenAsync(revokeCodeDto.VerifyCode);
        if (result)
        {
            return true;
        }

        _logger.LogError("validate Facebook token failed");
        return false;
    }

    [ExceptionHandler(typeof(System.Exception), Message = "Verify AccessToken failed,AccessToken is",
        TargetType = typeof(ApplicationExceptionHandler), LogTargets = ["accessToken"],
        MethodName = nameof(ApplicationExceptionHandler.VerifyRevokeCodeHandler))]
    public virtual async Task<bool> VerifyFacebookAccessTokenAsync(string accessToken)
    {
        var appToken = _facebookOptions.AppId + "%7C" + _facebookOptions.AppSecret;
        var requestUrl =
            "https://graph.facebook.com/debug_token?access_token=" + appToken + "&input_token=" + accessToken;
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        var result = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("{Message}", response.ToString());
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            var verifyUserInfo = JsonConvert.DeserializeObject<VerifyFacebookResultResponse>(result);
            if (verifyUserInfo == null)
            {
                _logger.LogError("Verify Facebook userInfo fail.");
                return false;
            }

            if (!verifyUserInfo.Data.IsValid)
            {
                _logger.LogError("Verify accessToken from Facebook fail.");
                return false;
            }

            if (verifyUserInfo.Data.ExpiresAt >= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return true;
            }

            _logger.LogError("Token Expired");
            return false;
        }

        return false;
    }
}