namespace CAVerifier.Monitor;

public enum MonitorTarget
{
    sendVerificationRequest,
    sendVerificationRequestFail,
    verifyCode,
    verifyCodeFail,
    verifyGoogleToken,
    verifyGoogleTokenFail,
    verifyAppleToken,
    verifyAppleTokenFail
}