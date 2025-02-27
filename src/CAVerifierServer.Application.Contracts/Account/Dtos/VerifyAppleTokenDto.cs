using System;
using CAVerifierServer.Account;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class VerifyAppleTokenDto : VerifierCodeDto
{
    [Id(0)] public AppleUserExtraInfo AppleUserExtraInfo { get; set; }
}

[GenerateSerializer]
public class AppleUserExtraInfo
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Email { get; set; }
    [Id(2)] public bool VerifiedEmail { get; set; }
    [Id(3)] public bool IsPrivateEmail { get; set; }
    [Id(4)] public string GuardianType { get; set; }
    [Id(5)] public DateTime AuthTime { get; set; }
}