using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.Account;
using CAVerifierServer.Email;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.VerifyCodeSender;

public class EmailVerifyCodeSender : IVerifyCodeSender
{
    public string Type => "Email";
    private readonly Regex _regex;
    private readonly IVerifierEmailSender _emailSender;
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly ILogger<EmailVerifyCodeSender> _logger;

    public EmailVerifyCodeSender(IVerifierEmailSender emailSender, IOptions<AwsEmailOptions> awsEmailOptions,
        IOptions<VerifierInfoOptions> verifierinfoOptions, ILogger<EmailVerifyCodeSender> logger)
    {
        _emailSender = emailSender;
        _verifierInfoOptions = verifierinfoOptions.Value;
        _awsEmailOptions = awsEmailOptions.Value;
        _regex = new Regex(CAVerifierServerApplicationConsts.EmailRegex);
        _logger = logger;
    }

    public async Task SendTransactionInfoNotificationAsync(string email, EmailTemplate template, string showOperationDetails)
    {
        if (EmailTemplate.BeforeApproval.Equals(template))
        {
            await QueueEmailAsync(email, CAVerifierServerApplicationConsts.TransactionBeforeApprovalSubject,
                EmailBodyBuilder.BuildTransactionTemplateBeforeApproval(_verifierInfoOptions.Name, _awsEmailOptions.Image,
                    CAVerifierServerApplicationConsts.PORTKEY, showOperationDetails));
        }
        else if (EmailTemplate.AfterApproval.Equals(template))
        {
            await QueueEmailAsync(email, CAVerifierServerApplicationConsts.TransactionAfterApprovalSubject,
                EmailBodyBuilder.BuildTransactionTemplateAfterApproval(_verifierInfoOptions.Name, _awsEmailOptions.Image,
                    CAVerifierServerApplicationConsts.PORTKEY, showOperationDetails));
        }
    }

    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code, string showOperationDetails)
    {
        await QueueEmailAsync(guardianIdentifier, CAVerifierServerApplicationConsts.Subject,
            EmailBodyBuilder.BuildBodyTemplateWithOperationDetails(_verifierInfoOptions.Name, _awsEmailOptions.Image,
                CAVerifierServerApplicationConsts.PORTKEY, code, showOperationDetails));
    }
    
    public async Task SendCodeToSecondaryEmailAsync(string guardianIdentifier, string code)
    {
        await QueueEmailAsync(guardianIdentifier, CAVerifierServerApplicationConsts.SecondaryVerifyCodeSubject,
            EmailBodyBuilder.BuildBodyTemplateForSecondaryEmail(_verifierInfoOptions.Name, _awsEmailOptions.Image,
                CAVerifierServerApplicationConsts.PORTKEY, code));
    }

    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
        var result = !string.IsNullOrWhiteSpace(guardianIdentifier) && _regex.IsMatch(guardianIdentifier);
        var maskedGuardianIdentifier = MaskEmail(guardianIdentifier);
        try
        {
            _logger.LogDebug("ValidateGuardianIdentifier guardianIdentifier:{0} !string.IsNullOrWhiteSpace:{1} _regex.IsMatch:{2} validationResult:{3}",
                maskedGuardianIdentifier, !string.IsNullOrWhiteSpace(guardianIdentifier), _regex.IsMatch(guardianIdentifier), result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateGuardianIdentifier Error");
        }
        return result;
    }
    
    private async Task QueueEmailAsync(string to, string subject, string body)
    {
        await _emailSender.QueueAsync(to, subject, body, true);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var parts = email.Split('@');
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
