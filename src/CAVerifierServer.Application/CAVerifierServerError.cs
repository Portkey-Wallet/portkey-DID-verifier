using System.Collections.Generic;

namespace CAVerifierServer;

public static class Error
{
    public const int Unsupported = 20001;
    public const int NullOrEmptyInput= 20002;
    public const int InvalidLoginGuardianIdentifier = 20003;
    public const int InvalidGuardianIdentifierInput = 20004;
    public const int TooManyRetries = 20005;
    public const int WrongCode = 20006;
    public const int Timeout = 20007;
    public const int Verified = 20008;
    public const int IdNotExist = 20009;
    public const string VerifyCodeErrorLogPrefix = "Verify code falied.Error:";
    public const string SendVerificationRequestErrorLogPrefix = "SendVerificationRequest falied. Error:";
    public const string VerifyAppleErrorLogPrefix = "Verify apple identity token falied.Error:";
    public static readonly Dictionary<int, string> Message = new()
    {
        { Unsupported, "Unsupported Type" },
        { NullOrEmptyInput, "Input is null or empty" },
        { InvalidLoginGuardianIdentifier, "LoginGuardianIdentifier does not match the VerifierSessionId" },
        { InvalidGuardianIdentifierInput, "Invalid GuardianIdentifier input" },
        { TooManyRetries, "Too many retries. Please request a new verification code to continue." },
        { WrongCode, "Incorrect code, please try again." },
        { Timeout, "Verification code expired. Please request a new one to continue." },
        { Verified, "Already Verified" },
        { IdNotExist,"There is no such entity" }
    };
}