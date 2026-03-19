using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
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
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly IAwsEmailDeliveryClient _awsEmailDeliveryClient;
    private readonly IClock _clock;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<AwsEmailSender> _logger;
    private readonly IReadOnlyList<AwsEmailAccountOptions> _accounts;
    private readonly ConcurrentDictionary<string, DateTime> _cooldownAccounts = new(StringComparer.OrdinalIgnoreCase);
    private int _roundRobinCursor = -1;
    private const string ConfigSetHeaderName = "X-SES-CONFIGURATION-SET";


    public AwsEmailSender(IOptions<AwsEmailOptions> awsEmailOptions, IAwsEmailDeliveryClient awsEmailDeliveryClient,
        IClock clock, ICorrelationIdProvider correlationIdProvider, ILogger<AwsEmailSender> logger,
        IEmailSenderConfiguration configuration, IBackgroundJobManager backgroundJobManager) : base(configuration,
        backgroundJobManager)
    {
        _logger = logger;
        _awsEmailOptions = awsEmailOptions.Value;
        _awsEmailDeliveryClient = awsEmailDeliveryClient;
        _clock = clock;
        _correlationIdProvider = correlationIdProvider;
        _accounts = BuildConfiguredAccounts(_awsEmailOptions);
    }

    public override async Task SendAsync(string to, string subject, string body, bool isBodyHtml = true)
    {
        var mail = new MailMessage
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = isBodyHtml
        };
        mail.To.Add(new MailAddress(to));
        await SendAsync(mail, normalize: true);
    }

    public override async Task SendAsync(string from, string to, string subject, string body, bool isBodyHtml = true)
    {
        var mail = new MailMessage(from, to, subject, body)
        {
            IsBodyHtml = isBodyHtml
        };
        await SendAsync(mail, normalize: true);
    }

    public override async Task SendAsync(MailMessage mail, bool normalize = true)
    {
        if (normalize)
        {
            NormalizeMail(mail);
        }

        await SendEmailAsync(mail);
    }

    protected override async Task SendEmailAsync(MailMessage mail)
    {
        var now = _clock.Now;
        var candidateAccounts = GetOrderedCandidateAccounts(now);
        var attemptedAccounts = new List<string>();
        Exception lastRetryableException = null;
        var correlationId = _correlationIdProvider.Get();
        using var templateMail = CloneMailMessage(mail);
        var requestedFrom = templateMail.From;
        var maskedRecipient = MaskRecipients(templateMail);

        foreach (var account in candidateAccounts)
        {
            var attemptIndex = attemptedAccounts.Count + 1;
            attemptedAccounts.Add(account.Key);

            using var attemptMail = CreateAttemptMail(templateMail, account, requestedFrom);
            try
            {
                _logger.LogInformation(
                    "Attempting to send email to {MaskedRecipient} via aws account {AccountKey}. CorrelationId:{CorrelationId} Attempt:{AttemptIndex}/{TotalAttempts}",
                    maskedRecipient, account.Key, correlationId, attemptIndex, candidateAccounts.Count);
                await _awsEmailDeliveryClient.SendAsync(account, attemptMail);
                _cooldownAccounts.TryRemove(account.Key, out _);
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
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastRetryableException = ex;
                MarkCooldown(account.Key);
                _logger.LogWarning(ex,
                    "Retryable aws email failure for {MaskedRecipient}. Account:{AccountKey} CorrelationId:{CorrelationId} CooldownUntil:{CooldownUntil}",
                    maskedRecipient, account.Key, correlationId, _cooldownAccounts[account.Key]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Non-retryable aws email failure for {MaskedRecipient}. Account:{AccountKey} CorrelationId:{CorrelationId}",
                    maskedRecipient, account.Key, correlationId);
                throw;
            }
        }

        throw new InvalidOperationException(
            $"All aws email accounts failed. Accounts tried: {string.Join(" -> ", attemptedAccounts)}",
            lastRetryableException);
    }

    private static IReadOnlyList<AwsEmailAccountOptions> BuildConfiguredAccounts(AwsEmailOptions options)
    {
        var configuredAccounts = options.Accounts ?? new List<AwsEmailAccountOptions>();
        var hasConfiguredAccounts = configuredAccounts.Count > 0;
        var accounts = configuredAccounts
            .Where(account => account != null)
            .Select((account, index) => NormalizeAccount(account, index))
            .Where(account => account.Enabled)
            .ToList();

        if (accounts.Count == 0 && !hasConfiguredAccounts && HasLegacyAccount(options))
        {
            accounts.Add(NormalizeAccount(new AwsEmailAccountOptions
            {
                Key = "default",
                Enabled = true,
                From = options.From,
                FromName = options.FromName,
                SmtpUsername = options.SmtpUsername,
                SmtpPassword = options.SmtpPassword,
                ConfigSet = options.ConfigSet,
                Host = options.Host,
                Port = options.Port
            }, 0));
        }

        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("No enabled aws email account is configured.");
        }

        var duplicatedKeys = accounts.GroupBy(account => account.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicatedKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicated aws email account keys: {string.Join(", ", duplicatedKeys)}");
        }

        return accounts;
    }

    private static AwsEmailAccountOptions NormalizeAccount(AwsEmailAccountOptions account, int index)
    {
        var normalized = new AwsEmailAccountOptions
        {
            Key = string.IsNullOrWhiteSpace(account.Key) ? $"account-{index + 1}" : account.Key.Trim(),
            Enabled = account.Enabled,
            From = account.From?.Trim(),
            FromName = account.FromName?.Trim(),
            SmtpUsername = account.SmtpUsername?.Trim(),
            SmtpPassword = account.SmtpPassword,
            ConfigSet = account.ConfigSet?.Trim(),
            Host = account.Host?.Trim(),
            Port = account.Port
        };

        ValidateAccount(normalized);
        return normalized;
    }

    private static void ValidateAccount(AwsEmailAccountOptions account)
    {
        if (!account.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(account.From))
        {
            throw new InvalidOperationException($"Aws email account {account.Key} must configure From.");
        }

        if (string.IsNullOrWhiteSpace(account.SmtpUsername))
        {
            throw new InvalidOperationException(
                $"Aws email account {account.Key} must configure SmtpUsername.");
        }

        if (string.IsNullOrWhiteSpace(account.SmtpPassword))
        {
            throw new InvalidOperationException(
                $"Aws email account {account.Key} must configure SmtpPassword.");
        }

        if (string.IsNullOrWhiteSpace(account.Host))
        {
            throw new InvalidOperationException($"Aws email account {account.Key} must configure Host.");
        }

        if (account.Port <= 0)
        {
            throw new InvalidOperationException($"Aws email account {account.Key} must configure Port.");
        }
    }

    private static bool HasLegacyAccount(AwsEmailOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.From) &&
               !string.IsNullOrWhiteSpace(options.SmtpUsername) &&
               !string.IsNullOrWhiteSpace(options.SmtpPassword) &&
               !string.IsNullOrWhiteSpace(options.Host) &&
               options.Port > 0;
    }

    private void NormalizeMail(MailMessage mail)
    {
        if (mail.HeadersEncoding == null)
        {
            mail.HeadersEncoding = Encoding.UTF8;
        }

        if (mail.SubjectEncoding == null)
        {
            mail.SubjectEncoding = Encoding.UTF8;
        }

        if (mail.BodyEncoding == null)
        {
            mail.BodyEncoding = Encoding.UTF8;
        }
    }

    private List<AwsEmailAccountOptions> GetOrderedCandidateAccounts(DateTime now)
    {
        var availableAccounts = _accounts.Where(account => !IsCoolingDown(account.Key, now)).ToList();
        if (availableAccounts.Count == 0)
        {
            _logger.LogWarning("All aws email accounts are in cooldown. Falling back to all configured accounts.");
            availableAccounts = _accounts.ToList();
        }

        if (_awsEmailOptions.SelectionMode != AwsEmailSelectionMode.RoundRobin)
        {
            _logger.LogWarning("Unsupported aws email selection mode {SelectionMode}. Falling back to RoundRobin.",
                _awsEmailOptions.SelectionMode);
        }

        var startIndex = GetStartIndex(availableAccounts.Count);
        return availableAccounts.Skip(startIndex).Concat(availableAccounts.Take(startIndex)).ToList();
    }

    private int GetStartIndex(int count)
    {
        if (count <= 1)
        {
            return 0;
        }

        var next = Interlocked.Increment(ref _roundRobinCursor);
        return (int)((uint)next % (uint)count);
    }

    private bool IsCoolingDown(string accountKey, DateTime now)
    {
        if (!_cooldownAccounts.TryGetValue(accountKey, out var cooldownUntil))
        {
            return false;
        }

        if (cooldownUntil > now)
        {
            return true;
        }

        _cooldownAccounts.TryRemove(accountKey, out _);
        return false;
    }

    private void MarkCooldown(string accountKey)
    {
        _cooldownAccounts[accountKey] = _clock.Now.AddSeconds(Math.Max(0, _awsEmailOptions.FailureCooldownSeconds));
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception switch
        {
            FormatException => false,
            ArgumentException => false,
            SmtpFailedRecipientsException => false,
            SmtpFailedRecipientException => false,
            _ => true
        };
    }

    private static void ApplyAccount(MailMessage mail, AwsEmailAccountOptions account, MailAddress requestedFrom)
    {
        var fromAddress = requestedFrom?.Address;
        var fromDisplayName = requestedFrom?.DisplayName;
        mail.From = string.IsNullOrWhiteSpace(fromAddress)
            ? new MailAddress(account.From, account.FromName)
            : new MailAddress(fromAddress, string.IsNullOrWhiteSpace(fromDisplayName) ? account.FromName : fromDisplayName);

        if (string.IsNullOrWhiteSpace(account.ConfigSet))
        {
            mail.Headers.Remove(ConfigSetHeaderName);
            return;
        }

        mail.Headers.Remove(ConfigSetHeaderName);
        mail.Headers.Add(ConfigSetHeaderName, account.ConfigSet);
    }

    private static MailMessage CreateAttemptMail(MailMessage source, AwsEmailAccountOptions account, MailAddress requestedFrom)
    {
        // Each attempt gets its own MailMessage instance so failover mutations stay isolated.
        var attemptMail = CloneMailMessage(source);
        ApplyAccount(attemptMail, account, requestedFrom);
        return attemptMail;
    }

    private static MailMessage CloneMailMessage(MailMessage source)
    {
        var clonedMail = new MailMessage
        {
            Subject = source.Subject,
            SubjectEncoding = source.SubjectEncoding,
            Body = source.Body,
            BodyEncoding = source.BodyEncoding,
            IsBodyHtml = source.IsBodyHtml,
            HeadersEncoding = source.HeadersEncoding,
            Priority = source.Priority,
            DeliveryNotificationOptions = source.DeliveryNotificationOptions
        };

        if (source.Sender != null)
        {
            clonedMail.Sender = CloneMailAddress(source.Sender);
        }

        if (source.From != null)
        {
            clonedMail.From = CloneMailAddress(source.From);
        }

        CopyAddressCollection(source.To, clonedMail.To);
        CopyAddressCollection(source.CC, clonedMail.CC);
        CopyAddressCollection(source.Bcc, clonedMail.Bcc);

        foreach (var replyTo in source.ReplyToList)
        {
            clonedMail.ReplyToList.Add(CloneMailAddress(replyTo));
        }

        foreach (var headerKey in source.Headers.AllKeys.Where(key =>
                     !string.Equals(key, ConfigSetHeaderName, StringComparison.OrdinalIgnoreCase)))
        {
            clonedMail.Headers.Add(headerKey, source.Headers[headerKey]);
        }

        foreach (Attachment attachment in source.Attachments)
        {
            clonedMail.Attachments.Add(CloneAttachment(attachment));
        }

        foreach (AlternateView alternateView in source.AlternateViews)
        {
            clonedMail.AlternateViews.Add(CloneAlternateView(alternateView));
        }

        return clonedMail;
    }

    private static void CopyAddressCollection(MailAddressCollection source, MailAddressCollection target)
    {
        foreach (var address in source)
        {
            target.Add(CloneMailAddress(address));
        }
    }

    private static MailAddress CloneMailAddress(MailAddress address)
    {
        return new MailAddress(address.Address, address.DisplayName);
    }

    private static Attachment CloneAttachment(Attachment source)
    {
        var clonedAttachment = new Attachment(CloneContentStream(source.ContentStream), source.Name,
            source.ContentType?.MediaType);
        clonedAttachment.ContentId = source.ContentId;
        clonedAttachment.NameEncoding = source.NameEncoding;
        clonedAttachment.TransferEncoding = source.TransferEncoding;
        CopyContentType(source.ContentType, clonedAttachment.ContentType);
        CopyContentDisposition(source.ContentDisposition, clonedAttachment.ContentDisposition);
        return clonedAttachment;
    }

    private static AlternateView CloneAlternateView(AlternateView source)
    {
        var clonedView = new AlternateView(CloneContentStream(source.ContentStream), CloneContentType(source.ContentType));
        clonedView.TransferEncoding = source.TransferEncoding;

        foreach (LinkedResource linkedResource in source.LinkedResources)
        {
            clonedView.LinkedResources.Add(CloneLinkedResource(linkedResource));
        }

        return clonedView;
    }

    private static LinkedResource CloneLinkedResource(LinkedResource source)
    {
        var clonedResource = new LinkedResource(CloneContentStream(source.ContentStream), CloneContentType(source.ContentType));
        clonedResource.ContentId = source.ContentId;
        clonedResource.TransferEncoding = source.TransferEncoding;
        CopyContentType(source.ContentType, clonedResource.ContentType);
        CopyContentLink(source.ContentLink, contentLink => clonedResource.ContentLink = contentLink);
        return clonedResource;
    }

    private static ContentType CloneContentType(ContentType source)
    {
        if (source == null)
        {
            return new ContentType(MediaTypeNames.Text.Plain);
        }

        var clonedContentType = new ContentType(source.MediaType);
        CopyContentType(source, clonedContentType);
        return clonedContentType;
    }

    private static void CopyContentType(ContentType source, ContentType target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.Boundary = source.Boundary;
        target.CharSet = source.CharSet;
        target.Name = source.Name;
        foreach (var parameterKey in source.Parameters.Keys.Cast<string>())
        {
            target.Parameters[parameterKey] = source.Parameters[parameterKey];
        }
    }

    private static void CopyContentDisposition(ContentDisposition source, ContentDisposition target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.CreationDate = source.CreationDate;
        target.DispositionType = source.DispositionType;
        target.FileName = source.FileName;
        target.Inline = source.Inline;
        target.ModificationDate = source.ModificationDate;
        target.ReadDate = source.ReadDate;
        target.Size = source.Size;
    }

    private static void CopyContentLink(Uri source, Action<Uri> assign)
    {
        if (source == null)
        {
            return;
        }

        assign(new Uri(source.OriginalString, UriKind.RelativeOrAbsolute));
    }

    private static Stream CloneContentStream(Stream source)
    {
        if (source == null)
        {
            return Stream.Null;
        }

        var clonedStream = new MemoryStream();
        var resetPosition = source.CanSeek ? source.Position : 0;
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        source.CopyTo(clonedStream);
        clonedStream.Position = 0;

        if (source.CanSeek)
        {
            source.Position = resetPosition;
        }

        return clonedStream;
    }

    private static string MaskRecipients(MailMessage mail)
    {
        return string.Join(",", mail.To.Select(address => MaskRecipient(address.Address)));
    }

    private static string MaskRecipient(string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return string.Empty;
        }

        var parts = recipient.Split('@');
        if (parts.Length != 2 || parts[0].Length == 0)
        {
            return "***";
        }

        var localName = parts[0].Length == 1
            ? $"{parts[0][0]}***"
            : $"{parts[0][0]}***{parts[0][^1]}";
        return $"{localName}@{parts[1]}";
    }
}
