using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.CustomException;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelesignEnterprise;
using Volo.Abp.Sms;

namespace CAVerifierServer.Phone;

public class TelesignSmsMessageSender : ISMSServiceSender
{
    public string ServiceName => "Telesign";
    private readonly ILogger<TelesignSmsMessageSender> _logger;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly TelesignSMSMessageOptions _telesignSMSMessageOptions;
    private readonly MessagingClient _messagingClient;
    private readonly Regex _regex = new Regex("(.{6}).*(.{4})");
    private readonly SMSTemplateOptions _smsTemplateOptions;

    public TelesignSmsMessageSender(ILogger<TelesignSmsMessageSender> logger,
        IOptions<VerifierInfoOptions> verifierInfoOptions,
        IOptions<TelesignSMSMessageOptions> telesignSmsMessageOptions,
        IOptionsSnapshot<SMSTemplateOptions> smsTemplateOptions)
    {
        _logger = logger;
        _smsTemplateOptions = smsTemplateOptions.Value;
        _telesignSMSMessageOptions = telesignSmsMessageOptions.Value;
        _verifierInfoOptions = verifierInfoOptions.Value;
        _messagingClient =
            new MessagingClient(_telesignSMSMessageOptions.CustomerId, _telesignSMSMessageOptions.ApiKey);
    }

    private async Task SendTextMessageAsync(SmsMessage smsMessage)
    {
        var phoneNumber = smsMessage.PhoneNumber;
        var message = string.Format(_smsTemplateOptions.Template, _verifierInfoOptions.Name, smsMessage.Text);
        try
        {
            _logger.LogDebug("Telesign SMS Service sending SMSMessage to {phoneNum}",
                _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement));
            var response = await _messagingClient.MessageAsync(phoneNumber, message, _telesignSMSMessageOptions.Type);
            if (!response.OK)
            {
                _logger.LogError(
                    "Telesign SMS Service sending SMSMessage failed to {phoneNum}",
                    _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement));
                throw new SmsSenderFailedException("Telesign SMS Service sending SMSMessage failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telesign SMS Service sending message error : {ex}", ex.Message);
            throw ex;
        }
    }

    public async Task SendAsync(SmsMessage smsMessage)
    {
        await SendTextMessageAsync(smsMessage);
    }
}