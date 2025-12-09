using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAVerifierServer.MsgSender;

[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public partial class VerifierCodeSenderTest : CAVerifierServerApplicationTestBase
{
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSender;
    private const string EmailType = "Email";
    private const string PhoneType = "Phone";
    private const string UnSupportType = "InvalidateType";
    private const string DefaultEmail = "sam@xxxx.com";
    private const string DefaultCode = "123456";
    private const string InvalidateEmail = "123456789";
    private const string FakeOverSeaPhoneNum = "+651234567890";
    private const string FakePhoneNum = "+861234567890";
    private const string FakeFailedPhoneNum = "+861234567891";

    public VerifierCodeSenderTest()
    {
        _verifyCodeSender = GetRequiredService<IEnumerable<IVerifyCodeSender>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockEmailSender());
        services.AddSingleton(GetMockSmsSender());
        services.AddSingleton(GetMockSmsServiceSender());
        services.AddSingleton(GetMockSmsServiceOptions());
        services.AddSingleton(GetMockMobileCountryRegularCategoryOptions());
        services.AddSingleton(GetMockSMSTemplateOptions());
    }

    [Fact]
    public void SendCodeByGuardianIdentifier_TypeTest()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
        var phoneVerifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == PhoneType);
        phoneVerifierCodeSender.ShouldNotBe(null);
        phoneVerifierCodeSender.Type.ShouldBe("Phone");
        var verifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == UnSupportType);
        verifierCodeSender.ShouldBe(null);
    }

    [Fact]
    public async Task SendCodeByGuardianIdentifier_Test()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
        await emailVerifyCodeSender.SendCodeByGuardianIdentifierAsync(DefaultEmail, DefaultCode);

        var phoneVerifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == PhoneType);
        phoneVerifierCodeSender.ShouldNotBe(null);
        phoneVerifierCodeSender.Type.ShouldBe("Phone");
        await phoneVerifierCodeSender.SendCodeByGuardianIdentifierAsync(FakePhoneNum, DefaultCode);
        await phoneVerifierCodeSender.SendCodeByGuardianIdentifierAsync(FakeOverSeaPhoneNum, DefaultCode);
    }

    [Fact]
    public void SendCodeByGuardianIdentifier_ValidateGuardianIdentifier_Test()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
        var validateEmailSuccess = emailVerifyCodeSender.ValidateGuardianIdentifier(DefaultEmail);
        validateEmailSuccess.ShouldBe(true);
        var validateEmailFail = emailVerifyCodeSender.ValidateGuardianIdentifier(InvalidateEmail);
        validateEmailFail.ShouldBe(false);
        var phoneVerifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == PhoneType);
        phoneVerifierCodeSender.ShouldNotBe(null);
        phoneVerifierCodeSender.Type.ShouldBe("Phone");
        phoneVerifierCodeSender.ValidateGuardianIdentifier("+861234567890").ShouldBe(true);
        phoneVerifierCodeSender.ValidateGuardianIdentifier("").ShouldBe(false);
        var validatePhoneSuccess = emailVerifyCodeSender.ValidateGuardianIdentifier(DefaultEmail);
        validatePhoneSuccess.ShouldBe(true);
        var validatePhoneFail = emailVerifyCodeSender.ValidateGuardianIdentifier(InvalidateEmail);
        validatePhoneFail.ShouldBe(false);
    }
}