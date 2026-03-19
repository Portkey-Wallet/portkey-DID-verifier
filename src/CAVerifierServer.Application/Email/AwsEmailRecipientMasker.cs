using System.Linq;
using System.Net.Mail;

namespace CAVerifierServer.Email;

internal static class AwsEmailRecipientMasker
{
    public static string MaskRecipients(MailMessage mail)
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
