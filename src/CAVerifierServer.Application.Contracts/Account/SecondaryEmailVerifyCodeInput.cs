using System;

namespace CAVerifierServer.Account;

public class SecondaryEmailVerifyCodeInput
{
    public string SecondaryEmail { get; set;}
    public Guid VerifierSessionId { get; set; }
    public string Code { get; set; }
}