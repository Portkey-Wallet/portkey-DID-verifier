namespace CAVerifierServer.Options;

public class TwilioSmsMessageOptions
{
    public string AccountSid { get; set; }

    public string AuthToken { get; set; }

    public string ServiceId { get; set; }

    public string TemplateId { get; set; }

    public string DefaultTemplateId { get; set; }
    
    public string Channel { get; set; }
    
    public string Locale { get; set; }
}