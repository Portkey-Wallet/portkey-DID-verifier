using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace CAVerifierServer.Grain.Tests.ThirdParty;

public class JwtMock
{
    public static JwtSecurityTokenHandler GetJwtSecurityTokenHandlerMock()
    {
        var jwtSecurityTokenHandler = new Mock<JwtSecurityTokenHandler>();
        SecurityToken token = new JwtSecurityToken
            { Payload = { { "email_verified", "true" }, { "is_private_email", "false" } } };
        jwtSecurityTokenHandler.Setup(p => p.ValidateToken(It.IsAny<string>(),
                It.IsAny<TokenValidationParameters>(),
                out token))
            .Returns(SelectClaimsPrincipal());
        jwtSecurityTokenHandler.Setup(p => p.MaximumTokenSizeInBytes).Returns(1000000);
        jwtSecurityTokenHandler.Setup(p => p.CanReadToken(It.IsAny<string>())).Returns(true);

        return jwtSecurityTokenHandler.Object;
    }


    private static ClaimsPrincipal SelectClaimsPrincipal()
    {
        IPrincipal currentPrincipal = Thread.CurrentPrincipal;
        return currentPrincipal is ClaimsPrincipal claimsPrincipal
            ? claimsPrincipal
            : (currentPrincipal == null ? (ClaimsPrincipal)null : new ClaimsPrincipal(currentPrincipal));
    }
}

public static class MockJwtTokens
{
    public static string Issuer { get; } = Guid.NewGuid().ToString();
    public static SecurityKey SecurityKey { get; }
    public static SigningCredentials SigningCredentials { get; }

    private static readonly JwtSecurityTokenHandler s_tokenHandler = new JwtSecurityTokenHandler();
    private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
    private static readonly byte[] s_key = new byte[32];

    static MockJwtTokens()
    {
        s_rng.GetBytes(s_key);
        SecurityKey = new SymmetricSecurityKey(s_key) { KeyId = Guid.NewGuid().ToString() };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
    }

    public static string GenerateJwtToken(IEnumerable<Claim> claims)
    {
        return s_tokenHandler.WriteToken(new JwtSecurityToken(Issuer, null, claims, null,
            DateTime.UtcNow.AddMinutes(20), SigningCredentials));
    }
}

public partial class ThirdPartyVerificationGrainTest
{
    private IOptionsSnapshot<TelegramAuthOptions> MockTelegramAuthOptionsSnapshot()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<TelegramAuthOptions>>();

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new TelegramAuthOptions
            {
                BaseUrl = "",
                Timeout = 10
            });
        return mockOptionsSnapshot.Object;
    }

    private IOptionsSnapshot<JwtTokenOptions> MockJwtTokenOptionsSnapshot()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<JwtTokenOptions>>();

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new JwtTokenOptions
            {
                Issuer = "Issuer",
                Audiences = new List<string>() { "Audience" },
                // Expire = 3600 * 24 * 365 * 5
            });
        return mockOptionsSnapshot.Object;
    }
}