using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.Timing;
using Volo.Abp.Tracing;

namespace CAVerifierServer.Email;

public class AwsEmailSender : EmailSenderBase, IVerifierEmailSender
{
    private readonly IAwsEmailDeliveryClient _awsEmailDeliveryClient;
    private readonly IClock _clock;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<AwsEmailSender> _logger;
    private readonly AwsEmailRoutingPolicy _routingPolicy;

    public AwsEmailSender(IAwsEmailDeliveryClient awsEmailDeliveryClient, AwsEmailRoutingPolicy routingPolicy,
        IClock clock, ICorrelationIdProvider correlationIdProvider, ILogger<AwsEmailSender> logger,
        IEmailSenderConfiguration configuration, IBackgroundJobManager backgroundJobManager) : base(configuration,
        backgroundJobManager)
    {
        _logger = logger;
        _awsEmailDeliveryClient = awsEmailDeliveryClient;
        _clock = clock;
        _correlationIdProvider = correlationIdProvider;
        _routingPolicy = routingPolicy;
    }

    protected override Task NormalizeMailAsync(MailMessage mail)
    {
        AwsMailMessageFactory.NormalizeMail(mail);
        return Task.CompletedTask;
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
        var requestedFrom = ResolveRequestedFrom(templateMail.From, correlationId);
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
                throw;
            }
        }

        _logger.LogError(lastRetryableException,
            "All aws email accounts failed for {MaskedRecipient}. CorrelationId:{CorrelationId} Attempts:{Attempts}",
            maskedRecipient, correlationId, string.Join(" -> ", attemptedAccounts));
        throw AwsSmtpFailureClassifier.CreateDeliveryUnavailableException(lastRetryableException);
    }

    private MailAddress ResolveRequestedFrom(MailAddress requestedFrom, string correlationId)
    {
        if (requestedFrom == null)
        {
            return null;
        }

        foreach (var account in _routingPolicy.ConfiguredAccounts)
        {
            if (string.Equals(account.From, requestedFrom.Address, StringComparison.OrdinalIgnoreCase))
            {
                return requestedFrom;
            }
        }

        _logger.LogWarning(
            "Ignoring unsupported requested from address {RequestedFrom} for verifier aws email sender. CorrelationId:{CorrelationId}",
            requestedFrom.Address, correlationId);
        return requestedFrom;
    }
}
