using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Emailing;
using Volo.Abp.Sms;

namespace CAVerifierServer.SmsSender;

public partial class SmsSenderTest
{
    private IEmailSender GetMockEmailSender()
    {
        var mockEmailSender = new Mock<IEmailSender>();
        mockEmailSender.Setup(o => o.QueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        return mockEmailSender.Object;
    }

    private ISmsSender GetMockSmsSender()
    {
        var mockSmsSender = new Mock<ISmsSender>();
        mockSmsSender.Setup(o => o.SendAsync(It.IsAny<SmsMessage>()))
            .Returns(Task.CompletedTask);
        return mockSmsSender.Object;
    }

    private ISMSServiceSender GetMockSmsServiceSender()
    {
        var mockSmsSender = new Mock<ISMSServiceSender>();
        mockSmsSender.Setup(o => o.ServiceName).Returns("MockSmsServiceSender");
        mockSmsSender.Setup(o => o.SendAsync(It.IsAny<SmsMessage>()))
            .Returns(Task.CompletedTask);
        return mockSmsSender.Object;
    }

    private IOptions<SmsServiceOptions> GetSmsServiceOptions()
    {
        var smsServiceDic = new Dictionary<string, SmsServiceOption>();
        smsServiceDic.Add("MockSmsServiceSender", new SmsServiceOption
        {
            SupportingCountriesRatio = new Dictionary<string, int>
            {
                { "CN", 1 }
            },
        });
        return new OptionsWrapper<SmsServiceOptions>(
            new SmsServiceOptions
            {
                SmsServiceInfos = smsServiceDic
            });
    }

    private IOptionsSnapshot<SMSTemplateOptions> GetSmsTemplateOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<SMSTemplateOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new SMSTemplateOptions
            {
                Template =
                    "[{0}] Portkey Code: {1}. Expires in 10 minutes. Please ignore this if you didn’t request a code.",
            });
        return mockOptionsSnapshot.Object;
    }


    private IOptions<AwsEmailOptions> GetAwsEmailOptions()
    {
        return new OptionsWrapper<AwsEmailOptions>(
            new AwsEmailOptions
            {
                From = "sam@XXXXX.com",
                ConfigSet = "MockConfigSet",
                FromName = "MockName",
                Host = "MockHost",
                Image = "MockImage",
                Port = 8000,
                SmtpUsername = "MockUsername",
                SmtpPassword = "MockPassword"
            });
    }

    private IOptions<TwilioSmsMessageOptions> GetMockTwilioOptions()
    {
        return new OptionsWrapper<TwilioSmsMessageOptions>(
            new TwilioSmsMessageOptions
            {
                AuthToken = "MockAuthToken",
                AccountSid = "MockAccountSid",
                ServiceId = "MockServiceId",
                TemplateId = "MockTemplateId",
                DefaultTemplateId = "MockDefaultTemplateId",
                Channel = "SMS",
                Locale = "EN"
                
                
            });
    }
}