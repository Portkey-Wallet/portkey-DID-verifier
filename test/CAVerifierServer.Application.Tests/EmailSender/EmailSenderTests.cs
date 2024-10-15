using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAVerifierServer.EmailSender;

[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public partial class EmailSenderTests : CAVerifierServerApplicationTestBase
{
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSender;
    private const string EmailType = "Email";
    private const string DefaultGuardianIdentifier = "sam@XXXX.com";
    private const string DefaultCode = "123456";


    public EmailSenderTests()
    {
        _verifyCodeSender = GetRequiredService<IEnumerable<IVerifyCodeSender>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetAwsEmailOptions());
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task EmailSenderTest()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");
        await emailVerifyCodeSender.SendCodeByGuardianIdentifierAsync(DefaultGuardianIdentifier, DefaultCode, "");
    }

    [Fact]
    public void ValidateGuardianIdentifierTest()
    {
        var emailVerifyCodeSender = _verifyCodeSender.FirstOrDefault(v => v.Type == EmailType);
        emailVerifyCodeSender.ShouldNotBe(null);
        emailVerifyCodeSender.Type.ShouldBe("Email");

        var result = emailVerifyCodeSender.ValidateGuardianIdentifier(DefaultGuardianIdentifier);
        result.ShouldBeTrue();
    }
}