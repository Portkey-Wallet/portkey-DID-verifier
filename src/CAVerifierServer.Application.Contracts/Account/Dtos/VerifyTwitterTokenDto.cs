using System;

namespace CAVerifierServer.Account;

public class VerifyTwitterTokenDto : VerifierCodeDto
{
    public TwitterUserExtraInfo TwitterUserExtraInfo { get; set; }
}

public class TwitterUserExtraInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public bool Verified { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}