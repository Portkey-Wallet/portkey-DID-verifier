using System;

namespace CAVerifierServer.Account;

public class VerifyGoogleTokenDto : VerifierCodeDto
{
    public GoogleUserExtraInfo GoogleUserExtraInfo { get; set; }
}

public class GoogleUserExtraInfo
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Picture { get; set; }
    public bool VerifiedEmail { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
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