using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CAVerifierServer.Options;

namespace CAVerifierServer.Email;

public class AwsSmtpEmailDeliveryClient : IAwsEmailDeliveryClient
{
    public async Task SendAsync(AwsEmailAccountOptions account, MailMessage mail)
    {
        using var client = new SmtpClient(account.Host, account.Port);
        client.Credentials = new NetworkCredential(account.SmtpUsername, account.SmtpPassword);
        client.EnableSsl = true;
        await client.SendMailAsync(mail);
    }
}
