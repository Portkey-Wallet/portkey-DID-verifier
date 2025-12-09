using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAVerifierServer.MsgSender;

[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public partial class VerifierCodeSmsSenderTests : CAVerifierServerApplicationTestBase
{
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSender;

    public VerifierCodeSmsSenderTests()
    {
        _verifyCodeSender = GetRequiredService<IEnumerable<IVerifyCodeSender>>();
    }

    private const string EmailType = "Email";
    private const string PhoneType = "Phone";
    private const string UnSupportType = "InvalidateType";
    private const string DefaultCode = "123456";
    private const string FakePhoneNum = "+8613456789012";

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockSmsServiceSender());
        services.AddSingleton(GetMockMobileCountryRegularCategoryOptions());
        services.AddSingleton(GetMockSmsServiceOptions());
        services.AddSingleton(GetMockSMSTemplateOptions());
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task SendCodeByGuardianIdentifier_TypeTest()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
        var phoneVerifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == PhoneType);
        phoneVerifierCodeSender.ShouldNotBe(null);
        phoneVerifierCodeSender.Type.ShouldBe("Phone");
        var verifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == UnSupportType);
        verifierCodeSender.ShouldBe(null);
        await phoneVerifierCodeSender.SendCodeByGuardianIdentifierAsync(FakePhoneNum, DefaultCode);
    }
    
    [Fact]
    public async Task SendCodeByGuardianIdentifier_InvalidateParam()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
        var phoneVerifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == PhoneType);
        phoneVerifierCodeSender.ShouldNotBe(null);
        phoneVerifierCodeSender.Type.ShouldBe("Phone");
        var verifierCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == UnSupportType);
        verifierCodeSender.ShouldBe(null);
        await phoneVerifierCodeSender.SendCodeByGuardianIdentifierAsync("", DefaultCode);
    }
}