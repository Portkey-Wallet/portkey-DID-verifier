using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.CustomException;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Sms;

namespace CAVerifierServer.MsgSender;

public partial class VerifierCodeSmsSenderTests
{
    private IOptionsSnapshot<SmsServiceOptions> GetMockSmsServiceOptions()
    {
        var smsMockServiceOptions = new Mock<IOptionsSnapshot<SmsServiceOptions>>();
        var smsServiceDic = new Dictionary<string, SmsServiceOption>();
        smsServiceDic.Add("AWS", new SmsServiceOption
        {
            SupportingCountriesRatio = new Dictionary<string, int>
            {
                { "CN", 4 }
            }
        });

        smsServiceDic.Add("TeleSign", new SmsServiceOption
        {
            SupportingCountriesRatio = new Dictionary<string, int>
            {
                { "CN", 3 }
            }
        });
        smsServiceDic.Add("Twilio", new SmsServiceOption
        {
            SupportingCountriesRatio = new Dictionary<string, int>
            {
                { "CN", 2 }
            }
        });
        smsServiceDic.Add("MockSmsServiceSender", new SmsServiceOption
        {
            SupportingCountriesRatio = new Dictionary<string, int>
            {
                { "CN", 1 }
            },
        });
        smsMockServiceOptions.Setup(o => o.Value).Returns(
            new SmsServiceOptions
            {
                SmsServiceInfos = smsServiceDic
            });
        return smsMockServiceOptions.Object;
    }

    private ISMSServiceSender GetMockSmsServiceSender()
    {
        var mockSmsSender = new Mock<ISMSServiceSender>();
        mockSmsSender.Setup(o => o.ServiceName).Returns("MockSmsServiceSender");
        mockSmsSender.Setup(o => o.SendAsync(It.IsAny<SmsMessage>()))
            .Returns(Task.CompletedTask);
        return mockSmsSender.Object;
    }


    private IOptionsSnapshot<MobileCountryRegularCategoryOptions> GetMockMobileCountryRegularCategoryOptions()
    {
        var mockMobileCountryRegularCategoryOptions = new Mock<IOptionsSnapshot<MobileCountryRegularCategoryOptions>>();
        var list = new List<MobileInfo>
        {
            new MobileInfo
            {
                CountryCode = "+86",
                Country = "CN",
                MobileRegular = "^\\+86-?1[3456789]\\d{9}$"
            }
        };
        mockMobileCountryRegularCategoryOptions.Setup(o => o.Value).Returns(
            new MobileCountryRegularCategoryOptions
            {
                MobileInfos = list
            });
        return mockMobileCountryRegularCategoryOptions.Object;
    }

    private IOptionsSnapshot<SMSTemplateOptions> GetMockSMSTemplateOptions()
    {
        var mockTemplate = new Mock<IOptionsSnapshot<SMSTemplateOptions>>();
        mockTemplate.Setup(o => o.Value).Returns(
            new SMSTemplateOptions
            {
                Template =
                    "[{0}] PORTKEY Verification Code: {1}. This verification code will expire in 10 minutes. If you did not request this message, please ignore it."
            });
        return mockTemplate.Object;
    }
}