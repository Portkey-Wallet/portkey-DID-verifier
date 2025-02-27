

using Orleans;

namespace CAVerifierServer.Account;
[GenerateSerializer]
public class VerifyCodeDto
{
    [Id(0)] public string VerifierCode { get; set; }
}