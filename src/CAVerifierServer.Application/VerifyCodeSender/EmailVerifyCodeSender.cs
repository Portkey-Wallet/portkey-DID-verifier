using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.Email;
using CAVerifierServer.Options;
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

    public EmailVerifyCodeSender (IEmailSender emailSender, IOptions<AwsEmailOptions> awsEmailOptions, IOptionsSnapshot<VerifierInfoOptions> verifierinfoOptions)
    {
        _emailSender = emailSender;
        _verifierInfoOptions = verifierinfoOptions.Value;
        _awsEmailOptions = awsEmailOptions.Value;
        _regex = new Regex(CAVerifierServerApplicationConsts.EmailRegex);
    }



    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code, string showOperationDetails)
    {
            await SendEmailAsync(new SendEmailInput
            {
                From = _awsEmailOptions.From,
                To = guardianIdentifier,
                Body = showOperationDetails.IsNullOrWhiteSpace() ?
                    EmailBodyBuilder.BuildBodyTemplate(_verifierInfoOptions.Name, _awsEmailOptions.Image, CAVerifierServerApplicationConsts.PORTKEY, code) :
                    EmailBodyBuilder.BuildBodyTemplateWithOperationDetails(_verifierInfoOptions.Name, _awsEmailOptions.Image, CAVerifierServerApplicationConsts.PORTKEY, code, showOperationDetails),
                Subject = CAVerifierServerApplicationConsts.Subject
            });
        
    }

    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
        return !string.IsNullOrWhiteSpace(guardianIdentifier) && _regex.IsMatch(guardianIdentifier);    
    }
    
    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
    }
    
}