using Volo.Abp.Sms;

namespace CAVerifierServer.VerifyCodeSender;

public interface ISMSServiceSender : ISmsSender
{
    string ServiceName { get; }
}