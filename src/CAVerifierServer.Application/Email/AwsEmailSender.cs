using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.Timing;
using Volo.Abp.Tracing;

namespace CAVerifierServer.Email;

public class AwsEmailSender : EmailSenderBase
{
    private readonly IAwsEmailDeliveryClient _awsEmailDeliveryClient;
    private readonly IClock _clock;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<AwsEmailSender> _logger;
    private readonly AwsEmailAccountOptions[] _configuredAccounts;
    private readonly AwsEmailRoutingPolicy _routingPolicy;

    public AwsEmailSender(IOptions<AwsEmailOptions> awsEmailOptions, IAwsEmailDeliveryClient awsEmailDeliveryClient,
        IClock clock, ICorrelationIdProvider correlationIdProvider, ILogger<AwsEmailSender> logger,
        IEmailSenderConfiguration configuration, IBackgroundJobManager backgroundJobManager) : base(configuration,
        backgroundJobManager)
    {
        _logger = logger;
        _awsEmailDeliveryClient = awsEmailDeliveryClient;
        _clock = clock;
        _correlationIdProvider = correlationIdProvider;
        var accounts = AwsEmailAccountProvider.BuildConfiguredAccounts(awsEmailOptions.Value);
        _configuredAccounts = accounts.ToArray();
        _routingPolicy = new AwsEmailRoutingPolicy(awsEmailOptions.Value, clock, accounts, logger);
    }

    public override async Task SendAsync(string to, string subject, string body, bool isBodyHtml = true)
    {
        var mail = AwsMailMessageFactory.CreateMessage(to, subject, body, isBodyHtml);
        await SendAsync(mail, normalize: true);
    }

    public override async Task SendAsync(string from, string to, string subject, string body, bool isBodyHtml = true)
    {
        var mail = AwsMailMessageFactory.CreateMessage(to, subject, body, isBodyHtml);
        mail.From = new MailAddress(from);
        await SendAsync(mail, normalize: true);
    }

    public override async Task SendAsync(MailMessage mail, bool normalize = true)
    {
        if (normalize)
        {
            AwsMailMessageFactory.NormalizeMail(mail);
        }

        EnsureRequestedFromMatchesConfiguredAccounts(mail.From);

        await SendEmailAsync(mail);
    }

    protected override async Task SendEmailAsync(MailMessage mail)
    {
        var candidateAccounts = _routingPolicy.GetOrderedCandidateAccounts(out var nextAvailableAt);
        var correlationId = _correlationIdProvider.Get();
        if (candidateAccounts.Count == 0)
        {
            _logger.LogWarning(
                "All aws email accounts are cooling down. CorrelationId:{CorrelationId} NextAvailableAt:{NextAvailableAt}",
                correlationId, nextAvailableAt);
            throw AwsSmtpFailureClassifier.CreateDeliveryUnavailableException();
        }

        var attemptedAccounts = new System.Collections.Generic.List<string>();
        SmtpException lastRetryableException = null;
        using var templateMail = AwsMailMessageFactory.CloneMailMessage(mail);
        var requestedFrom = templateMail.From;
        var maskedRecipient = AwsEmailRecipientMasker.MaskRecipients(templateMail);

        foreach (var account in candidateAccounts)
        {
            var attemptIndex = attemptedAccounts.Count + 1;
            attemptedAccounts.Add(account.Key);
            var attemptStartedAt = _clock.Now;

            using var attemptMail = AwsMailMessageFactory.CreateAttemptMail(templateMail, account, requestedFrom);
            try
            {
                _logger.LogInformation(
                    "Attempting to send email to {MaskedRecipient} via aws account {AccountKey}. CorrelationId:{CorrelationId} Attempt:{AttemptIndex}/{TotalAttempts}",
                    maskedRecipient, account.Key, correlationId, attemptIndex, candidateAccounts.Count);
                await _awsEmailDeliveryClient.SendAsync(account, attemptMail);
                _routingPolicy.ClearCooldownOnSuccess(account.Key, attemptStartedAt);
                if (attemptIndex == 1)
                {
                    _logger.LogInformation(
                        "Email sent to {MaskedRecipient} via preferred aws account {AccountKey}. CorrelationId:{CorrelationId}",
                        maskedRecipient, account.Key, correlationId);
                }
                else
                {
                    _logger.LogInformation(
                        "Email sent to {MaskedRecipient} via failover aws account {AccountKey}. CorrelationId:{CorrelationId} Attempts:{Attempts}",
                        maskedRecipient, account.Key, correlationId, string.Join(" -> ", attemptedAccounts));
                }
                return;
            }
            catch (SmtpException ex) when (AwsSmtpFailureClassifier.IsRetryable(ex))
            {
                lastRetryableException = ex;
                var cooldownUntil = _routingPolicy.MarkCooldown(account.Key);
                _logger.LogWarning(ex,
                    "Retryable aws email failure for {MaskedRecipient}. Account:{AccountKey} CorrelationId:{CorrelationId} CooldownUntil:{CooldownUntil}",
                    maskedRecipient, account.Key, correlationId, cooldownUntil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Non-retryable aws email failure for {MaskedRecipient}. Account:{AccountKey} CorrelationId:{CorrelationId} Attempts:{Attempts}",
                    maskedRecipient, account.Key, correlationId, string.Join(" -> ", attemptedAccounts));
                throw AwsSmtpFailureClassifier.CreateDeliveryUnavailableException(ex);
            }
        }

        _logger.LogError(lastRetryableException,
            "All aws email accounts failed for {MaskedRecipient}. CorrelationId:{CorrelationId} Attempts:{Attempts}",
            maskedRecipient, correlationId, string.Join(" -> ", attemptedAccounts));
        throw AwsSmtpFailureClassifier.CreateDeliveryUnavailableException(lastRetryableException);
    }

    private void EnsureRequestedFromMatchesConfiguredAccounts(MailAddress requestedFrom)
    {
        if (requestedFrom == null)
        {
            return;
        }

        var matchesConfiguredAccount = _configuredAccounts.Any(account =>
            string.Equals(account.From, requestedFrom.Address, StringComparison.OrdinalIgnoreCase));
        if (matchesConfiguredAccount)
        {
            return;
        }

        throw new InvalidOperationException("Requested from address must match a configured aws email account.");
    }
}
