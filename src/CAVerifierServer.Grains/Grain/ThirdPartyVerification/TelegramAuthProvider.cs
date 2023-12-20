using System.Security.Cryptography;
using System.Text;
using AElf;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public interface ITelegramAuthProvider
{
    Task<bool> ValidateTelegramHashAsync(TelegramUserExtraInfo telegramAuthDto);
}

public class TelegramAuthProvider : ISingletonDependency, ITelegramAuthProvider
{
    private ILogger<TelegramAuthProvider> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;

    public TelegramAuthProvider(ILogger<TelegramAuthProvider> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
    }

    public async Task<bool> ValidateTelegramHashAsync(TelegramUserExtraInfo telegramAuthDto)
    {
        if (telegramAuthDto.Hash.IsNullOrWhiteSpace())
        {
            _logger.LogError("hash parameter in the telegram callback is null. id={0}", telegramAuthDto.Id);
            return false;
        }

        var token = _telegramAuthOptions.Bots[_telegramAuthOptions.DefaultUsed].Token;
        var dataCheckString = GetDataCheckString(telegramAuthDto);
        var localHash = await GenerateTelegramHashAsync(token, dataCheckString);

        if (!localHash.Equals(telegramAuthDto.Hash))
        {
            _logger.LogError("verification of the telegram information has failed. id={0}", telegramAuthDto.Id);
            return false;
        }

        if (!telegramAuthDto.AuthDate.IsNullOrWhiteSpace())
        {
            //validate auth date
            var expiredUnixTimestamp = (long)DateTime.UtcNow.AddSeconds(-_telegramAuthOptions.Expire)
                .Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var authDate = long.Parse(telegramAuthDto.AuthDate);
            if (authDate < expiredUnixTimestamp)
            {
                _logger.LogError("verification of the telegram information has failed, login timeout. id={0}", telegramAuthDto.Id);
                return false;
            }
        }

        return true;
    }
    
    private static string GetDataCheckString(TelegramUserExtraInfo telegramAuthDto)
    {
        Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
        if (telegramAuthDto.Id != null)
        {
            keyValuePairs.Add("id", telegramAuthDto.Id);
        }

        if (telegramAuthDto.UserName != null)
        {
            keyValuePairs.Add("username", telegramAuthDto.UserName);
        }

        if (telegramAuthDto.AuthDate != null)
        {
            keyValuePairs.Add("auth_date", telegramAuthDto.AuthDate);
        }

        if (telegramAuthDto.FirstName != null)
        {
            keyValuePairs.Add("first_name", telegramAuthDto.FirstName);
        }

        if (telegramAuthDto.LastName != null)
        {
            keyValuePairs.Add("last_name", telegramAuthDto.LastName);
        }

        if (telegramAuthDto.ProtoUrl != null)
        {
            keyValuePairs.Add("photo_url", telegramAuthDto.ProtoUrl);
        }
        var sortedByKey = keyValuePairs.Keys.OrderBy(k => k);
        StringBuilder sb = new StringBuilder();
        foreach (var key in sortedByKey)
        {
            sb.AppendLine($"{key}={keyValuePairs[key]}");
        }

        sb.Length = sb.Length - 1;
        return sb.ToString();
    }

    private static Task<string> GenerateTelegramHashAsync(string token, string dataCheckString)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] dataCheckStringBytes = Encoding.UTF8.GetBytes(dataCheckString);
        var computeFrom = HashHelper.ComputeFrom(tokenBytes).ToByteArray();

        using var hmac = new HMACSHA256(computeFrom);
        var hashBytes = hmac.ComputeHash(dataCheckStringBytes);
        return Task.FromResult(hashBytes.ToHex());
    }
}