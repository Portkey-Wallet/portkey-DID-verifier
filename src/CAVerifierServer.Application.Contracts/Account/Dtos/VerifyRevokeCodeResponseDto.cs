using Orleans;

namespace CAVerifierServer.Account.Dtos;

[GenerateSerializer]
public class VerifyRevokeCodeResponseDto
{
    [Id(0)] public bool Success { get; set; }
}