namespace CAVerifierServer.Grains;

public static class Error
{
    public const int Unsupported = 20001;
    public const int NullOrEmptyInput= 20002;
    public const int InvalidLoginGuardianIdentifier = 20003;
    public const int InvalidEmail = 20004;
    public const int TooManyRetries = 20005;
    public const int WrongCode = 20006;
    public const int Timeout = 20007;
    public const int Verified = 20008;
    public const int IdNotExist = 20009;
    public const string VerifyCodeErrorLogPrefix = "Verify code falied.Error:";
    public const string SendVerificationRequestErrorLogPrefix = "SendVerificationRequest falied. Error:";
    public const string VerifyAppleErrorLogPrefix = "Verify apple identity token falied.Error:";
    public const string VerifyTelegramErrorLogPrefix = "Verify Telegram identity token falied.Error:";
    public const string VerifyTwitterErrorLogPrefix = "Verify twitter identity token falied.Error:";
    public const string VerifyFacebookErrorLogPrefix = "Verify facebook identity token falied.Error:";
    public const int InvalidVerifierSessionId = 20010;
    public static readonly Dictionary<int, string> Message = new()
    {
        { Unsupported, "Unsupported Type" },
        { NullOrEmptyInput, "Input is null or empty" },
        { InvalidLoginGuardianIdentifier, "LoginGuardianIdentifier does not match the VerifierSessionId" },
        { InvalidEmail, "Invalid email input" },
        { TooManyRetries, "Too Many Retries" },
        { WrongCode, "Invalid code" },
        { Timeout, "Timeout" },
        { Verified, "Already Verified" },
        { IdNotExist,"There is no such entity" },
        { InvalidVerifierSessionId,"LoginGuardianIdentifier does not match the VerifierSessionId" }
    };
    
}