using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAVerifierServer.Account;

/* This is just an example test class.
 * Normally, you don't test code of the modules you are using
 * (like IIdentityUserAppService here).
 * Only test your own application services.
 */
[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public partial class AccountAppServiceTests : CAVerifierServerApplicationTestBase
{
    private const string DefaultEmailAddress = "sam@XXXXX.com";
    private readonly IAccountAppService _accountAppService;
    private const string DefaultType = "Email";
    private const string Code = "123456";
    private const string InvalidType = "ErrorType";
    private const string InvalidEmail = "1234567";
    private const string LocalIpaddress = "127.0.0.1";
    private const string DefaultToken = "MockToken";

    public AccountAppServiceTests()
    {
        _accountAppService = GetRequiredService<IAccountAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockEmailSender());
        services.AddSingleton(GetMockchainInfoOptions());
        services.AddSingleton(GetMockContractsProvider());
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(GetMockThirdPartyVerificationGrain());
        services.AddSingleton(GetMockClusterClient());
        services.AddSingleton(GetMockGuardianIdentifierVerificationGrain());
        services.AddSingleton(GetAppleAuthOptions());
        services.AddSingleton(GetAppleKeys());
    }


    [Fact]
    public async Task SendVerificationRequest_Success_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        //success
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var randomCode = RandomNumProvider.GetCode(6);
        randomCode.Length.ShouldBe(6);
        result.Success.ShouldBe(true);
        result.Data.VerifierSessionId.ShouldBe(verifierSessionId);
    }

    [Fact]
    public async Task SendVerificationRequest_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        //success
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = "Phone",
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var randomCode = RandomNumProvider.GetCode(6);
        randomCode.Length.ShouldBe(6);
        result.Success.ShouldBe(false);
        result.Message.ShouldBe("MockFalseMessage");
    }

    [Fact]
    public async Task SendVerificationRequest_OverFrequencyLimit_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        //success
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var randomCode = RandomNumProvider.GetCode(6);
        randomCode.Length.ShouldBe(6);
        result.Success.ShouldBe(true);
        result.Data.VerifierSessionId.ShouldBe(verifierSessionId);
    }


    [Fact]
    public async Task SendVerificationRequest_UnsupportedType_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = InvalidType,
            VerifierSessionId = Guid.NewGuid(),
            GuardianIdentifier = DefaultEmailAddress
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.Unsupported]);
    }

    [Fact]
    public async Task SendVerificationRequest_Register_InvalidEmailInput_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = InvalidEmail,
            VerifierSessionId = Guid.NewGuid(),
            Type = DefaultType
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.InvalidGuardianIdentifierInput]);
    }

    [Fact]
    public async Task SendVerificationRequest_Register_InvalidPhoneInput_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = "",
            VerifierSessionId = Guid.NewGuid(),
            Type = "Phone"
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.InvalidGuardianIdentifierInput]);
    }


    [Fact]
    public async Task SendVerificationRequest_InputIsNullOrEmpty_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            VerifierSessionId = Guid.NewGuid(),
            Type = DefaultType
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.InvalidGuardianIdentifierInput]);

        var resultDto = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Type = DefaultType
        });
        resultDto.Success.ShouldBe(true);
        resultDto.Data.VerifierSessionId.ShouldBe(Guid.Empty);

        var dto = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid()
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.Unsupported]);
    }


    [Fact]
    public async Task VerifyCode_VerifierSessionIdNotExist_Test()
    {
        var id = await SendVerificationRequest();
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var result = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid(),
            Code = Code,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe("Input is null or empty");
    }


    [Fact]
    public async Task VerifyCode_InputIsNullOrEmpty_Test()
    {
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var id = await SendVerificationRequest();
        var dto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            VerifierSessionId = id,
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var resultDto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var responseResultDto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        responseResultDto.Success.ShouldBe(false);
        responseResultDto.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var result = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Code = Code,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var responseResult = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Code = Code,
            Salt = salt
        });
        responseResult.Success.ShouldBe(false);
        responseResult.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);
    }

    [Fact]
    public async Task VerifyCodeSuccess_Test()
    {
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var id = await SendVerificationRequest();
        var dto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            VerifierSessionId = id,
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash,
            GuardianIdentifier = DefaultEmailAddress,
            OperationType = "1"
        });
        dto.Success.ShouldBe(true);
    }


    private async Task<Guid> SendVerificationRequest()
    {
        var verifierSessionId = Guid.NewGuid();
        await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            VerifierSessionId = verifierSessionId,
            GuardianIdentifier = DefaultEmailAddress,
        });
        return verifierSessionId;
    }

    [Fact]
    private async Task WhiteList_Test()
    {
        var ipList = new List<string>();
        ipList.Add(LocalIpaddress);
        var result = await _accountAppService.WhiteListCheckAsync(ipList);
        result.ShouldBe(LocalIpaddress);
    }

    [Fact]
    public async Task VerifyGoogleToken_Test()
    {
        var verifierRequest = new VerifyTokenRequestDto
        {
            IdentifierHash = HashHelper.ComputeFrom("salt" + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex())
                .ToHex(),
            AccessToken = DefaultToken,
            Salt = "salt",
            OperationType = "1"
        };
        var response = await _accountAppService.VerifyGoogleTokenAsync(verifierRequest);
        response.Success.ShouldBe(true);

        var request = new VerifyTokenRequestDto
        {
            IdentifierHash = HashHelper.ComputeFrom("salt" + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex())
                .ToHex(),
            AccessToken = "mockToken",
            Salt = "salt",
            OperationType = "1"
        };
        var responseResult = await _accountAppService.VerifyGoogleTokenAsync(request);
        responseResult.Success.ShouldBe(false);
        responseResult.Message.ShouldBe("MockFalseMessage");
    }

    [Fact]
    public async Task VerifyAppleId_Test()
    {
        var request = new VerifyTokenRequestDto
        {
            IdentifierHash = HashHelper.ComputeFrom("salt" + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex())
                .ToHex(),
            AccessToken = DefaultToken,
            Salt = "salt",
            OperationType = "1"
        };
        var response = await _accountAppService.VerifyAppleTokenAsync(request);
        response.Success.ShouldBe(true);

        var input = new VerifyTokenRequestDto
        {
            IdentifierHash = HashHelper.ComputeFrom("salt" + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex())
                .ToHex(),
            AccessToken = "mockToken",
            Salt = "salt",
            OperationType = "1"
        };
        var responseResult = await _accountAppService.VerifyAppleTokenAsync(input);
        responseResult.Success.ShouldBe(false);
        responseResult.Message.ShouldBe("MockFalseMessage");
    }

    [Fact]
    public async Task VerifyTelegramTokenAsync_Test()
    {
        var request = new VerifyTokenRequestDto
        {
            IdentifierHash = HashHelper.ComputeFrom("salt" + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex())
                .ToHex(),
            AccessToken = DefaultToken,
            Salt = "salt",
            OperationType = "1"
        };
        var response = await _accountAppService.VerifyAppleTokenAsync(request);
        response.Success.ShouldBe(true);

        var input = new VerifyTokenRequestDto
        {
            IdentifierHash = HashHelper.ComputeFrom("salt" + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex())
                .ToHex(),
            AccessToken = "ErrorToken",
            Salt = "salt",
            OperationType = "1"
        };
        var responseResult = await _accountAppService.VerifyAppleTokenAsync(input);
        responseResult.Success.ShouldBe(false);
        responseResult.Message.ShouldBe("MockFalseMessage");
    }
}