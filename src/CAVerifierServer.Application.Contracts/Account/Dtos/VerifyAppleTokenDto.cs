using System;
using CAVerifierServer.Account;

namespace CAVerifierServer.Account;

public class VerifyAppleTokenDto : VerifierCodeDto
{
    public AppleUserExtraInfo AppleUserExtraInfo { get; set; }
}

public class AppleUserExtraInfo
{
    public string Id { get; set; }
    public string Email { get; set; }
    public bool VerifiedEmail { get; set; }
    public bool IsPrivateEmail { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}