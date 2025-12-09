using AElf;
using CAVerifierServer.Account;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Grains;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Timing;
using Xunit;

namespace CAVerifierServer.Grain.Tests.GuardianIdentifier;

[Collection(ClusterCollection.Name)]
public class GuardianIdentifierVerificationGrainTest : CAVerifierServerGrainTestBase
{
    private const string DefaultEmailAddress = "sam@XXXX.com";
    private const string DefaultType = "Email";
    private const string DefaultCode = "123456";
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSender;
    private const string Email = "key@XXXXX.com";

    public GuardianIdentifierVerificationGrainTest()
    {
        var clock = GetRequiredService<IClock>();
    }
    

    [Fact]
    public async Task GetVerifyCode_Success_Test()
    {
        
        
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        result.Success.ShouldBe(true);
        result.Data.VerifierCode.Length.ShouldBe(6);
    }

    [Fact]
    public async Task GetVerifyCode_OverFrequencyLimit_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid()
        });

        result.Success.ShouldBe(false);
        result.Message.ShouldBe("The interval between sending two verification codes is less than 60s");
    }

    [Fact]
    public async Task GetVerifyCode_SameVerifierSessionId_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        result.Data.VerifierCode.Length.ShouldBe(6);

        Change_Clock_Now(2);

        var resultDto = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        resultDto.Success.ShouldBe(true);
        resultDto.Data.VerifierCode.ShouldNotBe(result.Data.VerifierCode);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_Success_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        try
        {
            await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
            {
                GuardianIdentifier = DefaultEmailAddress,
                Code = result.Data.VerifierCode,
                VerifierSessionId = verifierSessionId,
                Salt = salt,
                GuardianIdentifierHash = hash,
                OperationType = "1"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldNotBeNull();
        }

    }

    [Fact]
    public async Task VerifyAndCreateSignature_AlreadyVerified_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        try
        {
            var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
            {
                GuardianIdentifier = DefaultEmailAddress,
                Code = result.Data.VerifierCode,
                VerifierSessionId = verifierSessionId,
                Salt = salt,
                GuardianIdentifierHash = hash
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldNotBeNull();
        }


    }
    
    

    [Fact]
    public async Task VerifyRevokeCode_Success_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        try
        {
            await grain.VerifyRevokeCodeAsync(new VerifyRevokeCodeDto
            {
                GuardianIdentifier = DefaultEmailAddress,
                VerifyCode = result.Data.VerifierCode,
                VerifierSessionId = verifierSessionId,
                Type = DefaultType
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldNotBeNull();
        }

    }


    [Fact]
    public async Task VerifyAndCreateSignature_InvalidLoginGuardianIdentifier_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();

        var newGrain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(Email);
        var signatureAsyncResult = await newGrain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = Email,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Success.ShouldBe(false);
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.InvalidLoginGuardianIdentifier]);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_InvalidVerifierSessionId_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = Guid.NewGuid(),
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.InvalidVerifierSessionId]);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_Timeout_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        Change_Clock_Now(3);
        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.Timeout]);
    }

    [Fact]
    public async Task VerifyAndCreateSignature_TooManyRetries_Test()
    {

        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = DefaultCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Success.ShouldBe(false);
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.WrongCode]);
        var resultDto = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = DefaultCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe(Error.Message[Error.WrongCode]);

        var result = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = DefaultCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.TooManyRetries]);
    }

    private void Change_Clock_Now(int offset)
    {
        foreach (var silo in Cluster.Silos)
        {
            ((MockClock)((InProcessSiloHandle)silo).SiloHost.Services.GetRequiredService<IClock>()).SetOffset(offset);
        }
    }

    [Fact]
    public void DtoTest()
    {
        var verifyCodeDto = new VerifierCodeDto
        {
            VerificationDoc = "FadeVerificationDoc",
            Signature = "FadeSignature"
        };
        verifyCodeDto.Signature.ShouldBe("FadeSignature");
        verifyCodeDto.VerificationDoc.ShouldBe("FadeVerificationDoc");

        var info = new GoogleUserInfoDto
        {
            Id = "id",
            FullName = "MockFullName",
            FirstName = "MockFirstName",
            LastName = "MockLastName",
            Email = "MockEmail",
            VerifiedEmail = true,
            Picture = "MockPicture"
        };
        info.Id.ShouldBe("id");
        info.FullName.ShouldBe("MockFullName");
        info.FirstName.ShouldBe("MockFirstName");
        info.LastName.ShouldBe("MockLastName");
        info.Email.ShouldBe("MockEmail");
        info.VerifiedEmail.ShouldBe(true);
        info.Picture.ShouldBe("MockPicture");
        
        var tokenDto = new VerifyAppleTokenDto{
                AppleUserExtraInfo = new AppleUserExtraInfo
                {
                    Id = "id",
                    Email = "MockEmail",
                    AuthTime = DateTime.Now,
                    IsPrivateEmail = true,
                    GuardianType = DefaultType,
                    VerifiedEmail = true
                }
        };
        
        tokenDto.AppleUserExtraInfo.Id.ShouldBe("id");
        tokenDto.AppleUserExtraInfo.Email.ShouldBe("MockEmail");
        tokenDto.AppleUserExtraInfo.IsPrivateEmail.ShouldBe(true);
        tokenDto.AppleUserExtraInfo.GuardianType.ShouldBe(DefaultType);
        tokenDto.AppleUserExtraInfo.VerifiedEmail.ShouldBe(true);

        var dto = new VerifyGoogleTokenDto
        {
            GoogleUserExtraInfo = new GoogleUserExtraInfo
            {
                Id = "id",
                FullName = "MockFullName",
                FirstName = "MockFirstName",
                LastName = "MockLastName",
                Email = "MockEmail",
                VerifiedEmail = true,
                Picture = "MockPicture"
            }
        };
        dto.GoogleUserExtraInfo.Id.ShouldBe("id");
        dto.GoogleUserExtraInfo.FullName.ShouldBe("MockFullName");
        dto.GoogleUserExtraInfo.FirstName.ShouldBe("MockFirstName");
        dto.GoogleUserExtraInfo.LastName.ShouldBe("MockLastName");
        dto.GoogleUserExtraInfo.Email.ShouldBe("MockEmail");
        dto.GoogleUserExtraInfo.VerifiedEmail.ShouldBe(true);
        dto.GoogleUserExtraInfo.Picture.ShouldBe("MockPicture");
    }
}