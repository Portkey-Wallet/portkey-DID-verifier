using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.CustomException;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Twilio;
using Twilio.Rest.Verify.V2.Service;
using Volo.Abp.Sms;

namespace CAVerifierServer.Phone;

public class TwilioSmsMessageSender : ISMSServiceSender
{
    public string ServiceName => "Twilio";
    private readonly ILogger<AwsSmsMessageSender> _logger;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly TwilioSmsMessageOptions _twilioSmsMessageOptions;
    private readonly Regex _regex = new Regex("(.{6}).*(.{4})");
    private readonly Regex _CNRegex = new Regex(CAVerifierServerApplicationConsts.ChinaPhoneRegex);
    private const string CustomerVar = "custom_var";
    private const string VerifyStatus = "pending";

    public TwilioSmsMessageSender(ILogger<AwsSmsMessageSender> logger,
        IOptions<VerifierInfoOptions> verifierInfoOptions,
        IOptions<TwilioSmsMessageOptions> twilioSmsMessageOptions)
    {
        _logger = logger;
        _twilioSmsMessageOptions = twilioSmsMessageOptions.Value;
        _verifierInfoOptions = verifierInfoOptions.Value;
    }


    public async Task SendAsync(SmsMessage smsMessage)
    {
        var customSubstitutions = new Dictionary<string, string> { { CustomerVar, _verifierInfoOptions.Name } };
        var customSubstitutionsJsonStr = JsonConvert.SerializeObject(customSubstitutions);

        try
        {
            TwilioClient.Init(_twilioSmsMessageOptions.AccountSid, _twilioSmsMessageOptions.AuthToken);
            var isMatch = _CNRegex.IsMatch(smsMessage.PhoneNumber);
            var verification = await VerificationResource.CreateAsync(
                to: smsMessage.PhoneNumber,
                templateSid: isMatch ? null : _twilioSmsMessageOptions.TemplateId,
                locale: _twilioSmsMessageOptions.Locale,
                customCode: smsMessage.Text,
                templateCustomSubstitutions: customSubstitutionsJsonStr,
                channel: _twilioSmsMessageOptions.Channel,
                pathServiceSid: _twilioSmsMessageOptions.ServiceId
            );

            if (verification.Status != VerifyStatus)
            {
                _logger.LogError(
                    "Twilio SMS Service sending SMSMessage failed to {phoneNum}.",
                    _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement));
                throw new SmsSenderFailedException("Twilio SMS Service sending SMSMessage failed");
            }

            _logger.LogDebug("Twilio SMS Service sending SMSMessage to {phoneNum}",
                _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement));

            _logger.LogDebug("Start Approve phone,phoneNum is  {phoneNum}",
                _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement));


            await VerificationResource.UpdateAsync(
                status: VerificationResource.StatusEnum.Approved,
                pathServiceSid: _twilioSmsMessageOptions.ServiceId,
                pathSid: verification.Sid
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio SMS Service Sending message error : {ex}", ex.Message);
            throw ex;
        }
    }
}