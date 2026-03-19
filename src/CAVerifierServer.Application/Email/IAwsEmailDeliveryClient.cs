using System.Net.Mail;
using System.Threading.Tasks;
using CAVerifierServer.Options;

namespace CAVerifierServer.Email;

public interface IAwsEmailDeliveryClient
{
    Task SendAsync(AwsEmailAccountOptions account, MailMessage mail);
}
