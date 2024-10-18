using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CAVerifierServer.CustomException;
using CAVerifierServer.Exception;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Sms;

namespace CAVerifierServer.Phone;

public class AwsSmsMessageSender : ISMSServiceSender
{
    public string ServiceName => "AWS";
    private readonly ILogger<AwsSmsMessageSender> _logger;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly AwssmsMessageOptions _awssmsMessageOptions;
    private readonly AmazonSimpleNotificationServiceClient _amazonSimpleNotificationServiceClient;
    private const string SuccessMark = "2";
    private readonly Regex _regex = new Regex("(.{6}).*(.{4})");
    private readonly SMSTemplateOptions _smsTemplateOptions;


    public AwsSmsMessageSender(ILogger<AwsSmsMessageSender> logger, IOptions<VerifierInfoOptions> verifierInfoOptions,
        IOptions<AwssmsMessageOptions> smsMessageOptions, IOptionsSnapshot<SMSTemplateOptions> smsTemplateOptions)
    {
        _logger = logger;
        _smsTemplateOptions = smsTemplateOptions.Value;
        _awssmsMessageOptions = smsMessageOptions.Value;
        _verifierInfoOptions = verifierInfoOptions.Value;
        _amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(
            _awssmsMessageOptions.AwsAccessKeyId, _awssmsMessageOptions.AwsSecretAccessKeyId,
            RegionEndpoint.GetBySystemName(_awssmsMessageOptions.SystemName));
    }

    [ExceptionHandler(typeof(System.Exception), Message = "AWS SMS Service Sending message error",
        TargetType = typeof(ApplicationExceptionHandler), 
        MethodName = nameof(ApplicationExceptionHandler.SendTextMessageHandler))]
    public virtual async Task SendTextMessageAsync(SmsMessage smsMessage)
    {
        // Now actually send the message.
        var request = new PublishRequest
        {
            Message = string.Format(_smsTemplateOptions.Template, _verifierInfoOptions.Name, smsMessage.Text),
            PhoneNumber = smsMessage.PhoneNumber
        };
        _logger.LogDebug("AWS SMS Service sending SMSMessage to {phoneNum}",
            _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement));
        var response = await _amazonSimpleNotificationServiceClient.PublishAsync(request);
        var isSuccess = Convert.ToInt32(response.HttpStatusCode).ToString().StartsWith(SuccessMark);
        if (!isSuccess)
        {
            _logger.LogError(
                "AWS SMS Service sending SMSMessage failed to {phoneNum}, ResponseCode is {statusCode}",
                _regex.Replace(smsMessage.PhoneNumber, CAVerifierServerApplicationConsts.PhoneNumReplacement),
                response.HttpStatusCode);
            throw new SmsSenderFailedException("AWS SMS Service sending SMSMessage failed");
        }
    }


    public async Task SendAsync(SmsMessage smsMessage)
    {
        await SendTextMessageAsync(smsMessage);
    }
}