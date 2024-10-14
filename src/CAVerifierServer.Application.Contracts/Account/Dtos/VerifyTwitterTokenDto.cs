using System;
using Orleans;

namespace CAVerifierServer.Account;

public class VerifyTwitterTokenDto : VerifierCodeDto
{
    public TwitterUserExtraInfo TwitterUserExtraInfo { get; set; }
}

[GenerateSerializer]
public class TwitterUserExtraInfo
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public string UserName { get; set; }
    [Id(3)] public bool Verified { get; set; }
    [Id(4)] public string GuardianType { get; set; }
    [Id(5)] public DateTime AuthTime { get; set; }
}