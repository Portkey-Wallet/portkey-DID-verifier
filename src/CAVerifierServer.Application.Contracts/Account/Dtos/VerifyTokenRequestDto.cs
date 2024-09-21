using System.ComponentModel.DataAnnotations;

namespace CAVerifierServer.Account;

public class VerifyTokenRequestDto
{
    [Required] public string AccessToken { get; set; }
    [Required] public string IdentifierHash { get; set; }
    [Required] public string Salt { get; set; }
    [Required] public string OperationType { get; set; }

    public string ChainId { get; set; }
    
    public string OperationDetails { get; set; }
    //the email used for receiving the transaction information before approval
    public string SecondaryEmail { get; set; }
    
    public string ShowOperationDetails { get; set; }
}