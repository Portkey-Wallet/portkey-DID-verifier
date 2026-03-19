using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using CAVerifierServer.Options;

namespace CAVerifierServer.Email;

internal static class AwsMailMessageFactory
{
    private const string ConfigSetHeaderName = "X-SES-CONFIGURATION-SET";

    public static MailMessage CreateMessage(string to, string subject, string body, bool isBodyHtml)
    {
        var mail = new MailMessage
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = isBodyHtml
        };
        mail.To.Add(new MailAddress(to));
        return mail;
    }

    public static void NormalizeMail(MailMessage mail)
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

    public static MailMessage CreateAttemptMail(MailMessage source, AwsEmailAccountOptions account,
        MailAddress requestedFrom)
    {
        var attemptMail = CloneMailMessage(source);
        ApplyAccount(attemptMail, account, requestedFrom);
        return attemptMail;
    }

    public static MailMessage CloneMailMessage(MailMessage source)
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

    private static void ApplyAccount(MailMessage mail, AwsEmailAccountOptions account, MailAddress requestedFrom)
    {
        var fromDisplayName = string.IsNullOrWhiteSpace(requestedFrom?.DisplayName)
            ? account.FromName
            : requestedFrom.DisplayName;
        var fromAddress = string.Equals(requestedFrom?.Address, account.From, StringComparison.OrdinalIgnoreCase)
            ? requestedFrom.Address
            : account.From;
        mail.From = new MailAddress(fromAddress, fromDisplayName);
        mail.Sender = null;

        if (string.IsNullOrWhiteSpace(account.ConfigSet))
        {
            mail.Headers.Remove(ConfigSetHeaderName);
            return;
        }

        mail.Headers.Remove(ConfigSetHeaderName);
        mail.Headers.Add(ConfigSetHeaderName, account.ConfigSet);
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
        clonedView.BaseUri = source.BaseUri == null
            ? null
            : new Uri(source.BaseUri.OriginalString, UriKind.RelativeOrAbsolute);

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
}
