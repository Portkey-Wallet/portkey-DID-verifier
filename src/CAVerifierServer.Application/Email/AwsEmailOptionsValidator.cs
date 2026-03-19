using System;
using CAVerifierServer.Options;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.Email;

public sealed class AwsEmailOptionsValidator : IValidateOptions<AwsEmailOptions>
{
    public ValidateOptionsResult Validate(string name, AwsEmailOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("awsEmail options are required.");
        }

        try
        {
            AwsEmailAccountProvider.BuildConfiguredAccounts(options);
            return ValidateOptionsResult.Success;
        }
        catch (Exception exception)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
    }
}
