using System;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class VerifyGoogleTokenDto : VerifierCodeDto
{
    [Id(0)] public GoogleUserExtraInfo GoogleUserExtraInfo { get; set; }
}

[GenerateSerializer]
public class GoogleUserExtraInfo
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string FullName { get; set; }
    [Id(2)] public string FirstName { get; set; }
    [Id(3)] public string LastName { get; set; }
    [Id(4)] public string Email { get; set; }
    [Id(5)] public string Picture { get; set; }
    [Id(6)] public bool VerifiedEmail { get; set; }
    [Id(7)] public string GuardianType { get; set; }
    [Id(8)] public DateTime AuthTime { get; set; }
}

public enum GuardianIdentifierType
{
    Email = 0,
    Phone = 1,
    Google = 2,
    Apple = 3,
    Telegram = 4,
    Facebook = 5,
    Twitter = 6
}