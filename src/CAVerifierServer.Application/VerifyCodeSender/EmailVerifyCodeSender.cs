using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.Account;
using CAVerifierServer.Email;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Volo.Abp.Emailing;

namespace CAVerifierServer.VerifyCodeSender;

public class EmailVerifyCodeSender : IVerifyCodeSender
{
    public string Type => "Email";
    private readonly Regex _regex;
    private readonly IEmailSender _emailSender;
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly ILogger<EmailVerifyCodeSender> _logger;

    public EmailVerifyCodeSender (IEmailSender emailSender, IOptions<AwsEmailOptions> awsEmailOptions, IOptionsSnapshot<VerifierInfoOptions> verifierinfoOptions,
        ILogger<EmailVerifyCodeSender> logger)
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
            await SendEmailAsync(new SendEmailInput
            {
                From = _awsEmailOptions.From,
                To = email,
                Body = 
                    EmailBodyBuilder.BuildTransactionTemplateBeforeApproval(_verifierInfoOptions.Name, _awsEmailOptions.Image, CAVerifierServerApplicationConsts.PORTKEY,  showOperationDetails),
                Subject = CAVerifierServerApplicationConsts.TransactionBeforeApprovalSubject
            });
        }
        else if (EmailTemplate.AfterApproval.Equals(template))
        {
            await SendEmailAsync(new SendEmailInput
            {
                From = _awsEmailOptions.From,
                To = email,
                Body = 
                    EmailBodyBuilder.BuildTransactionTemplateAfterApproval(_verifierInfoOptions.Name, _awsEmailOptions.Image, CAVerifierServerApplicationConsts.PORTKEY,  showOperationDetails),
                Subject = CAVerifierServerApplicationConsts.TransactionAfterApprovalSubject
            });
        }
    }

    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code, string showOperationDetails)
    {
            await SendEmailAsync(new SendEmailInput
            {
                From = _awsEmailOptions.From,
                To = guardianIdentifier,
                Body = 
                    EmailBodyBuilder.BuildBodyTemplateWithOperationDetails(_verifierInfoOptions.Name, _awsEmailOptions.Image, CAVerifierServerApplicationConsts.PORTKEY, code, showOperationDetails),
                Subject = CAVerifierServerApplicationConsts.Subject
            });
        
    }
    
    public async Task SendCodeToSecondaryEmailAsync(string guardianIdentifier, string code)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = guardianIdentifier,
            Body = 
                EmailBodyBuilder.BuildBodyTemplateForSecondaryEmail(_verifierInfoOptions.Name, _awsEmailOptions.Image, CAVerifierServerApplicationConsts.PORTKEY, code),
            Subject = CAVerifierServerApplicationConsts.SecondaryVerifyCodeSubject
        });
        
    }

    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
        var result = !string.IsNullOrWhiteSpace(guardianIdentifier) && _regex.IsMatch(guardianIdentifier);
        try
        {
            _logger.LogDebug("ValidateGuardianIdentifier guardianIdentifier:{0} !string.IsNullOrWhiteSpace:{1} _regex.IsMatch:{2} validationResult:{3}",
                guardianIdentifier, !string.IsNullOrWhiteSpace(guardianIdentifier), _regex.IsMatch(guardianIdentifier), result);
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "ValidateGuardianIdentifier Error");
        }
        return result;
    }
    
    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
    }
    
}