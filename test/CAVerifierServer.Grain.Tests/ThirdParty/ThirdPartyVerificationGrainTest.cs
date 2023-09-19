using System.Security.Claims;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CAVerifierServer.Grain.Tests.ThirdParty;

[Collection(ClusterCollection.Name)]
public partial class ThirdPartyVerificationGrainTest : CAVerifierServerGrainTestBase
{

    protected override void AfterAddApplication(IServiceCollection services)
    {
        var clientFactory = Substitute.For<IHttpClientFactory>();
        services.AddSingleton(clientFactory);

      //  services.AddSingleton(GetJwtSecurityTokenHandlerMock());
    }

    [Fact]
    public async Task VerifyGoogleTokenTest()
    {
        var userId = Guid.NewGuid().ToString();
        var grain = Cluster.Client.GetGrain<IThirdPartyVerificationGrain>(userId);
        try
        {
            var result = await grain.VerifyGoogleTokenAsync(new VerifyTokenGrainDto());
        }
        catch (Exception e)
        {
            e.Message.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task VerifyAppleTokenTest()
    {
        var userId = Guid.NewGuid().ToString();
        var grain = Cluster.Client.GetGrain<IThirdPartyVerificationGrain>(userId);
        
        var token = MockJwtTokens.GenerateJwtToken(new Claim[]
        {
            new(ClaimTypes.NameIdentifier, "123456"),
        });
        var result = await grain.VerifyAppleTokenAsync(new VerifyTokenGrainDto()
        { 
            AccessToken =
                "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg"
        });
        result.Message.ShouldNotBeNull();
    }
}