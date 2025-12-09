using System.Security.Claims;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Verifier.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
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
        
        // services.AddSingleton(GetJwtSecurityTokenHandlerMock());
        services.AddSingleton(MockTelegramAuthOptionsSnapshot());
        services.AddSingleton(MockJwtTokenOptionsSnapshot());
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

    [Fact]
    public async Task VerifyTelegramTokenAsync_test()
    {
        var grainDto = new VerifyTokenGrainDto
        {
            AccessToken =
                "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI2NjM3NzU1NDg2IiwidXNlck5hbWUiOiJVc2VyTmFtZSIsImF1dGhEYXRlIjoiMTcwMzQ3MzQ3MSIsImZpcnN0TmFtZSI6IkZpcnN0TmFtZSIsImxhc3ROYW1lIjoiTGFzdE5hbWUiLCJoYXNoIjoiOWJmNzVkMjg0MTMzM2FiYjcwYTQxNzI4OTkyMWNlZTI3YzU3NDRhMjE1OGU2MzQ1MTc2ZTM2OWQ0YzQ4MTA2NiIsInByb3RvVXJsIjoieHh4eC5qcGciLCJuYmYiOjE3MDM1ODk5NDYsImV4cCI6MTg2MTI2OTk0NiwiaXNzIjoiSXNzdWVyIiwiYXVkIjoiQXVkaWVuY2UifQ.VtLTVH1DriL05WZ5s05cwlMMJbaUAd0Uuq70JioAXlwr1dt9KYV3glRYUtdAuzqaPy7ib133Bvu7Rs2p-7wTz_1dimtL55PLdo2JT_B--Bzx9vRUAtTJNSJdpjJNQuuHYb0zfdOsrimbwdfuBCNmMvCGdiS_VscaQnKQRILn6zrk0-35X3-FQ5OuULaPnr_RbtxX4Z8KoJnSWrxG4TmNnG7FTaim6PsYwsIlMWAt0svxeKj7tJTuowMCUG1_cozaRsmN3T9ZX65t24tF2v-_oO-1EhGm_o-ez9pDxJWZtNTmQR0dalYnE5sMh96jBn7I501U27aXu50MxuRjdvJ-nw",
            IdentifierHash = null,
            Salt = null,
            OperationType = null,
            ChainId = null
        };
        
        var userId = Guid.NewGuid().ToString();
        var grain = Cluster.Client.GetGrain<IThirdPartyVerificationGrain>(userId);
    
        var result = await grain.VerifyTelegramTokenAsync(grainDto);
        result.Message.ShouldBe("Invalid token");
    }
}