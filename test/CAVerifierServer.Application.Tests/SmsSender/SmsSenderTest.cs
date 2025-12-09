using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Sms;
using Xunit;

namespace CAVerifierServer.SmsSender;

[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public partial class SmsSenderTest : CAVerifierServerApplicationTestBase
{
    private readonly IEnumerable<ISMSServiceSender> smsServiceSender;
    private const string AWS = "AWS";
    private const string Telesign = "Telesign";
    private const string Twilio = "Twilio";
    private const string UnSupportType = "InvalidateType";
    private const string FakePhoneNum = "+8613545678901";
    private const string FakeOverseaPhoneNum = "+12025550100";
    private const string FakeCode = "123456";


    public SmsSenderTest()
    {
        smsServiceSender = GetRequiredService<IEnumerable<ISMSServiceSender>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetAwsEmailOptions());
        services.AddSingleton(GetSmsTemplateOptions());
        services.AddSingleton(GetMockSmsServiceSender());
    }

    [Fact]
    public async Task SendCodeByGuardianIdentifier_TypeTest()
    {
        var awsSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == AWS);
        awsSmsSender.ServiceName.ShouldBe(AWS);
        var telesignSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == Telesign);
        telesignSmsSender.ServiceName.ShouldBe(Telesign);
        var twilioSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == Twilio);
        twilioSmsSender.ServiceName.ShouldBe(Twilio);
        var unSupportSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == UnSupportType);
        unSupportSmsSender.ShouldBeNull();
        var smsMessage = new SmsMessage(FakePhoneNum, FakeCode);
        var overseaSmsMessage = new SmsMessage(FakeOverseaPhoneNum, FakeCode);
        try
        {
            await telesignSmsSender.SendAsync(smsMessage);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }

        try
        {
            await awsSmsSender.SendAsync(smsMessage);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }

        try
        {
            await twilioSmsSender.SendAsync(smsMessage);
            
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }

        try
        {
            await twilioSmsSender.SendAsync(overseaSmsMessage);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
}