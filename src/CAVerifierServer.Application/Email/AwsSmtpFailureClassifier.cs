using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace CAVerifierServer.Email;

internal static class AwsSmtpFailureClassifier
{
    private const string TemporaryUnavailableMessage = "Email delivery is temporarily unavailable. Please try again later.";
    private static readonly string[] RetryableSmtpMessageMarkers =
    {
        "daily message quota exceeded",
        "maximum sending rate exceeded",
        "throttling",
        "temporary authentication failure",
        "temporarily deferred",
        "try again later",
        "temporary failure",
        "connection reset"
    };

    private static readonly string[] NonRetryableSmtpMessageMarkers =
    {
        "authentication",
        "credentials",
        "not authorized",
        "not authorised",
        "message rejected",
        "email address is not verified",
        "address not verified",
        "identity not verified",
        "mail from domain is not verified",
        "mailbox unavailable",
        "invalid address"
    };

    public static bool IsRetryable(SmtpException exception)
    {
        if (exception == null)
        {
            return false;
        }

        if (exception is SmtpFailedRecipientException or SmtpFailedRecipientsException)
        {
            return false;
        }

        var message = exception.Message?.ToLowerInvariant() ?? string.Empty;
        if (ContainsAny(message, RetryableSmtpMessageMarkers))
        {
            return true;
        }

        if (ContainsAny(message, NonRetryableSmtpMessageMarkers))
        {
            return false;
        }

        return exception.StatusCode switch
        {
            SmtpStatusCode.GeneralFailure => true,
            SmtpStatusCode.ServiceNotAvailable => true,
            SmtpStatusCode.LocalErrorInProcessing => true,
            SmtpStatusCode.InsufficientStorage => true,
            SmtpStatusCode.ClientNotPermitted => true,
            _ => false
        };
    }

    public static InvalidOperationException CreateDeliveryUnavailableException(Exception innerException = null)
    {
        return innerException == null
            ? new InvalidOperationException(TemporaryUnavailableMessage)
            : new InvalidOperationException(TemporaryUnavailableMessage, innerException);
    }

    private static bool ContainsAny(string value, IEnumerable<string> markers)
    {
        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
