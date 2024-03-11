using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CAVerifierServer.Grains.Dto;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramAuthProvider(ILogger<TelegramAuthProvider> logger,
        IOptions<TelegramAuthOptions> telegramAuthOptions, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> ValidateTelegramHashAsync(TelegramUserExtraInfo telegramAuthDto)
    {
        if (telegramAuthDto.Hash.IsNullOrWhiteSpace())
        {
            _logger.LogError("hash parameter in the telegram callback is null. id={0}", telegramAuthDto.Id);
            return false;
        }

        var url = $"{_telegramAuthOptions.BaseUrl}/api/app/auth/verify";
        var properties = telegramAuthDto.GetType().GetProperties();
        var parameters = properties.ToDictionary(property => property.Name,
            property => property.GetValue(telegramAuthDto)?.ToString());

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_telegramAuthOptions.Timeout);
        httpClient.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse($"application/json"));

        var paramsStr = JsonSerializer.Serialize(parameters);
        HttpContent content = new StringContent(paramsStr, Encoding.UTF8, "application/json");
        content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/json");

        var response = await httpClient.PostAsync(url, content);
        var stream = await response.Content.ReadAsStreamAsync();
        var resultDto = await JsonSerializer.DeserializeAsync<GrainResultDto<bool>>(stream, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (resultDto == null || !resultDto.Success)
        {
            _logger.LogError("verification of the telegram information has failed. {0}", resultDto?.Message);
            return false;
        }

        return resultDto.Success;
    }
}