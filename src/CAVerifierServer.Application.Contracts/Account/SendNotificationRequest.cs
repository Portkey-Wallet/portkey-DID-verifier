namespace CAVerifierServer.Account;

public class SendNotificationRequest
{
    //see EmailTemplate
    public EmailTemplate Template { get; set; }
    public string Email { get; set; }
    public string ShowOperationDetails { get; set; }
}