using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CAVerifierServer.Email;
using CAVerifierServer.Options;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.Timing;
using Volo.Abp.Tracing;
using Xunit;

namespace CAVerifierServer.EmailSender;

public class AwsEmailSenderLoadBalancingTests
{
    [Fact]
    public async Task Should_Fallback_To_Legacy_Single_Account_Config()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        var sender = CreateSender(new AwsEmailOptions
        {
            From = "legacy@portkey.com",
            FromName = "Legacy Sender",
            Host = "smtp.legacy.com",
            Port = 587,
            SmtpUsername = "legacy-user",
            SmtpPassword = "legacy-password"
        }, deliveryClient);

        await sender.SendAsync("user@example.com", "subject", "body");

        deliveryClient.Attempts.Count.ShouldBe(1);
        deliveryClient.Attempts[0].AccountKey.ShouldBe("default");
        deliveryClient.Attempts[0].From.ShouldBe("legacy@portkey.com");
    }

    [Fact]
    public async Task Should_RoundRobin_Across_Multiple_Accounts()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        var sender = CreateSender(CreateMultiAccountOptions(), deliveryClient);

        await sender.SendAsync("user@example.com", "subject-1", "body");
        await sender.SendAsync("user@example.com", "subject-2", "body");
        await sender.SendAsync("user@example.com", "subject-3", "body");
        await sender.SendAsync("user@example.com", "subject-4", "body");

        deliveryClient.Attempts.Select(attempt => attempt.AccountKey)
            .ShouldBe(new[] { "primary", "secondary", "tertiary", "primary" });
    }

    [Fact]
    public async Task Should_Failover_And_Skip_Cooldown_Account()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        deliveryClient.EnqueueFailure("primary", new SmtpException("primary throttled"));
        var sender = CreateSender(CreateMultiAccountOptions(cooldownSeconds: 300), deliveryClient);

        await sender.SendAsync("user@example.com", "subject-1", "body");
        await sender.SendAsync("user@example.com", "subject-2", "body");

        deliveryClient.Attempts.Select(attempt => attempt.AccountKey)
            .ShouldBe(new[] { "primary", "secondary", "tertiary" });
    }

    [Fact]
    public async Task Should_Rejoin_Rotation_After_Cooldown_Expires()
    {
        var clock = new TestClock();
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        deliveryClient.EnqueueFailure("primary", new SmtpException("primary throttled"));
        var sender = CreateSender(CreateMultiAccountOptions(cooldownSeconds: 60), deliveryClient, clock);

        await sender.SendAsync("user@example.com", "subject-1", "body");
        clock.Advance(TimeSpan.FromSeconds(61));
        await sender.SendAsync("user@example.com", "subject-2", "body");
        await sender.SendAsync("user@example.com", "subject-3", "body");
        await sender.SendAsync("user@example.com", "subject-4", "body");

        deliveryClient.Attempts.Take(2).Select(attempt => attempt.AccountKey)
            .ShouldBe(new[] { "primary", "secondary" });
        deliveryClient.Attempts.Skip(2).Select(attempt => attempt.AccountKey)
            .ShouldContain("primary");
        deliveryClient.Attempts.Skip(2).Select(attempt => attempt.AccountKey)
            .ShouldContain("tertiary");
    }

    [Fact]
    public async Task Should_Not_Retry_For_NonRetryable_Error()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        deliveryClient.EnqueueFailure("primary", new SmtpFailedRecipientException("bad recipient"));
        var sender = CreateSender(CreateMultiAccountOptions(), deliveryClient);

        await Should.ThrowAsync<SmtpFailedRecipientException>(() =>
            sender.SendAsync("user@example.com", "subject", "body"));

        deliveryClient.Attempts.Select(attempt => attempt.AccountKey)
            .ShouldBe(new[] { "primary" });
    }

    [Fact]
    public async Task Should_Throw_Aggregated_Error_When_All_Accounts_Fail()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        deliveryClient.EnqueueFailure("primary", new SmtpException("primary throttled"));
        deliveryClient.EnqueueFailure("secondary", new SmtpException("secondary throttled"));
        deliveryClient.EnqueueFailure("tertiary", new SmtpException("tertiary throttled"));
        var sender = CreateSender(CreateMultiAccountOptions(), deliveryClient);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            sender.SendAsync("user@example.com", "subject", "body"));

        exception.Message.ShouldContain("primary -> secondary -> tertiary");
    }

    [Fact]
    public async Task Should_Failover_With_NonSeekable_Attachment_Stream()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        deliveryClient.EnqueueFailure("primary", new SmtpException("primary throttled"));
        var sender = CreateSender(CreateMultiAccountOptions(), deliveryClient);
        using var mail = new MailMessage
        {
            Subject = "subject",
            Body = "body",
            IsBodyHtml = true
        };
        mail.To.Add("user@example.com");
        mail.Attachments.Add(new Attachment(new NonSeekableReadStream("attachment-content"), "proof.txt",
            MediaTypeNames.Text.Plain));

        await sender.SendAsync(mail);

        deliveryClient.Attempts.Select(attempt => attempt.AccountKey)
            .ShouldBe(new[] { "primary", "secondary" });
        deliveryClient.Attempts.Take(2).All(attempt => attempt.AttachmentBytes > 0).ShouldBeTrue();
    }

    [Fact]
    public void Should_Not_Fallback_To_Legacy_When_Accounts_Are_Configured_But_Disabled()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        var options = new AwsEmailOptions
        {
            From = "legacy@portkey.com",
            Host = "smtp.legacy.com",
            Port = 587,
            SmtpUsername = "legacy-user",
            SmtpPassword = "legacy-password",
            Accounts = new List<AwsEmailAccountOptions>
            {
                new()
                {
                    Key = "disabled-primary",
                    Enabled = false,
                    From = "disabled@portkey.com",
                    Host = "smtp.disabled.com",
                    Port = 587,
                    SmtpUsername = "disabled-user",
                    SmtpPassword = "disabled-password"
                }
            }
        };

        Should.Throw<InvalidOperationException>(() => CreateSender(options, deliveryClient))
            .Message.ShouldContain("No enabled aws email account is configured.");
    }

    [Fact]
    public async Task Should_Try_Again_When_All_Accounts_Are_In_Cooldown()
    {
        var deliveryClient = new FakeAwsEmailDeliveryClient();
        deliveryClient.EnqueueFailure("primary", new SmtpException("primary throttled"));
        deliveryClient.EnqueueFailure("secondary", new SmtpException("secondary throttled"));
        deliveryClient.EnqueueFailure("tertiary", new SmtpException("tertiary throttled"));
        var sender = CreateSender(CreateMultiAccountOptions(cooldownSeconds: 300), deliveryClient);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            sender.SendAsync("user@example.com", "subject-1", "body"));

        await sender.SendAsync("user@example.com", "subject-2", "body");

        deliveryClient.Attempts.Count.ShouldBe(4);
        deliveryClient.Attempts.Skip(3).Select(attempt => attempt.AccountKey).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task EmailVerifyCodeSender_Should_Use_Html_QueueAsync_Without_From()
    {
        var emailSender = new Mock<IEmailSender>();
        emailSender.Setup(sender => sender.QueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>())).Returns(Task.CompletedTask);
        var verifyCodeSender = new CAVerifierServer.VerifyCodeSender.EmailVerifyCodeSender(emailSender.Object,
            new OptionsWrapper<AwsEmailOptions>(new AwsEmailOptions { Image = "https://image.png" }),
            new OptionsWrapper<VerifierInfoOptions>(new VerifierInfoOptions { Name = "Verifier-001" }),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CAVerifierServer.VerifyCodeSender.EmailVerifyCodeSender>>());

        await verifyCodeSender.SendCodeByGuardianIdentifierAsync("user@example.com", "123456", "");

        emailSender.Verify(sender => sender.QueueAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), true),
            Times.Once);
        emailSender.Verify(sender => sender.QueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    private static AwsEmailSender CreateSender(AwsEmailOptions options, FakeAwsEmailDeliveryClient deliveryClient,
        IClock clock = null)
    {
        return new AwsEmailSender(new OptionsWrapper<AwsEmailOptions>(options), deliveryClient, clock ?? new TestClock(),
            new StaticCorrelationIdProvider(), Mock.Of<Microsoft.Extensions.Logging.ILogger<AwsEmailSender>>(),
            Mock.Of<IEmailSenderConfiguration>(), Mock.Of<IBackgroundJobManager>());
    }

    private static AwsEmailOptions CreateMultiAccountOptions(int cooldownSeconds = 300)
    {
        return new AwsEmailOptions
        {
            FailureCooldownSeconds = cooldownSeconds,
            Accounts = new List<AwsEmailAccountOptions>
            {
                CreateAccount("primary"),
                CreateAccount("secondary"),
                CreateAccount("tertiary")
            }
        };
    }

    private static AwsEmailAccountOptions CreateAccount(string key)
    {
        return new AwsEmailAccountOptions
        {
            Key = key,
            Enabled = true,
            From = $"{key}@portkey.com",
            FromName = $"Portkey {key}",
            Host = $"{key}.smtp.portkey.com",
            Port = 587,
            SmtpUsername = $"{key}-user",
            SmtpPassword = $"{key}-password"
        };
    }

    private sealed class FakeAwsEmailDeliveryClient : IAwsEmailDeliveryClient
    {
        private readonly Dictionary<string, Queue<Exception>> _failures = new(StringComparer.OrdinalIgnoreCase);
        public List<EmailAttemptRecord> Attempts { get; } = new();

        public void EnqueueFailure(string accountKey, Exception exception)
        {
            if (!_failures.TryGetValue(accountKey, out var queue))
            {
                queue = new Queue<Exception>();
                _failures[accountKey] = queue;
            }

            queue.Enqueue(exception);
        }

        public Task SendAsync(AwsEmailAccountOptions account, MailMessage mail)
        {
            Attempts.Add(new EmailAttemptRecord(account.Key, mail.From?.Address,
                mail.To.Single().Address, mail.Headers["X-SES-CONFIGURATION-SET"], GetAttachmentBytes(mail)));

            if (_failures.TryGetValue(account.Key, out var queue) && queue.Count > 0)
            {
                throw queue.Dequeue();
            }

            return Task.CompletedTask;
        }

        private static long GetAttachmentBytes(MailMessage mail)
        {
            return mail.Attachments.Cast<Attachment>().Sum(attachment => ReadStreamLength(attachment.ContentStream));
        }

        private static long ReadStreamLength(System.IO.Stream stream)
        {
            if (stream == null)
            {
                return 0;
            }

            if (stream.CanSeek)
            {
                return stream.Length;
            }

            using var buffer = new System.IO.MemoryStream();
            stream.CopyTo(buffer);
            return buffer.Length;
        }
    }

    private sealed record EmailAttemptRecord(string AccountKey, string From, string To, string ConfigSet,
        long AttachmentBytes);

    private sealed class StaticCorrelationIdProvider : ICorrelationIdProvider
    {
        public string Get()
        {
            return "corr-001";
        }
    }

    private sealed class TestClock : IClock
    {
        private DateTime _now = DateTime.UtcNow;

        public DateTime Now => _now;

        public DateTimeKind Kind => DateTimeKind.Utc;

        public bool SupportsMultipleTimezone => true;

        public DateTime Normalize(DateTime dateTime)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        public void Advance(TimeSpan offset)
        {
            _now = _now.Add(offset);
        }
    }

    private sealed class NonSeekableReadStream : System.IO.MemoryStream
    {
        public NonSeekableReadStream(string content) : base(Encoding.UTF8.GetBytes(content))
        {
        }

        public override bool CanSeek => false;
    }
}
