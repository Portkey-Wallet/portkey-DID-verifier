using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Application;
using CAVerifierServer.Contracts;
using CAVerifierServer.Grains.Dto;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Options;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Volo.Abp.Emailing;

namespace CAVerifierServer.Account;

public partial class AccountAppServiceTests
{
    private IEmailSender GetMockEmailSender()
    {
        var mockEmailSender = new Mock<IEmailSender>();
        mockEmailSender.Setup(o => o.QueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AdditionalEmailSendingArgs>())).Returns(Task.CompletedTask);
        return mockEmailSender.Object;
    }

    private IOptions<ChainOptions> GetMockchainInfoOptions()
    {
        var chainInfoDic = new Dictionary<string, ChainInfo>();
        chainInfoDic.Add("AELF", new ChainInfo
        {
            ChainId = "MockChain",
            BaseUrl = "http://localhost:8000",
            ContractAddress = "",
            IsMainChain = false,
            PrivateKey = ""
        });
        return new OptionsWrapper<ChainOptions>(
            new ChainOptions
            {
                ChainInfos = chainInfoDic
            });
    }

    private IContractsProvider GetMockContractsProvider()
    {
        var mockContractsProvider = new Mock<IContractsProvider>();
        mockContractsProvider.Setup(o => o.GetCaServersListAsync(It.IsAny<ChainInfo>())).ReturnsAsync(
            new GetCAServersOutput
            {
                CaServers =
                {
                    new CAServer
                    {
                        Name = "MockServer",
                        EndPoint = "Http://127.0.0.1:8000"
                    }
                }
            });
        return mockContractsProvider.Object;
    }

    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;

        return factory;
    }

    private IThirdPartyVerificationGrain GetMockThirdPartyVerificationGrain()
    {
        var mockThirdPartyVerificationGrain = new Mock<IThirdPartyVerificationGrain>();
        mockThirdPartyVerificationGrain.Setup(o => o.VerifyGoogleTokenAsync(It.IsAny<VerifyTokenGrainDto>()))
            .ReturnsAsync((VerifyTokenGrainDto dto) =>
            {
                if (dto.AccessToken == DefaultToken)
                {
                    return new GrainResultDto<VerifyGoogleTokenGrainDto>
                    {
                        Message = "MockSuccessMessage",
                        Success = true
                    };
                }

                return new GrainResultDto<VerifyGoogleTokenGrainDto>()
                {
                    Message = "MockFalseMessage",
                    Success = false
                };
            });

        mockThirdPartyVerificationGrain.Setup(o => o.VerifyAppleTokenAsync(It.IsAny<VerifyTokenGrainDto>()))
            .ReturnsAsync((VerifyTokenGrainDto dto) =>
            {
                if (dto.AccessToken == DefaultToken)
                {
                    return new GrainResultDto<VerifyAppleTokenGrainDto>
                    {
                        Message = "MockSuccessMessage",
                        Success = true
                    };
                }

                return new GrainResultDto<VerifyAppleTokenGrainDto>
                {
                    Message = "MockFalseMessage",
                    Success = false
                };
            });

        mockThirdPartyVerificationGrain.Setup(o => o.VerifyTelegramTokenAsync(It.IsAny<VerifyTokenGrainDto>()))
            .ReturnsAsync((VerifyTokenGrainDto dto) =>
            {
                if (dto.AccessToken == DefaultToken)
                {
                    return new GrainResultDto<VerifyTelegramTokenGrainDto>
                    {
                        Message = "MockSuccessMessage",
                        Success = true
                    };
                }

                return new GrainResultDto<VerifyTelegramTokenGrainDto>
                {
                    Message = "MockFalseMessage",
                    Success = false
                };
            });
        
        mockThirdPartyVerificationGrain.Setup(o=>o.GetUserInfoFromGoogleAsync(It.IsAny<string>())).ReturnsAsync(new GoogleUserInfoDto()
        {
        });
        
        mockThirdPartyVerificationGrain.Setup(o=>o.GetTwitterUserInfoAsync(It.IsAny<string>())).ReturnsAsync(new TwitterUserInfo()
        {
        });
        
        
        
        return mockThirdPartyVerificationGrain.Object;
    }


    private IClusterClient GetMockClusterClient()
    {
        var mockClusterClient = new Mock<IClusterClient>();
        mockClusterClient.Setup(o => o.GetGrain<IGrainWithStringKey>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string primaryKey, string namePrefix) =>
            {
                if (primaryKey == DefaultEmailAddress)
                {
                    return GetMockGuardianIdentifierVerificationGrain();
                }

                return GetMockThirdPartyVerificationGrain();
            });
        return mockClusterClient.Object;
    }

    private IGuardianIdentifierVerificationGrain GetMockGuardianIdentifierVerificationGrain()
    {
        var mockGuardianIdentifierVerificationGrain = new Mock<IGuardianIdentifierVerificationGrain>();
        mockGuardianIdentifierVerificationGrain
            .Setup(o => o.GetVerifyCodeAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync((SendVerificationRequestInput dto) =>
            {
                if (dto.Type == DefaultType)
                {
                    return new GrainResultDto<VerifyCodeDto>
                    {
                        Message = "MockSuccessMessage",
                        Success = true,
                        Data = new VerifyCodeDto
                        {
                            VerifierCode = Code
                        }
                    };
                }

                return new GrainResultDto<VerifyCodeDto>
                {
                    Message = "MockFalseMessage",
                    Success = false
                };
            });

        mockGuardianIdentifierVerificationGrain.Setup(o => o.VerifyAndCreateSignatureAsync(It.IsAny<VerifyCodeInput>()))
            .ReturnsAsync((VerifyCodeInput input) =>
            {
                if (input.GuardianIdentifier == DefaultEmailAddress)
                {
                    return new GrainResultDto<UpdateVerifierSignatureDto>
                    {
                        Success = true,
                        Data = new UpdateVerifierSignatureDto
                        {
                            Data = "MockData",
                            Signature = "MockSignature"
                        }
                    };
                }

                return new GrainResultDto<UpdateVerifierSignatureDto>
                {
                    Message = "MockFalseMessage",
                    Success = false
                };
            });

        mockGuardianIdentifierVerificationGrain.Setup(o => o.VerifyRevokeCodeAsync(It.IsAny<VerifyRevokeCodeDto>()))
            .ReturnsAsync((VerifyRevokeCodeDto dto) =>
                {
                    if (dto.VerifyCode == DefaultToken)
                    {
                        return new GrainResultDto<VerifyRevokeCodeResponseDto>
                        {
                            Success = true
                        };
                        
                    }

                    return new GrainResultDto<VerifyRevokeCodeResponseDto>
                    {
                        Success = false
                    };
                }
                
            );


        return mockGuardianIdentifierVerificationGrain.Object;
    }


    public class DelegatingHandlerStub : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

        public DelegatingHandlerStub()
        {
            _handlerFunc = (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        }

        public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            _handlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handlerFunc(request, cancellationToken);
        }
    }

    private IOptions<AppleAuthOptions> GetAppleAuthOptions()
    {
        var list = new List<string>();
        list.Add("MockAppleId");
        return new OptionsWrapper<AppleAuthOptions>(
            new AppleAuthOptions
            {
                Audiences = list,
                KeysExpireTime = 1
            });
    }

    private IOptions<AppleKeys> GetAppleKeys()
    {
        var list = new List<AppleKey>();
        var key = new AppleKey
        {
            Kid = "kid",
            Kty = "kty",
            Use = "use",
            N = "n",
            E = "e",
            Alg = "alg"
        };
        list.Add(key);
        return new OptionsWrapper<AppleKeys>(
            new AppleKeys
            {
                Keys = list
            });
    }
}