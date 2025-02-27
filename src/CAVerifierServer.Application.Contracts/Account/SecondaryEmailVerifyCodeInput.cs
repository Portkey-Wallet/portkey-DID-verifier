using System;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class SecondaryEmailVerifyCodeInput
{
    [Id(0)] public string SecondaryEmail { get; set;}
    [Id(1)] public Guid VerifierSessionId { get; set; }
    [Id(2)] public string Code { get; set; }
}