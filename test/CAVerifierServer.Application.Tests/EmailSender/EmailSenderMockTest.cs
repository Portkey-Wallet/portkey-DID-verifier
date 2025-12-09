using System.Collections.Generic;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Options;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.EmailSender;

public partial class EmailSenderTests
{
    private IOptions<AwsEmailOptions> GetAwsEmailOptions()
    {
        return new OptionsWrapper<AwsEmailOptions>(
            new AwsEmailOptions
            {
                From = "sam@XXXX.com",
                ConfigSet = "MockConfigSet",
                FromName = "MockName",
                Host = "MockHost",
                Image = "MockImage",
                Port = 8000,
                SmtpUsername = "MockUsername",
                SmtpPassword = "MockPassword"
            });
    }
}