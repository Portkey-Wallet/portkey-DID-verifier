using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.Email;
using CAVerifierServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Volo.Abp.Emailing;
using Xunit;

namespace CAVerifierServer.EmailSender;

[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public class VerifierEmailSenderIntegrationTests : CAVerifierServerApplicationTestBase
{
    private readonly TestAwsEmailDeliveryClient _deliveryClient = new();
    private readonly IVerifierEmailSender _verifierEmailSender;
    private readonly IEmailSender _defaultEmailSender;

    public VerifierEmailSenderIntegrationTests()
    {
        _verifierEmailSender = GetRequiredService<IVerifierEmailSender>();
        _defaultEmailSender = GetRequiredService<IEmailSender>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IAwsEmailDeliveryClient>(_deliveryClient));
        services.Configure<AwsEmailOptions>(options =>
        {
            options.SelectionMode = AwsEmailSelectionMode.RoundRobin;
            options.FailureCooldownSeconds = 300;
            options.Image = "https://image.png";
            options.Accounts = new List<AwsEmailAccountOptions>
            {
                new()
                {
                    Key = "primary",
                    Enabled = true,
                    From = "primary@portkey.com",
                    FromName = "Portkey primary",
                    Host = "primary.smtp.portkey.com",
                    Port = 587,
                    SmtpUsername = "primary-user",
                    SmtpPassword = "primary-password"
                }
            };
        });
    }

    [Fact]
    public void Should_Resolve_Verifier_Email_Sender_Separately_From_Default_IEmailSender()
    {
        _verifierEmailSender.ShouldBeOfType<AwsEmailSender>();
        _defaultEmailSender.ShouldNotBeOfType<AwsEmailSender>();
    }

    [Fact]
    public async Task QueueAsync_Should_Send_Through_Verifier_Email_Sender()
    {
        await _verifierEmailSender.QueueAsync("user@example.com", "subject", "body", true);

        _deliveryClient.Attempts.Count.ShouldBe(1);
        _deliveryClient.Attempts[0].AccountKey.ShouldBe("primary");
        _deliveryClient.Attempts[0].From.ShouldBe("primary@portkey.com");
        _deliveryClient.Attempts[0].To.ShouldBe("user@example.com");
    }

    private sealed class TestAwsEmailDeliveryClient : IAwsEmailDeliveryClient
    {
        public List<AttemptRecord> Attempts { get; } = new();

        public Task SendAsync(AwsEmailAccountOptions account, System.Net.Mail.MailMessage mail)
        {
            Attempts.Add(new AttemptRecord(account.Key, mail.From?.Address, mail.To[0].Address));
            return Task.CompletedTask;
        }
    }

    private sealed record AttemptRecord(string AccountKey, string From, string To);
}
