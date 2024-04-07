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
    private const string UnSupportType = "InvalidateType";
    private const string DefaultEmail = "sam@xxxx.com";
    private const string DefaultCode = "123456";
    private const string InvalidateEmail = "123456789";

    public VerifierCodeSenderTest()
    {
        _verifyCodeSender = GetRequiredService<IEnumerable<IVerifyCodeSender>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockEmailSender());

    }

    [Fact]
    public void SendCodeByGuardianIdentifier_TypeTest()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
      
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
       
        var validatePhoneSuccess = emailVerifyCodeSender.ValidateGuardianIdentifier(DefaultEmail);
        validatePhoneSuccess.ShouldBe(true);
        var validatePhoneFail = emailVerifyCodeSender.ValidateGuardianIdentifier(InvalidateEmail);
        validatePhoneFail.ShouldBe(false);
    }
}